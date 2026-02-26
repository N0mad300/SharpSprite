using System.IO.Compression;
using SharpSprite.Core.Document;

namespace SharpSprite.Infrastructure.Ase
{
    /// <summary>
    /// Reads an .ase / .aseprite binary file and produces a
    /// <see cref="SharpSprite.Core.Document.Document"/> (our document model).
    ///
    /// Supports:
    /// <list type="bullet">
    ///   <item>RGBA, Grayscale, and Indexed color modes</item>
    ///   <item>All chunk types: Layer (0x2004), Cel (0x2005), Palette (0x2019),
    ///         Tags (0x2018), Slices (0x2022), Tileset (0x2023), UserData (0x2020),
    ///         ColorProfile (0x2007), ExternalFiles (0x2008), CelExtra (0x2006)</item>
    ///   <item>Compressed (ZLIB) and raw image cels</item>
    ///   <item>Linked cels</item>
    ///   <item>Group / tilemap layer hierarchies</item>
    ///   <item>Old palette chunks (0x0004 / 0x0011) for backward compat</item>
    /// </list>
    /// </summary>
    public sealed class AseDecoder
    {
        // ── Public entry points ───────────────────────────────────────────

        /// <summary>Decode from a file path.</summary>
        public static Document DecodeFile(string path)
        {
            using var fs = File.OpenRead(path);
            var doc = DecodeStream(fs);
            doc.FilePath = path;
            doc.IsModified = false;
            return doc;
        }

        /// <summary>Decode from any readable <see cref="Stream"/>.</summary>
        public static Document DecodeStream(Stream stream)
        {
            using var r = new AseReader(stream, leaveOpen: true);
            return new AseDecoder().Decode(r);
        }

        // ── Internal state ────────────────────────────────────────────────

        // Flat list of layers in file order (used to resolve cel layer indices)
        private readonly List<Layer> _layerIndex = new();

        // The "last chunk" that can receive a following User Data chunk
        private object? _lastUserDataTarget;

        // After a Tags chunk we queue user data chunks for each tag
        private Queue<Tag>? _pendingTagUserData;

        // Flag from header: layer opacity is valid
        private bool _headerLayerOpacityValid;
        private bool _headerGroupOpacityValid;

        // New palette flag: set when 0x2019 is found, suppresses old palette chunks
        private bool _foundNewPalette;

        // ── Main decode ───────────────────────────────────────────────────

        private Document Decode(AseReader r)
        {
            // ── File header ───────────────────────────────────────────────
            uint fileSize = r.ReadDWORD();
            ushort magic = r.ReadWORD();
            if (magic != AseConstants.FileMagic)
                throw new InvalidDataException($"Not an Aseprite file (bad magic 0x{magic:X4}).");

            ushort frameCount = r.ReadWORD();
            ushort width = r.ReadWORD();
            ushort height = r.ReadWORD();
            ushort colorDepth = r.ReadWORD();

            var colorMode = colorDepth switch
            {
                AseConstants.ColorDepth_RGBA => ColorMode.Rgba,
                AseConstants.ColorDepth_Grayscale => ColorMode.Grayscale,
                AseConstants.ColorDepth_Indexed => ColorMode.Indexed,
                _ => throw new InvalidDataException($"Unknown color depth {colorDepth}.")
            };

            uint flags = r.ReadDWORD();
            _headerLayerOpacityValid = (flags & AseConstants.HeaderFlag_LayerOpacityValid) != 0;
            _headerGroupOpacityValid = (flags & AseConstants.HeaderFlag_GroupOpacityValid) != 0;

            ushort speed = r.ReadWORD(); // deprecated; used as fallback
            r.Skip(4 + 4);                    // two reserved DWORDs
            byte transparentIndex = r.ReadByte();
            r.Skip(3);                        // ignore 3 bytes
            ushort numColors = r.ReadWORD(); // 0 = 256 for old sprites
            byte pixelW = r.ReadByte();
            byte pixelH = r.ReadByte();
            short gridX = r.ReadSHORT();
            short gridY = r.ReadSHORT();
            ushort gridW = r.ReadWORD();
            ushort gridH = r.ReadWORD();
            r.Skip(84);                       // reserved for future

            // ── Build sprite ──────────────────────────────────────────────
            var sprite = new Sprite(width, height, colorMode)
            {
                TransparentIndex = transparentIndex,
                PixelRatio = (pixelW > 0 && pixelH > 0)
                    ? new PixelRatio(pixelW, pixelH)
                    : PixelRatio.Square,
                Grid = new Grid(
                    gridW > 0 ? gridW : 16,
                    gridH > 0 ? gridH : 16,
                    gridX, gridY),
            };

            // Seed frame 0 duration from deprecated "speed"; overridden per-frame
            sprite.GetFrame(0).DurationMs = speed > 0 ? speed : 100;

            // ── Frames ────────────────────────────────────────────────────
            for (int frameIdx = 0; frameIdx < frameCount; frameIdx++)
            {
                if (frameIdx > 0)
                    sprite.InsertFrames(frameIdx - 1, 1, speed > 0 ? speed : 100);

                ReadFrame(r, sprite, frameIdx, speed);
            }

            // ── Wrap in Document ──────────────────────────────────────────
            var doc = new Document(sprite);
            doc.IsModified = false;
            return doc;
        }

