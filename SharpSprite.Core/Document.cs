namespace SharpSprite.Core.Document
{
    /// <summary>
    /// Top-level application document.  Wraps a <see cref="Sprite"/> and adds
    /// application-level concerns: filename, dirty tracking, and an event bus
    /// so the UI can subscribe to model changes.
    ///
    /// Mirrors the split between Aseprite's <c>doc::Document</c> (pure model)
    /// and <c>app::Document</c> (app-level concerns).
    /// </summary>
    public sealed class Document
    {
        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        public Document(Sprite sprite)
        {
            Sprite = sprite ?? throw new ArgumentNullException(nameof(sprite));
        }

        /// <summary>Create a new blank document.</summary>
        public Document(int width, int height, ColorMode colorMode = ColorMode.Rgba)
            : this(new Sprite(width, height, colorMode)) { }

        // ------------------------------------------------------------------
        // Properties
        // ------------------------------------------------------------------

        /// <summary>The sprite data model.</summary>
        public Sprite Sprite { get; }

        /// <summary>Absolute file path, or <c>null</c> for an unsaved document.</summary>
        public string? FilePath
        {
            get => Sprite.FilePath;
            set => Sprite.FilePath = value;
        }

        /// <summary>Display name derived from the file path, or "Untitled".</summary>
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(FilePath)) return "Untitled";
                return System.IO.Path.GetFileNameWithoutExtension(FilePath);
            }
        }

        /// <summary>True if the document has unsaved changes.</summary>
        public bool IsModified
        {
            get => Sprite.IsModified;
            set
            {
                if (Sprite.IsModified == value) return;
                Sprite.IsModified = value;
                ModifiedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // ------------------------------------------------------------------
        // Events
        // ------------------------------------------------------------------

        /// <summary>Raised when <see cref="IsModified"/> changes.</summary>
        public event EventHandler? ModifiedChanged;

        /// <summary>
        /// Raised when any part of the document model changes.
        /// Handlers should refresh the canvas or timeline as needed.
        /// </summary>
        public event EventHandler<DocumentChangedEventArgs>? Changed;

        /// <summary>Notify subscribers of a model change.</summary>
        public void NotifyChanged(DocumentChangeKind kind)
        {
            IsModified = true;
            Changed?.Invoke(this, new DocumentChangedEventArgs(kind));
        }
    }

    // -------------------------------------------------------------------------
    // Change notification types
    // -------------------------------------------------------------------------

    public enum DocumentChangeKind
    {
        General,
        CanvasResized,
        LayerAdded,
        LayerRemoved,
        LayerReordered,
        LayerPropertyChanged,
        CelAdded,
        CelRemoved,
        CelMoved,
        CelImageChanged,
        FrameAdded,
        FrameRemoved,
        FrameDurationChanged,
        PaletteChanged,
        TagAdded,
        TagRemoved,
        TagChanged,
        SliceAdded,
        SliceRemoved,
        SliceChanged,
        TilesetAdded,
        TilesetRemoved,
        TileChanged,
    }

    public sealed class DocumentChangedEventArgs : EventArgs
    {
        public DocumentChangeKind Kind { get; }
        public DocumentChangedEventArgs(DocumentChangeKind kind) { Kind = kind; }
    }
}
