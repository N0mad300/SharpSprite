namespace SharpSprite.Core.Commands
{
    /// <summary>
    /// A reversible operation on a <see cref="SharpSprite.Core.Document.Document"/>.
    ///
    /// Design rules:
    /// <list type="bullet">
    ///   <item><see cref="Execute"/> applies the change AND fires
    ///         <c>document.NotifyChanged()</c>.</item>
    ///   <item><see cref="Undo"/> reverts the change AND fires
    ///         <c>document.NotifyChanged()</c>.</item>
    ///   <item><see cref="TryMerge"/> lets two consecutive commands of the
    ///         same type collapse into one (used for pencil strokes so that
    ///         a single mouse drag is one undo step, not hundreds of pixels).
    ///         Returns <c>true</c> if the merge succeeded; the caller will
    ///         discard <paramref name="next"/> and keep <c>this</c>.</item>
    /// </list>
    /// </summary>
    public interface IDocumentCommand
    {
        /// <summary>Human-readable name shown in the Edit menu.</summary>
        string Name { get; }

        void Execute();
        void Undo();

        /// <summary>
        /// Attempt to absorb <paramref name="next"/> into this command.
        /// If the merge is possible, mutate <c>this</c> in-place and return
        /// <c>true</c>.  The <see cref="UndoStack"/> will discard
        /// <paramref name="next"/> and not push it.
        /// </summary>
        bool TryMerge(IDocumentCommand next);
    }
}
