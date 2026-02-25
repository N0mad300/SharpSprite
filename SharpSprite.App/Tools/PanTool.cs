using Avalonia;
using Avalonia.Input;

namespace SharpSprite.App.Tools
{
    /// <summary>
    /// Pan tool.  Drags the canvas's <see cref="Controls.PixelCanvasControl.PanOffset"/>
    /// property.  Does not modify the document and never pushes to the undo stack.
    /// </summary>
    public sealed class PanTool : ITool
    {
        public ToolType Type => ToolType.Pan;

        public Cursor? GetCursor(ToolContext ctx) => new Cursor(StandardCursorType.Hand);

        private bool _panning;
        private Point _lastScreenPt;

        public void OnPointerPressed(ToolContext ctx, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(ctx.Canvas).Properties.IsLeftButtonPressed) return;
            _panning = true;
            _lastScreenPt = e.GetPosition(ctx.Canvas);
            e.Handled = true;
        }

        public void OnPointerMoved(ToolContext ctx, PointerEventArgs e)
        {
            if (!_panning) return;
            if (!e.GetCurrentPoint(ctx.Canvas).Properties.IsLeftButtonPressed)
            {
                _panning = false;
                return;
            }

            var current = e.GetPosition(ctx.Canvas);
            double dx = current.X - _lastScreenPt.X;
            double dy = current.Y - _lastScreenPt.Y;
            _lastScreenPt = current;

            ctx.Canvas.PanOffset += new Vector(dx, dy);
            e.Handled = true;
        }

        public void OnPointerReleased(ToolContext ctx, PointerReleasedEventArgs e)
        {
            _panning = false;
        }
    }
}
