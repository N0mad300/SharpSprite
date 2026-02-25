using System;
using System.Collections.Generic;

namespace SharpSprite.App.Tools
{
    /// <summary>
    /// Holds a singleton instance of every <see cref="ITool"/>.
    /// Tools are stateful (they track per-drag state in fields) so they must
    /// not be shared across multiple canvases, but one instance per canvas
    /// context is fine.
    /// </summary>
    public sealed class ToolRegistry
    {
        private readonly Dictionary<ToolType, ITool> _tools;

        public ToolRegistry()
        {
            _tools = new Dictionary<ToolType, ITool>
            {
                [ToolType.Pencil] = new PencilTool(),
                [ToolType.Eraser] = new EraserTool(),
                [ToolType.Pan] = new PanTool(),
                [ToolType.Zoom] = new ZoomTool(),
            };
        }

        public ITool Get(ToolType type)
        {
            if (_tools.TryGetValue(type, out var tool)) return tool;
            throw new ArgumentOutOfRangeException(nameof(type), $"No tool registered for {type}");
        }
    }
}
