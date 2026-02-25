using Avalonia;
using SharpSprite.Core.Commands;
using SharpSprite.Core.Document;

namespace SharpSprite.App.Tools
{
    /// <summary>
    /// Passed to every tool event handler.  Provides:
    /// <list type="bullet">
    ///   <item>The active <see cref="Document"/> and its <see cref="UndoStack"/>.</item>
    ///   <item>The active <see cref="Layer"/> and <see cref="Frame"/> index.</item>
    ///   <item>The foreground and background colors.</item>
    ///   <item>The canvas transform so tools can convert screen→sprite coordinates.</item>
    ///   <item>A reference to the canvas control so tools can mutate its
    ///         display properties (e.g. PanOffset, Zoom for the Pan/Zoom tools).</item>
    /// </list>
    /// </summary>
    public sealed class ToolContext
    {
        // ------------------------------------------------------------------
        // Document access
        // ------------------------------------------------------------------

        public Document Document { get; init; } = null!;
        public UndoStack UndoStack { get; init; } = null!;
        public Layer? ActiveLayer { get; init; }
        public int ActiveFrame { get; init; }
        public Rgba32 ForegroundColor { get; init; }
        public Rgba32 BackgroundColor { get; init; }

        // ------------------------------------------------------------------
        // Canvas transform (screen → sprite)
        // ------------------------------------------------------------------

        /// <summary>
        /// The scale factor currently applied by <see cref="Controls.PixelCanvasControl"/>.
        /// Screen pixels / sprite pixels.
        /// </summary>
        public float CanvasScale { get; init; }

        /// <summary>
        /// The X offset (in screen pixels) of the sprite's top-left corner
        /// relative to the control's top-left corner.
        /// </summary>
        public float CanvasOffsetX { get; init; }

        /// <summary>Y offset counterpart of <see cref="CanvasOffsetX"/>.</summary>
        public float CanvasOffsetY { get; init; }

        // ------------------------------------------------------------------
        // Canvas control reference (for Pan/Zoom tools)
        // ------------------------------------------------------------------

        public Controls.PixelCanvasControl Canvas { get; init; } = null!;

        // ------------------------------------------------------------------
        // Coordinate conversion helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Convert a screen-space point (from a pointer event) to integer
        /// sprite-pixel coordinates.  Returns false if the point is outside
        /// the sprite bounds.
        /// </summary>
        public bool TryScreenToSprite(Point screenPt, out int sx, out int sy)
        {
            float spriteX = (float)(screenPt.X - CanvasOffsetX) / CanvasScale;
            float spriteY = (float)(screenPt.Y - CanvasOffsetY) / CanvasScale;

            sx = (int)System.Math.Floor(spriteX);
            sy = (int)System.Math.Floor(spriteY);

            var sprite = Document.Sprite;
            return sx >= 0 && sy >= 0 && sx < sprite.Width && sy < sprite.Height;
        }

        /// <summary>
        /// Convert a screen point to sprite coordinates without bounds-clamping.
        /// Useful for pan/zoom where out-of-bounds is still meaningful.
        /// </summary>
        public (float X, float Y) ScreenToSpriteF(Point screenPt)
        {
            return (
                (float)(screenPt.X - CanvasOffsetX) / CanvasScale,
                (float)(screenPt.Y - CanvasOffsetY) / CanvasScale);
        }

        // ------------------------------------------------------------------
        // Active image helper
        // ------------------------------------------------------------------

        /// <summary>
        /// Resolve the active pixel image for the active layer + frame.
        /// Returns null if the layer has no cel at this frame, or if the
        /// layer is not a drawable layer.
        /// </summary>
        public Image? GetActiveImage()
        {
            if (ActiveLayer is not LayerImage li) return null;
            var cel = li.GetCel(ActiveFrame);
            return cel?.Data?.Image;
        }
    }
}
