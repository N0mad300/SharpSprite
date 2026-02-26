using System.IO.Compression;
using SharpSprite.Core.Document;

namespace SharpSprite.Infrastructure.Ase
{
    /// <summary>
    /// Writes a <see cref="SharpSprite.Core.Document.Document"/> to an
    /// .ase / .aseprite binary file.
    ///
    /// The encoder writes:
    /// <list type="bullet">
    ///   <item>128-byte file header</item>
    ///   <item>For each frame: frame header + layer chunks (frame 0) + cel chunks +
    ///         palette chunk + tags chunk (frame 0) + slice chunks (frame 0) +
    ///         tileset chunks (frame 0)</item>
    ///   <item>Compressed RGBA / Grayscale / Indexed image data (ZLIB)</item>
    ///   <item>User data chunks after layers / cels / tags / slices</item>
    /// </list>
    /// </summary>
    public sealed class AseEncoder
    {
        // ── Public entry points ───────────────────────────────────────────

        /// <summary>Encode and write to a file path.</summary>
        public static void EncodeFile(Document doc, string path)
        {
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            EncodeStream(doc, fs);
        }

        /// <summary>Encode and write to any writable <see cref="Stream"/>.</summary>
        public static void EncodeStream(Document doc, Stream stream)
        {
            new AseEncoder().Encode(doc, stream);
        }

        // ── Internal state ────────────────────────────────────────────────

        // Flat ordered list of layers matching NOTE.2 in the spec
        private readonly List<Layer> _layerIndex = new();

        // ── Main encode ───────────────────────────────────────────────────

        private void Encode(Document doc, Stream stream)
        {
            var sprite = doc.Sprite;

            // Build flat layer index first
            BuildLayerIndex(sprite.RootGroup, _layerIndex);

            using var w = new AseWriter(stream, leaveOpen: true);

            // ── Reserve space for the file header (128 bytes) ─────────────
            long headerStart = w.Position;
            w.WritePad(AseConstants.HeaderSize);

            // ── Frames ────────────────────────────────────────────────────
            for (int f = 0; f < sprite.FrameCount; f++)
                WriteFrame(w, sprite, f);

            w.Flush();
            long fileSize = w.Position;

            // ── Patch the file header ─────────────────────────────────────
            w.Seek(headerStart);
            WriteFileHeader(w, sprite, fileSize);
            w.Flush();
        }

        // ── File header ───────────────────────────────────────────────────

        private static void WriteFileHeader(AseWriter w, Sprite sprite, long fileSize)
        {
            ushort colorDepth = sprite.ColorMode switch
            {
                ColorMode.Rgba => AseConstants.ColorDepth_RGBA,
                ColorMode.Grayscale => AseConstants.ColorDepth_Grayscale,
                _ => AseConstants.ColorDepth_Indexed,
            };

            w.WriteDWORD((uint)fileSize);
            w.WriteWORD(AseConstants.FileMagic);
            w.WriteWORD((ushort)sprite.FrameCount);
            w.WriteWORD((ushort)sprite.Width);
            w.WriteWORD((ushort)sprite.Height);
            w.WriteWORD(colorDepth);

            // Flags: layer opacity valid (1) + group opacity valid (2)
            w.WriteDWORD(AseConstants.HeaderFlag_LayerOpacityValid |
                         AseConstants.HeaderFlag_GroupOpacityValid);

            w.WriteWORD(100); // deprecated speed – set to 100 ms
            w.WriteDWORD(0);  // reserved
            w.WriteDWORD(0);  // reserved
            w.WriteByte((byte)sprite.TransparentIndex);
            w.WritePad(3);
            ushort numColors = (ushort)(sprite.GetPalette(0).Count);
            w.WriteWORD(numColors);
            w.WriteByte((byte)sprite.PixelRatio.Width);
            w.WriteByte((byte)sprite.PixelRatio.Height);
            w.WriteSHORT((short)sprite.Grid.OriginX);
            w.WriteSHORT((short)sprite.Grid.OriginY);
            w.WriteWORD((ushort)sprite.Grid.TileWidth);
            w.WriteWORD((ushort)sprite.Grid.TileHeight);
            w.WritePad(84); // reserved
        }