        // ── Frame ─────────────────────────────────────────────────────────

        private void ReadFrame(AseReader r, Sprite sprite, int frameIdx, ushort defaultSpeed)
        {
            long frameStart = r.Position;

            uint frameBytes = r.ReadDWORD();
            ushort frameMagic = r.ReadWORD();
            if (frameMagic != AseConstants.FrameMagic)
                throw new InvalidDataException($"Bad frame magic at frame {frameIdx}.");

            ushort oldChunkCount = r.ReadWORD();
            ushort durationMs = r.ReadWORD();
            r.Skip(2);                            // reserved
            uint newChunkCount = r.ReadDWORD();

            // Apply frame duration
            if (durationMs > 0)
                sprite.GetFrame(frameIdx).DurationMs = durationMs;

            uint chunkCount = newChunkCount != 0 ? newChunkCount
                            : (oldChunkCount == 0xFFFF ? uint.MaxValue : oldChunkCount);

            long frameEnd = frameStart + frameBytes;

            for (uint c = 0; c < chunkCount && r.Position < frameEnd; c++)
            {
                long chunkStart = r.Position;
                uint chunkSize = r.ReadDWORD();
                ushort chunkType = r.ReadWORD();
                long dataStart = r.Position;
                long dataEnd = chunkStart + chunkSize;

                ReadChunk(r, sprite, frameIdx, chunkType, (int)(chunkSize - AseConstants.ChunkHeaderSize));

                // Always advance to end of chunk to be safe
                if (r.Position != dataEnd)
                    r.Seek(dataEnd);
            }

            // Ensure we are at the end of the frame
            r.Seek(frameEnd);
        }

        // ── Chunk dispatcher ──────────────────────────────────────────────

        private void ReadChunk(AseReader r, Sprite sprite, int frameIdx, ushort type, int dataLen)
        {
            switch (type)
            {
                case AseConstants.Chunk_OldPalette0004:
                    if (!_foundNewPalette) ReadOldPalette0004(r, sprite);
                    break;

                case AseConstants.Chunk_OldPalette0011:
                    if (!_foundNewPalette) ReadOldPalette0011(r, sprite);
                    break;

                case AseConstants.Chunk_Layer:
                    ReadLayerChunk(r, sprite);
                    break;

                case AseConstants.Chunk_Cel:
                    ReadCelChunk(r, sprite, frameIdx, dataLen);
                    break;

                case AseConstants.Chunk_CelExtra:
                    ReadCelExtraChunk(r);
                    break;

                case AseConstants.Chunk_ColorProfile:
                    // We don't apply color profiles, just skip
                    break;

                case AseConstants.Chunk_ExternalFiles:
                    ReadExternalFilesChunk(r);
                    break;

                case AseConstants.Chunk_Tags:
                    ReadTagsChunk(r, sprite);
                    break;

                case AseConstants.Chunk_Palette:
                    ReadPaletteChunk(r, sprite, frameIdx);
                    _foundNewPalette = true;
                    break;

                case AseConstants.Chunk_UserData:
                    ReadUserDataChunk(r);
                    break;

                case AseConstants.Chunk_Slice:
                    ReadSliceChunk(r, sprite);
                    break;

                case AseConstants.Chunk_Tileset:
                    ReadTilesetChunk(r, sprite);
                    break;

                default:
                    // Unknown chunk – skip silently
                    break;
            }
        }

