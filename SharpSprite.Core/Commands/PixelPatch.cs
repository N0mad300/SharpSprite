using SharpSprite.Core.Document;

namespace SharpSprite.Core.Commands
{
    /// <summary>
    /// Records the "before" and "after" state of a single pixel, used to build
    /// a sparse patch that can be applied or reverted without storing a full
    /// image copy.
    /// </summary>
    public readonly struct PixelChange
    {
        public readonly int X;
        public readonly int Y;
        public readonly Rgba32 Before;
        public readonly Rgba32 After;

        public PixelChange(int x, int y, Rgba32 before, Rgba32 after)
        {
            X = x; Y = y; Before = before; After = after;
        }
    }

    /// <summary>
    /// A sparse set of per-pixel before/after records for a single stroke.
    /// Only the first paint on each (x,y) within a stroke records the original
    /// "before" colour; subsequent paints on the same pixel within the same
    /// stroke update only the "after" colour.  This keeps undo semantically
    /// correct while handling overlapping strokes cheaply.
    /// </summary>
    public sealed class PixelPatch
    {
        // Key = packed (x | y << 16) for fast lookup
        private readonly Dictionary<int, PixelChange> _changes = new();

        public IReadOnlyCollection<PixelChange> Changes => _changes.Values;
        public int Count => _changes.Count;

        /// <summary>
        /// Record that pixel (x, y) changed from <paramref name="before"/> to
        /// <paramref name="after"/>.  If (x, y) already has an entry, only
        /// the "after" colour is updated (the original "before" is preserved).
        /// </summary>
        public void Record(int x, int y, Rgba32 before, Rgba32 after)
        {
            int key = x | (y << 16);
            if (_changes.TryGetValue(key, out var existing))
            {
                // Preserve original before; update after
                _changes[key] = new PixelChange(x, y, existing.Before, after);
            }
            else
            {
                _changes[key] = new PixelChange(x, y, before, after);
            }
        }

        /// <summary>Apply "after" colours to <paramref name="image"/>.</summary>
        public void Apply(Image image)
        {
            foreach (var ch in _changes.Values)
                WritePixel(image, ch.X, ch.Y, ch.After);
        }

        /// <summary>Apply "before" colours to <paramref name="image"/>.</summary>
        public void Revert(Image image)
        {
            foreach (var ch in _changes.Values)
                WritePixel(image, ch.X, ch.Y, ch.Before);
        }

        /// <summary>
        /// Absorb all changes from <paramref name="other"/> into this patch.
        /// Used when merging two commands into one undo step.
        /// </summary>
        public void MergeFrom(PixelPatch other)
        {
            foreach (var ch in other._changes.Values)
                Record(ch.X, ch.Y, ch.Before, ch.After);
        }

        private static void WritePixel(Image image, int x, int y, Rgba32 color)
        {
            if (x < 0 || x >= image.Width || y < 0 || y >= image.Height) return;

            switch (image.ColorMode)
            {
                case ColorMode.Rgba:
                    image.SetPixelRgba(x, y, color);
                    break;
                case ColorMode.Grayscale:
                    // Treat the Rgba32 as a grey value
                    byte grey = (byte)((color.R * 77 + color.G * 150 + color.B * 29) >> 8);
                    image.SetPixelGrayscale(x, y, grey, color.A);
                    break;
                case ColorMode.Indexed:
                    // color.R is used as the palette index (set by tool)
                    image.SetPixelIndexed(x, y, color.R);
                    break;
            }
        }
    }
}
