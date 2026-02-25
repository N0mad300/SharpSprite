using SharpSprite.Core.Document;

namespace SharpSprite.Core.Commands
{
    /// <summary>
    /// Records a paint or erase stroke as a <see cref="PixelPatch"/> so the
    /// entire stroke can be undone in one step.
    ///
    /// The command is constructed with an empty patch at stroke-begin, pixels
    /// are recorded into it during the stroke via <see cref="RecordPixel"/>,
    /// and the final command is pushed to the <see cref="UndoStack"/> at
    /// stroke-end.
    ///
    /// Merging:
    ///   Two <see cref="PaintStrokeCommand"/>s on the same cel merge if they
    ///   carry the same <see cref="_mergeKey"/>.  The key is set per-stroke
    ///   (it changes each time the mouse button is pressed), so only pixels
    ///   within a single continuous drag merge together.
    /// </summary>
    public sealed class PaintStrokeCommand : IDocumentCommand
    {
        // ------------------------------------------------------------------
        // Fields
        // ------------------------------------------------------------------

        private readonly Document.Document _document;
        private readonly Image _image;
        private readonly PixelPatch _patch;

        /// <summary>
        /// Identifies which "stroke session" this command belongs to.
        /// Two commands merge only if their keys match.
        /// Set to a new unique value at the start of each mouse-down.
        /// </summary>
        private readonly int _mergeKey;

        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        public PaintStrokeCommand(
            Document.Document document,
            Image image,
            PixelPatch patch,
            int mergeKey,
            string name = "Pencil")
        {
            _document = document;
            _image = image;
            _patch = patch;
            _mergeKey = mergeKey;
            Name = name;
        }

        // ------------------------------------------------------------------
        // IDocumentCommand
        // ------------------------------------------------------------------

        public string Name { get; }

        public void Execute()
        {
            _patch.Apply(_image);
            _document.NotifyChanged(DocumentChangeKind.CelImageChanged);
        }

        public void Undo()
        {
            _patch.Revert(_image);
            _document.NotifyChanged(DocumentChangeKind.CelImageChanged);
        }

        public bool TryMerge(IDocumentCommand next)
        {
            // Only merge with another paint command on the same stroke session
            // targeting the same image.
            if (next is PaintStrokeCommand other &&
                other._mergeKey == _mergeKey &&
                other._image == _image)
            {
                _patch.MergeFrom(other._patch);
                return true;
            }
            return false;
        }

        // ------------------------------------------------------------------
        // Builder helper – called during a live stroke
        // ------------------------------------------------------------------

        /// <summary>
        /// Record a pixel that was painted during this stroke.
        /// Reads the current pixel from <see cref="_image"/> as the "before"
        /// value, then writes <paramref name="color"/> as the "after" value.
        /// Also immediately paints the pixel into the image so the canvas
        /// updates in real-time.
        /// </summary>
        public void RecordPixel(int x, int y, Rgba32 color)
        {
            if (x < 0 || x >= _image.Width || y < 0 || y >= _image.Height) return;

            Rgba32 before = _image.ColorMode == ColorMode.Rgba
                ? _image.GetPixelRgba(x, y)
                : Rgba32.Transparent; // simplified for non-RGBA modes

            _patch.Record(x, y, before, color);

            // Paint immediately for live preview
            if (_image.ColorMode == ColorMode.Rgba)
                _image.SetPixelRgba(x, y, color);
        }
    }
}