        // ── Old palette 0x0004 ────────────────────────────────────────────

        private static void ReadOldPalette0004(AseReader r, Sprite sprite)
        {
            ushort packetCount = r.ReadWORD();
            var palette = sprite.GetPalette(0);
            int entryIdx = 0;
            for (int p = 0; p < packetCount; p++)
            {
                byte skip = r.ReadByte();
                byte count = r.ReadByte();
                entryIdx += skip;
                int n = count == 0 ? 256 : count;
                for (int i = 0; i < n; i++, entryIdx++)
                {
                    byte red = r.ReadByte();
                    byte green = r.ReadByte();
                    byte blue = r.ReadByte();
                    if (entryIdx < palette.Count)
                        palette.SetColor(entryIdx, new Rgba32(red, green, blue, 255));
                }
            }
        }

        // ── Old palette 0x0011 (0-63 range) ──────────────────────────────

        private static void ReadOldPalette0011(AseReader r, Sprite sprite)
        {
            ushort packetCount = r.ReadWORD();
            var palette = sprite.GetPalette(0);
            int entryIdx = 0;
            for (int p = 0; p < packetCount; p++)
            {
                byte skip = r.ReadByte();
                byte count = r.ReadByte();
                entryIdx += skip;
                int n = count == 0 ? 256 : count;
                for (int i = 0; i < n; i++, entryIdx++)
                {
                    byte red = (byte)(r.ReadByte() * 255 / 63);
                    byte green = (byte)(r.ReadByte() * 255 / 63);
                    byte blue = (byte)(r.ReadByte() * 255 / 63);
                    if (entryIdx < palette.Count)
                        palette.SetColor(entryIdx, new Rgba32(red, green, blue, 255));
                }
            }
        }

        // ── Layer chunk 0x2004 ────────────────────────────────────────────

        private void ReadLayerChunk(AseReader r, Sprite sprite)
        {
            ushort flags = r.ReadWORD();
            ushort layerType = r.ReadWORD();
            ushort childLevel = r.ReadWORD();
            r.Skip(2 + 2);            // default width/height (ignored)
            ushort blendMode = r.ReadWORD();
            byte opacity = r.ReadByte();
            r.Skip(3);                // reserved
            string name = r.ReadSTRING();

            Layer layer;
            switch (layerType)
            {
                case AseConstants.LayerType_Group:
                    layer = new LayerGroup(name);
                    break;
                case AseConstants.LayerType_Tilemap:
                    var tl = new LayerTilemap(name);
                    uint tilesetIdx = r.ReadDWORD();
                    if (tilesetIdx < (uint)sprite.Tilesets.Count)
                        tl.Tileset = sprite.Tilesets[(int)tilesetIdx];
                    layer = tl;
                    break;
                default: // Normal image layer
                    layer = new LayerImage(name);
                    break;
            }

            // Apply flags
            layer.Flags = (LayerFlags)flags;

            // Apply blend mode
            layer.BlendMode = (BlendMode)Math.Min((int)BlendMode.Divide, (int)blendMode);

            // Apply opacity only if header says it is valid
            if (_headerLayerOpacityValid || layer.IsGroup && _headerGroupOpacityValid)
                layer.Opacity = opacity;

            // Resolve hierarchy using childLevel
            InsertLayerIntoHierarchy(layer, childLevel, sprite);
            _layerIndex.Add(layer);
            _lastUserDataTarget = layer;
        }