        // ── Frame ─────────────────────────────────────────────────────────

        private void WriteFrame(AseWriter w, Sprite sprite, int frameIdx)
        {
            long frameStart = w.Position;

            // Reserve frame header
            w.WritePad(AseConstants.FrameHeaderSize);

            // Collect chunks
            var chunks = new List<byte[]>();

            if (frameIdx == 0)
            {
                // Tilesets must come before layers so layer chunks can ref them
                foreach (var ts in sprite.Tilesets)
                    chunks.Add(BuildTilesetChunk(ts, sprite));

                // Layers (all frames share the same layer definition from frame 0)
                foreach (var layer in _layerIndex)
                    chunks.Add(BuildLayerChunk(layer, frameIdx == 0));

                // Palette
                chunks.Add(BuildPaletteChunk(sprite.GetPalette(0)));

                // Tags
                if (sprite.Tags.Count > 0)
                    chunks.AddRange(BuildTagsChunks(sprite));

                // Slices
                foreach (var slice in sprite.Slices.All)
                    chunks.Add(BuildSliceChunk(slice));
            }
            else
            {
                // Check if palette changed at this frame
                foreach (var p in sprite.Palettes)
                {
                    if (p.Frame == frameIdx)
                        chunks.Add(BuildPaletteChunk(p));
                }
            }

            // Cels for this frame
            foreach (var (layer, cel) in sprite.CelsAtFrame(frameIdx))
            {
                int layerIndex = _layerIndex.IndexOf(layer);
                if (layerIndex < 0) continue;
                chunks.Add(BuildCelChunk(cel, layerIndex, sprite.ColorMode));

                // User data after cel
                if (cel.UserData.HasText || cel.UserData.HasColor)
                    chunks.Add(BuildUserDataChunk(cel.UserData));
            }

            // Write all chunks
            foreach (var chunk in chunks)
                w.WriteBytes(chunk);

            long frameEnd = w.Position;
            uint frameBytes = (uint)(frameEnd - frameStart);

            // Patch frame header
            w.Seek(frameStart);
            w.WriteDWORD(frameBytes);
            w.WriteWORD(AseConstants.FrameMagic);
            w.WriteWORD(0xFFFF); // old chunk count → use new field
            w.WriteWORD((ushort)sprite.GetFrame(frameIdx).DurationMs);
            w.WritePad(2);
            w.WriteDWORD((uint)chunks.Count);

            w.Seek(frameEnd);
        }

        // ── Layer chunk ───────────────────────────────────────────────────

        private byte[] BuildLayerChunk(Layer layer, bool isFirstFrame)
        {
            int childLevel = ComputeChildLevel(layer);

            using var ms = new MemoryStream();
            using var w = new AseWriter(ms, leaveOpen: true);

            ushort layerType = layer switch
            {
                LayerGroup => AseConstants.LayerType_Group,
                LayerTilemap => AseConstants.LayerType_Tilemap,
                _ => AseConstants.LayerType_Image,
            };

            WriteChunkHeader(w, AseConstants.Chunk_Layer, () =>
            {
                w.WriteWORD((ushort)layer.Flags);
                w.WriteWORD(layerType);
                w.WriteWORD((ushort)childLevel);
                w.WriteWORD(0); // default width (ignored)
                w.WriteWORD(0); // default height (ignored)
                w.WriteWORD((ushort)layer.BlendMode);
                w.WriteByte(layer.Opacity);
                w.WritePad(3);
                w.WriteSTRING(layer.Name);

                if (layer is LayerTilemap lt && lt.Tileset != null)
                {
                    int tsIdx = 0;
                    if (lt.Sprite != null)
                        tsIdx = FindTilesetIndex(lt.Sprite.Tilesets, lt.Tileset);
                    w.WriteDWORD((uint)Math.Max(0, tsIdx));
                }
            });

            // User data after layer chunk
            if (layer.UserData.HasText || layer.UserData.HasColor)
            {
                using var ms2 = new MemoryStream();
                using var w2 = new AseWriter(ms2, leaveOpen: true);
                WriteChunkHeader(w2, AseConstants.Chunk_UserData, () =>
                    WriteUserDataBody(w2, layer.UserData));
                w.WriteBytes(ms2.ToArray());
            }

            return ms.ToArray();
        }

