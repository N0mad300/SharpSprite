using Avalonia.Input;

namespace SharpSprite.App.Tools
{
    /// <summary>
    /// Zoom tool.
    ///
    /// Left-click  → zoom in  (increment <see cref="Controls.PixelCanvasControl.Zoom"/>).
    /// Right-click → zoom out (decrement).
    /// Alt + scroll wheel is handled directly by <see cref="Controls.PixelCanvasControl"/>
    /// regardless of the active tool.
    ///
    /// Does not modify the document; never pushes to the undo stack.
    /// </summary>
    public sealed class ZoomTool : ITool
    {
        public ToolType Type => ToolType.Zoom;

        public Cursor? GetCursor(ToolContext ctx)
            => new Cursor(StandardCursorType.SizeAll); // closest built-in to a magnifier

        public const int MinZoom = 1;
        public const int MaxZoom = 32;

        public void OnPointerPressed(ToolContext ctx, PointerPressedEventArgs e)
        {
            var props = e.GetCurrentPoint(ctx.Canvas).Properties;

            int current = ctx.Canvas.Zoom <= 0 ? 1 : ctx.Canvas.Zoom;

            if (props.IsLeftButtonPressed)
                ctx.Canvas.Zoom = System.Math.Min(MaxZoom, current * 2);
            else if (props.IsRightButtonPressed)
                ctx.Canvas.Zoom = System.Math.Max(MinZoom, current / 2);

            e.Handled = true;
        }

        public void OnPointerMoved(ToolContext ctx, PointerEventArgs e) { }
        public void OnPointerReleased(ToolContext ctx, PointerReleasedEventArgs e) { }
    }
}