        /// <summary>
        /// Inserts <paramref name="layer"/> into the sprite's layer tree
        /// according to the Aseprite child-level system.
        /// </summary>
        private static void InsertLayerIntoHierarchy(Layer layer, ushort childLevel, Sprite sprite)
        {
            if (childLevel == 0)
            {
                // Top-level layer
                sprite.AddLayer(layer);
            }
            else
            {
                // Walk the RootGroup's flattened layers to find the parent group
                var parent = FindParentGroup(sprite.RootGroup, childLevel);
                if (parent != null)
                    parent.AddLayer(layer);
                else
                    sprite.AddLayer(layer); // fallback
            }
        }

        /// <summary>
        /// Returns the deepest LayerGroup currently at the given child level.
        /// </summary>
        private static LayerGroup? FindParentGroup(LayerGroup root, int targetChildLevel)
        {
            // The parent group is the last group at (targetChildLevel - 1)
            LayerGroup? candidate = null;
            FindParentGroupRecursive(root, 0, targetChildLevel - 1, ref candidate);
            return candidate ?? root;
        }

        private static void FindParentGroupRecursive(
            LayerGroup group, int currentLevel, int targetLevel, ref LayerGroup? result)
        {
            if (currentLevel == targetLevel)
                result = group;

            foreach (var child in group.Layers)
            {
                if (child is LayerGroup grp)
                    FindParentGroupRecursive(grp, currentLevel + 1, targetLevel, ref result);
            }
        }

        // ── Cel chunk 0x2005 ─────────────────────────────────────────────

        private void ReadCelChunk(AseReader r, Sprite sprite, int frameIdx, int dataLen)
        {
            ushort layerIndex = r.ReadWORD();
            short x = r.ReadSHORT();
            short y = r.ReadSHORT();
            byte opacity = r.ReadByte();
            ushort celType = r.ReadWORD();
            short zIndex = r.ReadSHORT();
            r.Skip(5); // reserved

            if (layerIndex >= _layerIndex.Count) return;
            var layer = _layerIndex[layerIndex];

            Cel cel;

            switch (celType)
            {
                case AseConstants.CelType_RawImage:
                    {
                        ushort w = r.ReadWORD();
                        ushort h = r.ReadWORD();
                        var img = new Image(w, h, sprite.ColorMode);
                        int pixelBytes = w * h * Image.GetBytesPerPixel(sprite.ColorMode);
                        byte[] raw = r.ReadBytes(pixelBytes);
                        raw.AsSpan().CopyTo(img.DataWritable);
                        cel = new Cel(frameIdx, img, x, y);
                        break;
                    }

                case AseConstants.CelType_Linked:
                    {
                        ushort linkedFrame = r.ReadWORD();
                        cel = Cel.CreateLinked(frameIdx, linkedFrame);
                        break;
                    }

                case AseConstants.CelType_CompressedImage:
                    {
                        ushort w = r.ReadWORD();
                        ushort h = r.ReadWORD();
                        var img = new Image(w, h, sprite.ColorMode);
                        DecompressPixels(r, img);
                        cel = new Cel(frameIdx, img, x, y);
                        break;
                    }

                case AseConstants.CelType_CompressedTilemap:
                    {
                        ushort tilesW = r.ReadWORD();
                        ushort tilesH = r.ReadWORD();
                        ushort bitsPerTile = r.ReadWORD(); // always 32
                        uint tileIdMask = r.ReadDWORD();
                        uint flipXMask = r.ReadDWORD();
                        uint flipYMask = r.ReadDWORD();
                        uint diagFlipMask = r.ReadDWORD();
                        r.Skip(10);                        // reserved
                                                           // Tilemap stored as DWORD tiles compressed with ZLIB
                        var img = new Image(tilesW, tilesH, ColorMode.Tilemap);
                        DecompressTiles(r, img);
                        cel = new Cel(frameIdx, img, x, y);
                        break;
                    }

                default:
                    return; // unknown cel type
            }

            cel.Opacity = opacity;
            cel.ZIndex = zIndex;

            // Attach cel to its layer
            if (layer is LayerImage li)
                li.AddCel(cel);
            else if (layer is LayerTilemap lt)
                lt.AddCel(cel);

            _lastUserDataTarget = cel;
        }