        private int ComputeChildLevel(Layer layer)
        {
            int level = 0;
            var p = layer.Parent;
            while (p != null && p.Name != "__root__")
            {
                level++;
                p = p.Parent;
            }
            return level;
        }

        // ── Cel chunk ─────────────────────────────────────────────────────

        private static byte[] BuildCelChunk(Cel cel, int layerIndex, ColorMode colorMode)
        {
            using var ms = new MemoryStream();
            using var w = new AseWriter(ms, leaveOpen: true);

            WriteChunkHeader(w, AseConstants.Chunk_Cel, () =>
            {
                w.WriteWORD((ushort)layerIndex);
                w.WriteSHORT((short)cel.X);
                w.WriteSHORT((short)cel.Y);
                w.WriteByte(cel.Opacity);

                if (cel.IsLinked)
                {
                    w.WriteWORD(AseConstants.CelType_Linked);
                    w.WriteSHORT(cel.ZIndex);
                    w.WritePad(5);
                    w.WriteWORD((ushort)(cel.LinkedToFrame ?? 0));
                }
                else
                {
                    var image = cel.Data?.Image;
                    if (image == null) return;

                    bool isTilemap = image.ColorMode == ColorMode.Tilemap;
                    ushort celType = isTilemap
                        ? AseConstants.CelType_CompressedTilemap
                        : AseConstants.CelType_CompressedImage;

                    w.WriteWORD(celType);
                    w.WriteSHORT(cel.ZIndex);
                    w.WritePad(5);

                    if (isTilemap)
                    {
                        // Compressed tilemap
                        w.WriteWORD((ushort)image.Width);
                        w.WriteWORD((ushort)image.Height);
                        w.WriteWORD(32);              // bits per tile
                        w.WriteDWORD(TileConstants.IndexMask); // tile ID mask
                        w.WriteDWORD((uint)TileFlags.FlipX);
                        w.WriteDWORD((uint)TileFlags.FlipY);
                        w.WriteDWORD((uint)TileFlags.Rotate90);
                        w.WritePad(10);
                        byte[] tileData = CompressBytes(image.Data.ToArray());
                        w.WriteBytes(tileData);
                    }
                    else
                    {
                        // Compressed image
                        w.WriteWORD((ushort)image.Width);
                        w.WriteWORD((ushort)image.Height);
                        byte[] pixelData = CompressBytes(image.Data.ToArray());
                        w.WriteBytes(pixelData);
                    }
                }
            });

            return ms.ToArray();
        }

        // ── Palette chunk ─────────────────────────────────────────────────

        private static byte[] BuildPaletteChunk(Palette palette)
        {
            using var ms = new MemoryStream();
            using var w = new AseWriter(ms, leaveOpen: true);

            WriteChunkHeader(w, AseConstants.Chunk_Palette, () =>
            {
                uint count = (uint)palette.Count;
                w.WriteDWORD(count);
                w.WriteDWORD(0);          // from index
                w.WriteDWORD(count - 1);  // to index
                w.WritePad(8);

                for (int i = 0; i < palette.Count; i++)
                {
                    var c = palette.GetColor(i);
                    w.WriteWORD(0); // entry flags (no name)
                    w.WriteByte(c.R);
                    w.WriteByte(c.G);
                    w.WriteByte(c.B);
                    w.WriteByte(c.A);
                }
            });

            return ms.ToArray();
        }

        // ── Tags chunks ───────────────────────────────────────────────────

