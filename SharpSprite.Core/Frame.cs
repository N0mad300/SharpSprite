namespace SharpSprite.Core.Document
{
    // -------------------------------------------------------------------------
    // FrameInfo – per-frame metadata
    // -------------------------------------------------------------------------

    /// <summary>
    /// Metadata for a single animation frame.
    /// This struct wraps the per-frame information stored in the sprite.
    /// </summary>
    public sealed class FrameInfo
    {
        /// <summary>How long this frame is displayed in milliseconds.</summary>
        public int DurationMs { get; set; } = 100;

        public FrameInfo() { }
        public FrameInfo(int durationMs) { DurationMs = durationMs; }
        public FrameInfo Clone() => new(DurationMs);
    }

    // -------------------------------------------------------------------------
    // Tag (animation tag / animation range)
    // -------------------------------------------------------------------------

    /// <summary>
    /// An animation tag defines a named range of frames and a playback direction.
    /// </summary>
    public sealed class Tag
    {
        private static int _nextId = 1;

        public Tag()
        {
            Id = _nextId++;
        }

        // ------------------------------------------------------------------
        // Properties
        // ------------------------------------------------------------------

        /// <summary>Unique runtime identifier.</summary>
        public int Id { get; }

        /// <summary>Tag name shown in the timeline.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>First frame (inclusive, 0-based).</summary>
        public int FromFrame { get; set; } = 0;

        /// <summary>Last frame (inclusive, 0-based).</summary>
        public int ToFrame { get; set; } = 0;

        /// <summary>Playback direction for this tag.</summary>
        public AniDir AniDir { get; set; } = AniDir.Forward;

        /// <summary>Number of times to repeat this tag (0 = infinite).</summary>
        public int Repeat { get; set; } = 0;

        /// <summary>RGBA color used to highlight this tag in the timeline.</summary>
        public Rgba32 Color { get; set; } = new Rgba32(0, 0, 0, 255);

        /// <summary>User-defined metadata.</summary>
        public UserData UserData { get; } = new();

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        public int FrameCount => ToFrame - FromFrame + 1;

        public Tag Clone()
        {
            var t = new Tag
            {
                Name = Name,
                FromFrame = FromFrame,
                ToFrame = ToFrame,
                AniDir = AniDir,
                Repeat = Repeat,
                Color = Color,
            };
            t.UserData.Text = UserData.Text;
            t.UserData.Color = UserData.Color;
            return t;
        }
    }

    // -------------------------------------------------------------------------
    // Tags collection
    // -------------------------------------------------------------------------

    /// <summary>
    /// Ordered list of <see cref="Tag"/>s belonging to a sprite.
    /// Tags can overlap.
    /// </summary>
    public sealed class Tags
    {
        private readonly List<Tag> _tags = new();

        public int Count => _tags.Count;
        public IReadOnlyList<Tag> All => _tags;

        public Tag this[int index] => _tags[index];

        public void Add(Tag tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            _tags.Add(tag);
        }

        public bool Remove(Tag tag) => _tags.Remove(tag);
        public void RemoveAt(int index) => _tags.RemoveAt(index);
        public void Clear() => _tags.Clear();

        /// <summary>Find the first tag whose name matches (case-sensitive).</summary>
        public Tag? FindByName(string name)
        {
            foreach (var t in _tags)
                if (t.Name == name) return t;
            return null;
        }
    }

    // -------------------------------------------------------------------------
    // SliceKey – per-frame slice data
    // -------------------------------------------------------------------------

    /// <summary>
    /// The geometry of a <see cref="Slice"/> at a specific frame.
    /// A slice can have different bounds (and 9-slice / pivot data) on each frame.
    /// </summary>
    public sealed class SliceKey
    {
        /// <summary>The frame this key takes effect from (0-based).</summary>
        public int Frame { get; set; }

        /// <summary>Slice bounds in sprite canvas coordinates.</summary>
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        // 9-slice center rectangle (optional)
        public bool Has9Slices { get; set; }
        public int CenterX { get; set; }
        public int CenterY { get; set; }
        public int CenterWidth { get; set; }
        public int CenterHeight { get; set; }

        // Pivot point (optional)
        public bool HasPivot { get; set; }
        public int PivotX { get; set; }
        public int PivotY { get; set; }

        public SliceKey Clone() => new()
        {
            Frame = Frame,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            Has9Slices = Has9Slices,
            CenterX = CenterX,
            CenterY = CenterY,
            CenterWidth = CenterWidth,
            CenterHeight = CenterHeight,
            HasPivot = HasPivot,
            PivotX = PivotX,
            PivotY = PivotY,
        };
    }

    // -------------------------------------------------------------------------
    // Slice
    // -------------------------------------------------------------------------

    /// <summary>
    /// A named region on the sprite canvas, optionally with 9-slice and pivot data.
    /// Slices can change their geometry per frame using <see cref="SliceKey"/>s.
    /// </summary>
    public sealed class Slice
    {
        private static int _nextId = 1;

        private readonly SortedList<int, SliceKey> _keys = new();

        public Slice(string name = "Slice")
        {
            Id = _nextId++;
            Name = name;
        }

        public int Id { get; }
        public string Name { get; set; }

        /// <summary>RGBA color used to display this slice in the editor.</summary>
        public Rgba32 Color { get; set; } = new Rgba32(0, 130, 211, 255);

        public UserData UserData { get; } = new();

        // ------------------------------------------------------------------
        // Key management
        // ------------------------------------------------------------------

        public IReadOnlyList<SliceKey> Keys => (IReadOnlyList<SliceKey>)_keys.Values;

        public void AddKey(SliceKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            _keys[key.Frame] = key;
        }

        public bool RemoveKey(int frame) => _keys.Remove(frame);

        /// <summary>
        /// Get the effective key at the given frame (the key with the largest
        /// frame index ≤ <paramref name="frame"/>).
        /// </summary>
        public SliceKey? GetKeyAtFrame(int frame)
        {
            SliceKey? result = null;
            foreach (var kv in _keys)
            {
                if (kv.Key > frame) break;
                result = kv.Value;
            }
            return result;
        }

        public Slice Clone()
        {
            var s = new Slice(Name) { Color = Color };
            s.UserData.Text = UserData.Text;
            s.UserData.Color = UserData.Color;
            foreach (var kv in _keys)
                s._keys[kv.Key] = kv.Value.Clone();
            return s;
        }
    }

    // -------------------------------------------------------------------------
    // Slices collection
    // -------------------------------------------------------------------------

    public sealed class Slices
    {
        private readonly List<Slice> _slices = new();
        public IReadOnlyList<Slice> All => _slices;
        public int Count => _slices.Count;

        public void Add(Slice slice)
        {
            if (slice == null) throw new ArgumentNullException(nameof(slice));
            _slices.Add(slice);
        }

        public bool Remove(Slice slice) => _slices.Remove(slice);
        public Slice? FindByName(string name)
        {
            foreach (var s in _slices)
                if (s.Name == name) return s;
            return null;
        }
    }
}