        // ── Cel extra 0x2006 ──────────────────────────────────────────────

        private void ReadCelExtraChunk(AseReader r)
        {
            // We store cel extra data loosely; just read and ignore for now
            r.ReadDWORD(); // flags
            r.ReadFIXED(); // precise X
            r.ReadFIXED(); // precise Y
            r.ReadFIXED(); // width
            r.ReadFIXED(); // height
            r.Skip(16);
        }

        // ── External files 0x2008 ─────────────────────────────────────────

        private static void ReadExternalFilesChunk(AseReader r)
        {
            uint count = r.ReadDWORD();
            r.Skip(8); // reserved
            for (uint i = 0; i < count; i++)
            {
                r.ReadDWORD(); // entry ID
                r.ReadByte();  // type
                r.Skip(7);     // reserved
                r.ReadSTRING(); // filename or extension ID
            }
            // We don't currently model external files in our document; silently read.
        }

        // ── Tags chunk 0x2018 ─────────────────────────────────────────────

        private void ReadTagsChunk(AseReader r, Sprite sprite)
        {
            ushort tagCount = r.ReadWORD();
            r.Skip(8); // reserved

            _pendingTagUserData = new Queue<Tag>();

            for (int i = 0; i < tagCount; i++)
            {
                ushort from = r.ReadWORD();
                ushort to = r.ReadWORD();
                byte aniDir = r.ReadByte();
                ushort repeat = r.ReadWORD();
                r.Skip(6);              // reserved
                byte[] rgb = r.ReadBytes(3); // deprecated color bytes
                r.ReadByte();           // extra zero byte
                string name = r.ReadSTRING();

                var tag = new Tag
                {
                    Name = name,
                    FromFrame = from,
                    ToFrame = to,
                    AniDir = (AniDir)Math.Min((int)AniDir.PingPongReverse, (int)aniDir),
                    Repeat = repeat,
                    Color = new Rgba32(rgb[0], rgb[1], rgb[2], 255),
                };
                sprite.Tags.Add(tag);
                _pendingTagUserData.Enqueue(tag);
            }

            // The tags chunk itself can also have user data; let the next
            // user data chunk (if any) be assigned to the tags chunk object.
            // Per spec: after Tags chunk, follow user data chunks for each tag.
            _lastUserDataTarget = null; // clear so the first user data goes to first tag
        }

        // ── Palette chunk 0x2019 ─────────────────────────────────────────

        private static void ReadPaletteChunk(AseReader r, Sprite sprite, int frameIdx)
        {
            uint newSize = r.ReadDWORD();
            uint fromIdx = r.ReadDWORD();
            uint toIdx = r.ReadDWORD();
            r.Skip(8); // reserved

            var palette = sprite.GetPalette(frameIdx);

            // Resize if needed
            if (newSize > 0 && newSize != (uint)palette.Count)
                palette.Resize((int)newSize);

            for (uint i = fromIdx; i <= toIdx; i++)
            {
                ushort entryFlags = r.ReadWORD();
                byte red = r.ReadByte();
                byte green = r.ReadByte();
                byte blue = r.ReadByte();
                byte alpha = r.ReadByte();
                if ((entryFlags & AseConstants.PaletteEntryFlag_HasName) != 0)
                    r.ReadSTRING(); // color name (we don't store it)
                if (i < (uint)palette.Count)
                    palette.SetColor((int)i, new Rgba32(red, green, blue, alpha));
            }
        }

        // ── User data chunk 0x2020 ────────────────────────────────────────

