using System;
using Avalonia.Input;
using SharpSprite.Core.Commands;
using SharpSprite.Core.Document;

namespace SharpSprite.App.Tools
{
    /// <summary>
    /// Pencil tool.
    ///
    /// Behaviour:
    /// <list type="bullet">
    ///   <item>On pointer-down: start a new stroke session, paint the first pixel.</item>
    ///   <item>On pointer-move (button held): Bresenham-interpolate between the last
    ///         point and the current point so there are no gaps even on fast drags.</item>
    ///   <item>On pointer-up: push the completed <see cref="PaintStrokeCommand"/> to
    ///         the undo stack.</item>
    /// </list>
    ///
    /// The command is built incrementally during the drag; each pixel written
    /// is recorded into the command's <see cref="PixelPatch"/> in real-time so
    /// the canvas updates immediately without waiting for pointer-up.
    /// </summary>
    public sealed class PencilTool : ITool
    {
        // ------------------------------------------------------------------
        // ITool
        // ------------------------------------------------------------------

        public ToolType Type => ToolType.Pencil;

        public Cursor? GetCursor(ToolContext ctx) => new Cursor(StandardCursorType.Cross);

        // ------------------------------------------------------------------
        // Per-stroke state
        // ------------------------------------------------------------------

        private bool _drawing;
        private int _lastSx, _lastSy;
        private PaintStrokeCommand? _activeCommand;

        // Each mouse-down gets a unique merge key so only pixels within the
        // same drag merge into one undo step.
        private static int _mergeKeyCounter;

        // ------------------------------------------------------------------
        // Pointer handlers
        // ------------------------------------------------------------------

        public void OnPointerPressed(ToolContext ctx, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(ctx.Canvas).Properties.IsLeftButtonPressed) return;

            var image = ctx.GetActiveImage();
            if (image == null) return;

            int mergeKey = System.Threading.Interlocked.Increment(ref _mergeKeyCounter);
            var patch = new PixelPatch();
            _activeCommand = new PaintStrokeCommand(
                ctx.Document, image, patch, mergeKey, Name);

            _drawing = true;
            var pos = e.GetPosition(ctx.Canvas);

            if (ctx.TryScreenToSprite(pos, out int sx, out int sy))
            {
                _lastSx = sx;
                _lastSy = sy;
                _activeCommand.RecordPixel(sx, sy, ctx.ForegroundColor);
                ctx.Document.NotifyChanged(DocumentChangeKind.CelImageChanged);
            }

            Console.WriteLine("Started drawing");

            e.Handled = true;
        }

        public void OnPointerMoved(ToolContext ctx, PointerEventArgs e)
        {
            if (!_drawing || _activeCommand == null) return;
            if (!e.GetCurrentPoint(ctx.Canvas).Properties.IsLeftButtonPressed)
            {
                // Button was released outside the control – treat as release
                FinishStroke(ctx);
                return;
            }

            var pos = e.GetPosition(ctx.Canvas);
            // Allow slightly out-of-bounds points; RecordPixel clips them
            (float fx, float fy) = ctx.ScreenToSpriteF(pos);
            int sx = (int)Math.Floor(fx);
            int sy = (int)Math.Floor(fy);

            if (sx == _lastSx && sy == _lastSy) return; // no movement

            // Bresenham line from (_lastSx,_lastSy) to (sx,sy)
            PaintLine(ctx, _activeCommand, _lastSx, _lastSy, sx, sy, ctx.ForegroundColor);
            _lastSx = sx;
            _lastSy = sy;

            Console.WriteLine("Drawing");

            ctx.Document.NotifyChanged(DocumentChangeKind.CelImageChanged);
            e.Handled = true;
        }

        public void OnPointerReleased(ToolContext ctx, PointerReleasedEventArgs e)
        {
            if (!_drawing) return;
            FinishStroke(ctx);
            e.Handled = true;
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        protected string Name => "Pencil";

        private void FinishStroke(ToolContext ctx)
        {
            _drawing = false;
            if (_activeCommand != null)
            {
                ctx.UndoStack.Push(_activeCommand);
                _activeCommand = null;
            }
        }

        /// <summary>
        /// Paint every pixel along the Bresenham line from (x0,y0) to (x1,y1).
        /// This is the standard integer Bresenham algorithm.
        /// </summary>
        internal static void PaintLine(
            ToolContext ctx,
            PaintStrokeCommand cmd,
            int x0, int y0, int x1, int y1,
            Rgba32 color)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                cmd.RecordPixel(x0, y0, color);

                if (x0 == x1 && y0 == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }
    }
}
