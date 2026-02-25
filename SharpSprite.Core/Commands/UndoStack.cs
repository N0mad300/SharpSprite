namespace SharpSprite.Core.Commands
{
    /// <summary>
    /// A bounded undo / redo stack.
    ///
    /// Merging:
    ///   When <see cref="Push"/> is called, it first asks the top command
    ///   whether it can absorb the new one via <see cref="IDocumentCommand.TryMerge"/>.
    ///   If yes, the new command is dropped (already absorbed into the top).
    ///   This collapses a whole pencil drag into a single undo step.
    ///
    /// Capacity:
    ///   Older entries are silently evicted when <see cref="Capacity"/> is exceeded.
    /// </summary>
    public sealed class UndoStack
    {
        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        private readonly LinkedList<IDocumentCommand> _undoList = new();
        private readonly LinkedList<IDocumentCommand> _redoList = new();

        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        public UndoStack(int capacity = 100)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
        }

        // ------------------------------------------------------------------
        // Properties
        // ------------------------------------------------------------------

        public int Capacity { get; }

        public bool CanUndo => _undoList.Count > 0;
        public bool CanRedo => _redoList.Count > 0;

        /// <summary>Name of the next undo step (e.g. "Undo Pencil"), or null.</summary>
        public string? NextUndoName => _undoList.Last?.Value.Name;

        /// <summary>Name of the next redo step, or null.</summary>
        public string? NextRedoName => _redoList.First?.Value.Name;

        // ------------------------------------------------------------------
        // Events
        // ------------------------------------------------------------------

        /// <summary>Fired after any push, undo, redo, or clear.</summary>
        public event EventHandler? Changed;

        // ------------------------------------------------------------------
        // Core operations
        // ------------------------------------------------------------------

        /// <summary>
        /// Record an already-executed command.
        /// Clears the redo stack (a new action invalidates undone history).
        /// Attempts to merge with the current top command first.
        /// </summary>
        public void Push(IDocumentCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            // Try to merge into the existing top command
            if (_undoList.Last != null && _undoList.Last.Value.TryMerge(command))
            {
                // Merged – no structural change, but notify so UI labels refresh
                Changed?.Invoke(this, EventArgs.Empty);
                return;
            }

            // Clear redo history (branching)
            _redoList.Clear();

            _undoList.AddLast(command);

            // Evict oldest if over capacity
            while (_undoList.Count > Capacity)
                _undoList.RemoveFirst();

            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Undo the most recent command.
        /// The command's <see cref="IDocumentCommand.Undo"/> is responsible for
        /// notifying the document.
        /// </summary>
        public void Undo()
        {
            if (!CanUndo) return;
            var cmd = _undoList.Last!.Value;
            _undoList.RemoveLast();
            cmd.Undo();
            _redoList.AddFirst(cmd);
            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Re-apply the most recently undone command.</summary>
        public void Redo()
        {
            if (!CanRedo) return;
            var cmd = _redoList.First!.Value;
            _redoList.RemoveFirst();
            cmd.Execute();
            _undoList.AddLast(cmd);
            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Discard all undo/redo history (e.g. on file save-as).</summary>
        public void Clear()
        {
            _undoList.Clear();
            _redoList.Clear();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Mark the current stack position as the "clean" (saved) state.
        /// (Placeholder – full dirty-tracking can be added later with a
        /// saved-position pointer if needed.)
        /// </summary>
        public void MarkClean() { /* extend later */ }
    }
}