        private void ReadUserDataChunk(AseReader r)
        {
            uint flags = r.ReadDWORD();
            string text = (flags & AseConstants.UserDataFlag_HasText) != 0 ? r.ReadSTRING() : string.Empty;
            Rgba32? color = null;
            if ((flags & AseConstants.UserDataFlag_HasColor) != 0)
            {
                byte cr = r.ReadByte();
                byte cg = r.ReadByte();
                byte cb = r.ReadByte();
                byte ca = r.ReadByte();
                color = new Rgba32(cr, cg, cb, ca);
            }
            if ((flags & AseConstants.UserDataFlag_HasProperties) != 0)
            {
                // Read and discard properties (complex nested format)
                uint propSize = r.ReadDWORD();
                if (propSize >= 8)
                    r.Skip((int)propSize - 4); // -4 for the size field itself
            }

            // Dispatch user data to its target
            UserData? ud = null;

            // If there are pending tag user data, consume first tag
            if (_pendingTagUserData != null && _pendingTagUserData.Count > 0)
            {
                var tag = _pendingTagUserData.Dequeue();
                ud = tag.UserData;
                if (_pendingTagUserData.Count == 0)
                    _pendingTagUserData = null;
            }
            else if (_lastUserDataTarget != null)
            {
                ud = _lastUserDataTarget switch
                {
                    Layer l => l.UserData,
                    Cel c => c.UserData,
                    Slice s => s.UserData,
                    Tileset ts => ts.UserData,
                    _ => null,
                };
            }

            if (ud != null)
            {
                ud.Text = text;
                ud.Color = color.HasValue ? (uint?)color.Value.Packed : null;
            }
        }

        // ── Slice chunk 0x2022 ────────────────────────────────────────────

        private void ReadSliceChunk(AseReader r, Sprite sprite)
        {
            uint keyCount = r.ReadDWORD();
            uint sliceFlags = r.ReadDWORD();
            r.ReadDWORD(); // reserved
            string name = r.ReadSTRING();

            bool is9Patch = (sliceFlags & AseConstants.SliceFlag_9Patch) != 0;
            bool hasPivot = (sliceFlags & AseConstants.SliceFlag_Pivot) != 0;

            var slice = new Slice(name);
            sprite.Slices.Add(slice);

            for (uint k = 0; k < keyCount; k++)
            {
                uint frame = r.ReadDWORD();
                int sx = r.ReadLONG();
                int sy = r.ReadLONG();
                uint sw = r.ReadDWORD();
                uint sh = r.ReadDWORD();

                var key = new SliceKey
                {
                    Frame = (int)frame,
                    X = sx,
                    Y = sy,
                    Width = (int)sw,
                    Height = (int)sh,
                };

                if (is9Patch)
                {
                    key.Has9Slices = true;
                    key.CenterX = r.ReadLONG();
                    key.CenterY = r.ReadLONG();
                    key.CenterWidth = (int)r.ReadDWORD();
                    key.CenterHeight = (int)r.ReadDWORD();
                }
                if (hasPivot)
                {
                    key.HasPivot = true;
                    key.PivotX = r.ReadLONG();
                    key.PivotY = r.ReadLONG();
                }
                slice.AddKey(key);
            }

            _lastUserDataTarget = slice;
        }

        // ── Tileset chunk 0x2023 ─────────────────────────────────────────

        private void ReadTilesetChunk(AseReader r, Sprite sprite)
        {
            uint tilesetId = r.ReadDWORD();
            uint flags = r.ReadDWORD();
            uint tileCount = r.ReadDWORD();
            ushort tileWidth = r.ReadWORD();
            ushort tileHeight = r.ReadWORD();
            short baseIndex = r.ReadSHORT();
            r.Skip(14);
            string name = r.ReadSTRING();

            var tileset = new Tileset(tileWidth, tileHeight, sprite.ColorMode)
            {
                Name = name,
                BaseIndex = baseIndex,
            };

            if ((flags & AseConstants.TilesetFlag_ExternalLink) != 0)
            {
                r.ReadDWORD(); // external file entry ID
                r.ReadDWORD(); // tileset ID in external file
            }

            if ((flags & AseConstants.TilesetFlag_EmbedTiles) != 0)
            {
                uint dataLen = r.ReadDWORD();
                // tileCount tiles, each tileWidth * tileHeight pixels, compressed
                int totalPixels = (int)tileCount * tileWidth * tileHeight;
                int bytesPerPixel = Image.GetBytesPerPixel(sprite.ColorMode);
                byte[] decompressed = DecompressBytes(r, (int)dataLen, totalPixels * bytesPerPixel);

                // Tile 0 is already added by constructor; start from tile 0 and overwrite
                int bytesPerTile = tileWidth * tileHeight * bytesPerPixel;
                for (uint t = 0; t < tileCount; t++)
                {
                    var tileImg = new Image(tileWidth, tileHeight, sprite.ColorMode);
                    decompressed.AsSpan((int)(t * bytesPerTile), bytesPerTile)
                        .CopyTo(tileImg.DataWritable);

                    if (t == 0)
                    {
                        // Overwrite the empty tile
                        tileset.GetTile(0).DataWritable.Fill(0);
                        decompressed.AsSpan(0, bytesPerTile).CopyTo(tileset.GetTile(0).DataWritable);
                    }
                    else
                    {
                        // Ensure we have enough tiles
                        while (tileset.Count <= (int)t)
                            tileset.AddEmptyTile();
                        decompressed.AsSpan((int)(t * bytesPerTile), bytesPerTile)
                            .CopyTo(tileset.GetTile((int)t).DataWritable);
                    }
                }
            }

            sprite.AddTileset(tileset);
            _lastUserDataTarget = tileset;
        }

