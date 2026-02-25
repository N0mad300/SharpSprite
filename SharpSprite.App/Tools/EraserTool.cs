using Avalonia.Input;
using SharpSprite.Core.Document;

namespace SharpSprite.App.Tools
{
    /// <summary>
    /// Eraser tool.  Identical to <see cref="PencilTool"/> but always paints
    /// <see cref="Rgba32.Transparent"/> regardless of the foreground color.
    ///
    /// Implemented by inheriting PencilTool and overriding the context's
    /// foreground color at the point where we build the context — but since
    /// ToolContext is immutable-ish, we instead override the pointer handlers
    /// to swap the color before delegating.
    /// </summary>
    public sealed class EraserTool : ITool
    {
        public ToolType Type => ToolType.Eraser;

        public Cursor? GetCursor(ToolContext ctx) => new Cursor(StandardCursorType.Cross);

        private readonly PencilTool _pencil = new();

        public void OnPointerPressed(ToolContext ctx, PointerPressedEventArgs e)
            => _pencil.OnPointerPressed(WithEraseColor(ctx), e);

        public void OnPointerMoved(ToolContext ctx, PointerEventArgs e)
            => _pencil.OnPointerMoved(WithEraseColor(ctx), e);

        public void OnPointerReleased(ToolContext ctx, PointerReleasedEventArgs e)
            => _pencil.OnPointerReleased(WithEraseColor(ctx), e);

        /// <summary>
        /// Returns a shallow copy of the context with ForegroundColor = transparent.
        /// ToolContext is a class; we use an init-only pattern so we can copy it.
        /// </summary>
        private static ToolContext WithEraseColor(ToolContext ctx) => new()
        {
            Document = ctx.Document,
            UndoStack = ctx.UndoStack,
            ActiveLayer = ctx.ActiveLayer,
            ActiveFrame = ctx.ActiveFrame,
            ForegroundColor = Rgba32.Transparent,
            BackgroundColor = ctx.BackgroundColor,
            CanvasScale = ctx.CanvasScale,
            CanvasOffsetX = ctx.CanvasOffsetX,
            CanvasOffsetY = ctx.CanvasOffsetY,
            Canvas = ctx.Canvas,
        };
    }
}
