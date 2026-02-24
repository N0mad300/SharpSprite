using System;
using System.Collections.Generic;
using System.Text;
using SharpSprite.Core.Document;

namespace SharpSprite.Core.Document
{
    /// <summary>
    /// Factory helpers for creating commonly-needed document configurations.
    /// </summary>
    public static class SpriteFactory
    {
        // ------------------------------------------------------------------
        // Blank sprites
        // ------------------------------------------------------------------

        public static Document CreateBlankRgba(int width, int height)
        {
            var doc = new Document(width, height, ColorMode.Rgba);
            AddDefaultLayer(doc.Sprite, ColorMode.Rgba);
            return doc;
        }

        public static Document CreateBlankIndexed(int width, int height, int paletteSize = 256)
        {
            var doc = new Document(width, height, ColorMode.Indexed);
            AddDefaultLayer(doc.Sprite, ColorMode.Indexed);
            return doc;
        }

        public static Document CreateBlankGrayscale(int width, int height)
        {
            var doc = new Document(width, height, ColorMode.Grayscale);
            AddDefaultLayer(doc.Sprite, ColorMode.Grayscale);
            return doc;
        }

        // ------------------------------------------------------------------
        // From existing sprite
        // ------------------------------------------------------------------

        public static Document FromSprite(Sprite sprite) => new(sprite);

        // ------------------------------------------------------------------
        // Deep clone
        // ------------------------------------------------------------------

        /// <summary>
        /// Produce a complete deep-clone of <paramref name="source"/>.
        /// The clone will have no linked cels; all cells are independent copies.
        /// </summary>
        public static Document Clone(Document source)
        {
            var original = source.Sprite;
            var clone = new Sprite(original.Width, original.Height, original.ColorMode)
            {
                PixelRatio = original.PixelRatio,
                Grid = original.Grid.Clone(),
                FilePath = original.FilePath,
            };
            clone.UserData.Text = original.UserData.Text;
            clone.UserData.Color = original.UserData.Color;

            // Frames
            // Frame 0 already exists; update its duration
            clone.GetFrame(0).DurationMs = original.GetFrame(0).DurationMs;
            for (int f = 1; f < original.FrameCount; f++)
                clone.InsertFrames(f - 1, 1, original.GetFrame(f).DurationMs);

            // Palettes
            foreach (var p in original.Palettes)
                clone.SetPalette(p.Clone());

            // Tilesets
            foreach (var ts in original.Tilesets)
                clone.AddTileset(ts.Clone());

            // Layers (recursive)
            CloneLayerGroup(original.RootGroup, clone.RootGroup, clone);

            // Tags
            foreach (var tag in original.Tags.All)
                clone.Tags.Add(tag.Clone());

            // Slices
            foreach (var slice in original.Slices.All)
                clone.Slices.Add(slice.Clone());

            return new Document(clone);
        }

        // ------------------------------------------------------------------
        // Private helpers
        // ------------------------------------------------------------------

        private static void AddDefaultLayer(Sprite sprite, ColorMode colorMode)
        {
            var layer = new LayerImage("Background");
            layer.IsBackground = true;
            sprite.AddLayer(layer);

            // Add a transparent cel for frame 0
            var img = new Image(sprite.Width, sprite.Height, colorMode);
            img.Clear();
            layer.AddCel(new Cel(0, img));
        }

        private static void CloneLayerGroup(LayerGroup source, LayerGroup target, Sprite targetSprite)
        {
            foreach (var srcLayer in source.Layers)
            {
                Layer cloned = srcLayer switch
                {
                    LayerImage li => CloneLayerImage(li),
                    LayerTilemap lt => CloneLayerTilemap(lt, targetSprite),
                    LayerGroup lg => CloneLayerGroupRecursive(lg, targetSprite),
                    _ => throw new NotSupportedException($"Unknown layer type: {srcLayer.GetType()}")
                };
                target.AddLayer(cloned);
            }
        }

        private static LayerImage CloneLayerImage(LayerImage src)
        {
            var dst = new LayerImage(src.Name) { Flags = src.Flags, Opacity = src.Opacity, BlendMode = src.BlendMode };
            dst.UserData.Text = src.UserData.Text;
            dst.UserData.Color = src.UserData.Color;
            foreach (var cel in src.Cels)
                dst.AddCel(cel.Clone());
            return dst;
        }

        private static LayerTilemap CloneLayerTilemap(LayerTilemap src, Sprite targetSprite)
        {
            var dst = new LayerTilemap(src.Name)
            {
                Flags = src.Flags,
                Opacity = src.Opacity,
                BlendMode = src.BlendMode,
                Grid = src.Grid.Clone(),
            };
            dst.UserData.Text = src.UserData.Text;
            dst.UserData.Color = src.UserData.Color;
            // Resolve tileset by ID in the target sprite
            if (src.Tileset != null)
                dst.Tileset = targetSprite.Tilesets.Count > 0
                    ? targetSprite.Tilesets[0] // simple heuristic; caller should fix up
                    : null;
            foreach (var cel in src.Cels)
                dst.AddCel(cel.Clone());
            return dst;
        }

        private static LayerGroup CloneLayerGroupRecursive(LayerGroup src, Sprite targetSprite)
        {
            var dst = new LayerGroup(src.Name) { Flags = src.Flags, Opacity = src.Opacity, BlendMode = src.BlendMode };
            dst.UserData.Text = src.UserData.Text;
            dst.UserData.Color = src.UserData.Color;
            CloneLayerGroup(src, dst, targetSprite);
            return dst;
        }
    }
}