        private static List<byte[]> BuildTagsChunks(Sprite sprite)
        {
            var results = new List<byte[]>();

            // Build the tags chunk
            using var ms = new MemoryStream();
            using var w = new AseWriter(ms, leaveOpen: true);

            WriteChunkHeader(w, AseConstants.Chunk_Tags, () =>
            {
                w.WriteWORD((ushort)sprite.Tags.Count);
                w.WritePad(8);

                foreach (var tag in sprite.Tags.All)
                {
                    w.WriteWORD((ushort)tag.FromFrame);
                    w.WriteWORD((ushort)tag.ToFrame);
                    w.WriteByte((byte)tag.AniDir);
                    w.WriteWORD((ushort)tag.Repeat);
                    w.WritePad(6);
                    // Deprecated color bytes
                    w.WriteByte(tag.Color.R);
                    w.WriteByte(tag.Color.G);
                    w.WriteByte(tag.Color.B);
                    w.WriteByte(0); // extra zero
                    w.WriteSTRING(tag.Name);
                }
            });
            results.Add(ms.ToArray());

            // User data for each tag (one per tag, in order)
            foreach (var tag in sprite.Tags.All)
            {
                results.Add(BuildUserDataChunk(tag.UserData));
            }

            return results;
        }

        // ── Slice chunk ───────────────────────────────────────────────────

        private static byte[] BuildSliceChunk(Slice slice)
        {
            // Determine flags by inspecting first key
            bool has9Patch = false;
            bool hasPivot = false;
            foreach (var key in slice.Keys)
            {
                if (key.Has9Slices) has9Patch = true;
                if (key.HasPivot) hasPivot = true;
            }

            using var ms = new MemoryStream();
            using var w = new AseWriter(ms, leaveOpen: true);

            WriteChunkHeader(w, AseConstants.Chunk_Slice, () =>
            {
                uint flags = (has9Patch ? AseConstants.SliceFlag_9Patch : 0u)
                           | (hasPivot ? AseConstants.SliceFlag_Pivot : 0u);

                w.WriteDWORD((uint)slice.Keys.Count);
                w.WriteDWORD(flags);
                w.WriteDWORD(0); // reserved
                w.WriteSTRING(slice.Name);

                foreach (var key in slice.Keys)
                {
                    w.WriteDWORD((uint)key.Frame);
                    w.WriteLONG(key.X);
                    w.WriteLONG(key.Y);
                    w.WriteDWORD((uint)key.Width);
                    w.WriteDWORD((uint)key.Height);

                    if (has9Patch)
                    {
                        w.WriteLONG(key.CenterX);
                        w.WriteLONG(key.CenterY);
                        w.WriteDWORD((uint)key.CenterWidth);
                        w.WriteDWORD((uint)key.CenterHeight);
                    }
                    if (hasPivot)
                    {
                        w.WriteLONG(key.PivotX);
                        w.WriteLONG(key.PivotY);
                    }
                }
            });

            // User data after slice
            if (slice.UserData.HasText || slice.UserData.HasColor)
            {
                using var ms2 = new MemoryStream();
                using var w2 = new AseWriter(ms2, leaveOpen: true);
                WriteChunkHeader(w2, AseConstants.Chunk_UserData, () =>
                    WriteUserDataBody(w2, slice.UserData));
                w.WriteBytes(ms2.ToArray());
            }

            return ms.ToArray();
        }

        // ── Tileset chunk ─────────────────────────────────────────────────

