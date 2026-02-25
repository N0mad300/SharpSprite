using Avalonia.Input;

namespace SharpSprite.App.Tools
{
    /// <summary>
    /// All available tool types.  The enum value is used as a key in
    /// <see cref="ToolRegistry"/> and is bindable from the ViewModel.
    /// </summary>
    public enum ToolType
    {
        Pencil,
        Eraser,
        Pan,
        Zoom,
    }

    /// <summary>
    /// A tool that responds to pointer events on the canvas.
    ///
    /// Each tool receives a <see cref="ToolContext"/> that provides everything
    /// it needs: the document, active layer/frame, colors, undo stack, and
    /// the canvas's current transform (so it can convert screen→sprite coords).
    ///
    /// Tools are stateless singletons (they store their per-drag state in
    /// private fields that are reset on <see cref="OnPointerPressed"/>).
    /// </summary>
    public interface ITool
    {
        ToolType Type { get; }

        /// <summary>Called when the primary mouse button is pressed over the canvas.</summary>
        void OnPointerPressed(ToolContext ctx, PointerPressedEventArgs e);

        /// <summary>Called on every pointer move while the button is held.</summary>
        void OnPointerMoved(ToolContext ctx, PointerEventArgs e);

        /// <summary>Called when the primary mouse button is released.</summary>
        void OnPointerReleased(ToolContext ctx, PointerReleasedEventArgs e);

        /// <summary>
        /// The Avalonia cursor to display when this tool is active.
        /// Return <c>null</c> to use the default arrow cursor.
        /// </summary>
        Cursor? GetCursor(ToolContext ctx);
    }
}