        // ── Decompression helpers ─────────────────────────────────────────

        /// <summary>
        /// Decompress ZLIB-compressed pixel data from the current stream position
        /// directly into <paramref name="img"/>'s pixel buffer.
        /// </summary>
        private static void DecompressPixels(AseReader r, Image img)
        {
            // The remainder of the chunk is ZLIB-compressed pixel data.
            // We need to decompress until we fill img.DataWritable.
            using var zlib = new ZLibStream(
                new AseReaderStream(r), CompressionMode.Decompress, leaveOpen: true);

            int totalBytes = img.DataWritable.Length;
            byte[] buf = img.DataWritable.ToArray(); // temp; will copy back
            int bytesRead = 0;
            while (bytesRead < totalBytes)
            {
                int n = zlib.Read(buf, bytesRead, totalBytes - bytesRead);
                if (n == 0) break;
                bytesRead += n;
            }
            buf.AsSpan().CopyTo(img.DataWritable);
        }

        /// <summary>
        /// Decompress ZLIB-compressed tilemap data.
        /// </summary>
        private static void DecompressTiles(AseReader r, Image img)
        {
            // Tilemap: DWORD per tile (uint32)
            using var zlib = new ZLibStream(
                new AseReaderStream(r), CompressionMode.Decompress, leaveOpen: true);

            int totalBytes = img.DataWritable.Length;
            byte[] buf = new byte[totalBytes];
            int bytesRead = 0;
            while (bytesRead < totalBytes)
            {
                int n = zlib.Read(buf, bytesRead, totalBytes - bytesRead);
                if (n == 0) break;
                bytesRead += n;
            }
            buf.AsSpan().CopyTo(img.DataWritable);
        }

        /// <summary>
        /// Decompress <paramref name="compressedLen"/> bytes from <paramref name="r"/>
        /// and return exactly <paramref name="expectedBytes"/> decompressed bytes.
        /// </summary>
        private static byte[] DecompressBytes(AseReader r, int compressedLen, int expectedBytes)
        {
            byte[] compressed = r.ReadBytes(compressedLen);
            using var ms = new MemoryStream(compressed);
            using var zlib = new ZLibStream(ms, CompressionMode.Decompress);
            byte[] result = new byte[expectedBytes];
            int offset = 0;
            while (offset < expectedBytes)
            {
                int n = zlib.Read(result, offset, expectedBytes - offset);
                if (n == 0) break;
                offset += n;
            }
            return result;
        }

        // ── Helper: stream adapter so ZLibStream can read from AseReader ──

        private sealed class AseReaderStream : Stream
        {
            private readonly AseReader _r;
            public AseReaderStream(AseReader r) => _r = r;
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count)
            {
                try
                {
                    byte[] data = _r.ReadBytes(count);
                    data.CopyTo(buffer, offset);
                    return data.Length;
                }
                catch (EndOfStreamException) { return 0; }
            }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}