        private static byte[] BuildTilesetChunk(Tileset tileset, Sprite sprite)
        {
            int tsIdx = FindTilesetIndex(sprite.Tilesets, tileset);

            using var ms = new MemoryStream();
            using var w = new AseWriter(ms, leaveOpen: true);

            WriteChunkHeader(w, AseConstants.Chunk_Tileset, () =>
            {
                w.WriteDWORD((uint)Math.Max(0, tsIdx));
                // Flags: embed tiles (2) + empty tile is index 0 (4)
                w.WriteDWORD(AseConstants.TilesetFlag_EmbedTiles | AseConstants.TilesetFlag_EmptyTileIs0);
                w.WriteDWORD((uint)tileset.Count);
                w.WriteWORD((ushort)tileset.TileWidth);
                w.WriteWORD((ushort)tileset.TileHeight);
                w.WriteSHORT((short)tileset.BaseIndex);
                w.WritePad(14);
                w.WriteSTRING(tileset.Name);

                // Embed all tiles compressed
                int bytesPerTile = tileset.TileWidth * tileset.TileHeight
                                 * Image.GetBytesPerPixel(tileset.ColorMode);
                byte[] allTileData = new byte[tileset.Count * bytesPerTile];
                for (int t = 0; t < tileset.Count; t++)
                {
                    tileset.GetTile(t).Data.ToArray()
                           .AsSpan().CopyTo(allTileData.AsSpan(t * bytesPerTile));
                }
                byte[] compressed = CompressBytes(allTileData);
                w.WriteDWORD((uint)compressed.Length);
                w.WriteBytes(compressed);
            });

            // User data after tileset
            if (tileset.UserData.HasText || tileset.UserData.HasColor)
            {
                using var ms2 = new MemoryStream();
                using var w2 = new AseWriter(ms2, leaveOpen: true);
                WriteChunkHeader(w2, AseConstants.Chunk_UserData, () =>
                    WriteUserDataBody(w2, tileset.UserData));
                w.WriteBytes(ms2.ToArray());
            }

            return ms.ToArray();
        }

        // ── User data ─────────────────────────────────────────────────────

        private static byte[] BuildUserDataChunk(UserData ud)
        {
            using var ms = new MemoryStream();
            using var w = new AseWriter(ms, leaveOpen: true);
            WriteChunkHeader(w, AseConstants.Chunk_UserData, () => WriteUserDataBody(w, ud));
            return ms.ToArray();
        }

        private static void WriteUserDataBody(AseWriter w, UserData ud)
        {
            uint flags = 0;
            if (ud.HasText) flags |= AseConstants.UserDataFlag_HasText;
            if (ud.HasColor) flags |= AseConstants.UserDataFlag_HasColor;
            w.WriteDWORD(flags);
            if (ud.HasText) w.WriteSTRING(ud.Text);
            if (ud.HasColor && ud.Color.HasValue)
            {
                var c = new Rgba32(ud.Color.Value);
                w.WriteByte(c.R);
                w.WriteByte(c.G);
                w.WriteByte(c.B);
                w.WriteByte(c.A);
            }
        }

        // ── Chunk header helper ───────────────────────────────────────────

        /// <summary>
        /// Writes a chunk header + body via callback, then patches the size DWORD.
        /// </summary>
        private static void WriteChunkHeader(AseWriter w, ushort chunkType, Action writeBody)
        {
            long start = w.Position;
            w.WriteDWORD(0);         // placeholder size
            w.WriteWORD(chunkType);
            writeBody();
            long end = w.Position;
            uint chunkSize = (uint)(end - start);
            w.Seek(start);
            w.WriteDWORD(chunkSize);
            w.Seek(end);
        }

        // ── Compression helper ────────────────────────────────────────────

        private static byte[] CompressBytes(byte[] data)
        {
            using var ms = new MemoryStream();
            using (var zlib = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                zlib.Write(data, 0, data.Length);
            return ms.ToArray();
        }

        // ── Layer index helpers ───────────────────────────────────────────

        /// <summary>
        /// Linear search for a tileset inside an <see cref="IReadOnlyList{T}"/>,
        /// since the interface doesn't expose IndexOf.
        /// </summary>
        private static int FindTilesetIndex(IReadOnlyList<Tileset> list, Tileset? target)
        {
            if (target == null) return 0;
            for (int i = 0; i < list.Count; i++)
                if (ReferenceEquals(list[i], target)) return i;
            return 0;
        }

        private static void BuildLayerIndex(LayerGroup root, List<Layer> index)
        {
            foreach (var layer in root.Layers)
            {
                index.Add(layer);
                if (layer is LayerGroup grp)
                    BuildLayerIndex(grp, index);
            }
        }
    }
}