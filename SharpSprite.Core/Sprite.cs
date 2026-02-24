namespace SharpSprite.Core.Document
{
    /// <summary>
    /// The root document object.  A sprite contains the canvas size, color mode,
    /// all layers, all frames (with durations), all palettes, all tags, all slices,
    /// and all tilesets.
    ///
    /// Key constraints:
    /// <list type="bullet">
    ///   <item>Canvas dimensions: 1..65535 × 1..65535.</item>
    ///   <item>Layers are stored in a root <see cref="LayerGroup"/>; the root group
    ///         is never shown in the UI but lets all traversal code be uniform.</item>
    ///   <item>Palettes are stored in order of their <see cref="Palette.Frame"/>;
    ///         the active palette at any given frame is the one with the largest
    ///         <c>Frame</c> index ≤ the requested frame.</item>
    ///   <item>Tilesets are stored in a flat list and referenced by index from
    ///         <see cref="LayerTilemap"/> objects.</item>
    /// </list>
    /// </summary>
    public sealed class Sprite
    {
        // ------------------------------------------------------------------
        // Constants
        // ------------------------------------------------------------------

        public const int MaxWidth = 65535;
        public const int MaxHeight = 65535;

        // ------------------------------------------------------------------
        // Private state
        // ------------------------------------------------------------------

        private int _width;
        private int _height;
        private int _transparentIndex;

        private readonly List<FrameInfo> _frames = new();
        private readonly List<Palette> _palettes = new();
        private readonly List<Tileset> _tilesets = new();

        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        /// <summary>
        /// Create a new blank sprite.
        /// </summary>
        public Sprite(int width, int height, ColorMode colorMode = ColorMode.Rgba)
        {
            SetSize(width, height);
            ColorMode = colorMode;
            RootGroup = new LayerGroup("__root__") { Sprite = this };

            // Start with one frame at 100 ms
            _frames.Add(new FrameInfo(100));

            // Start with a default 256-colour palette
            _palettes.Add(BuildDefaultPalette());
        }

        // ------------------------------------------------------------------
        // Canvas geometry
        // ------------------------------------------------------------------

        public int Width
        {
            get => _width;
            set => SetSize(value, _height);
        }

        public int Height
        {
            get => _height;
            set => SetSize(_width, value);
        }

        /// <summary>
        /// Resize the sprite canvas.
        /// This does NOT resize cel images; use a dedicated resize command for that.
        /// </summary>
        public void SetSize(int width, int height)
        {
            if (width < 1 || width > MaxWidth) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1 || height > MaxHeight) throw new ArgumentOutOfRangeException(nameof(height));
            _width = width;
            _height = height;
        }

        // ------------------------------------------------------------------
        // Color mode
        // ------------------------------------------------------------------

        public ColorMode ColorMode { get; private set; }

        /// <summary>
        /// For <see cref="ColorMode.Indexed"/> sprites, the palette index that
        /// is treated as transparent (default 0).
        /// </summary>
        public int TransparentIndex
        {
            get => _transparentIndex;
            set
            {
                if (value < 0 || value > 255) throw new ArgumentOutOfRangeException(nameof(value));
                _transparentIndex = value;
            }
        }

        // ------------------------------------------------------------------
        // Pixel ratio
        // ------------------------------------------------------------------

        public PixelRatio PixelRatio { get; set; } = PixelRatio.Square;

        // ------------------------------------------------------------------
        // Canvas grid (for display + snapping)
        // ------------------------------------------------------------------

        public Grid Grid { get; set; } = new Grid(16, 16);

        // ------------------------------------------------------------------
        // Frames
        // ------------------------------------------------------------------

        /// <summary>Total number of frames (≥ 1).</summary>
        public int FrameCount => _frames.Count;

        /// <summary>Get the <see cref="FrameInfo"/> for a given frame index.</summary>
        public FrameInfo GetFrame(int frame) => _frames[frame];

        /// <summary>Set frame duration in milliseconds.</summary>
        public void SetFrameDuration(int frame, int durationMs)
        {
            if (durationMs <= 0) throw new ArgumentOutOfRangeException(nameof(durationMs));
            _frames[frame].DurationMs = durationMs;
        }

        /// <summary>Set the same duration on all frames.</summary>
        public void SetAllFramesDuration(int durationMs)
        {
            foreach (var f in _frames) f.DurationMs = durationMs;
        }

        /// <summary>Insert <paramref name="count"/> frames after <paramref name="afterFrame"/>.</summary>
        public void InsertFrames(int afterFrame, int count = 1, int durationMs = 100)
        {
            int insertAt = afterFrame + 1;
            for (int i = 0; i < count; i++)
                _frames.Insert(insertAt + i, new FrameInfo(durationMs));

            // Shift cels in all image/tilemap layers
            foreach (var layer in RootGroup.FlattenLeafLayers())
            {
                if (layer is LayerImage li)
                    li.ShiftCels(insertAt, count);
                // LayerTilemap shift not implemented here; extend as needed
            }
        }

        /// <summary>Remove the frame at the given index (minimum 1 frame must remain).</summary>
        public void RemoveFrame(int frame)
        {
            if (_frames.Count <= 1) throw new InvalidOperationException("Cannot remove the last frame.");
            _frames.RemoveAt(frame);

            foreach (var layer in RootGroup.FlattenLeafLayers())
            {
                if (layer is LayerImage li)
                {
                    li.RemoveCel(frame);
                    li.ShiftCels(frame + 1, -1);
                }
            }
        }

        // ------------------------------------------------------------------
        // Layers
        // ------------------------------------------------------------------

        /// <summary>
        /// The invisible root group that contains all top-level layers.
        /// Use this to traverse the full layer tree.
        /// </summary>
        public LayerGroup RootGroup { get; }

        /// <summary>Top-level layers (bottom-to-top display order).</summary>
        public IReadOnlyList<Layer> Layers => RootGroup.Layers;

        /// <summary>Append a top-level layer.</summary>
        public void AddLayer(Layer layer) => RootGroup.AddLayer(layer);

        /// <summary>Remove a top-level layer.</summary>
        public bool RemoveLayer(Layer layer) => RootGroup.RemoveLayer(layer);

        /// <summary>
        /// Flatten all layers into a single ordered list suitable for compositing.
        /// Groups are excluded; leaf layers (image/tilemap) are returned bottom-to-top.
        /// </summary>
        public IEnumerable<Layer> GetLayersForCompositing()
            => RootGroup.FlattenLeafLayers();

        // ------------------------------------------------------------------
        // Palettes
        // ------------------------------------------------------------------

        /// <summary>
        /// All palettes, in ascending frame order.
        /// There is always at least one palette (for frame 0).
        /// </summary>
        public IReadOnlyList<Palette> Palettes => _palettes;

        /// <summary>
        /// Get the active palette at the given frame index.
        /// Returns the palette with the greatest <see cref="Palette.Frame"/> ≤ frame.
        /// </summary>
        public Palette GetPalette(int frame = 0)
        {
            Palette? result = null;
            foreach (var p in _palettes)
            {
                if (p.Frame > frame) break;
                result = p;
            }
            return result ?? _palettes[0];
        }

        /// <summary>Add or replace a palette for the given frame.</summary>
        public void SetPalette(Palette palette)
        {
            for (int i = 0; i < _palettes.Count; i++)
            {
                if (_palettes[i].Frame == palette.Frame)
                {
                    _palettes[i] = palette;
                    return;
                }
                if (_palettes[i].Frame > palette.Frame)
                {
                    _palettes.Insert(i, palette);
                    return;
                }
            }
            _palettes.Add(palette);
        }

        public void RemovePaletteAt(int frame)
        {
            if (frame == 0)
                throw new InvalidOperationException("Cannot remove the palette for frame 0.");
            _palettes.RemoveAll(p => p.Frame == frame);
        }

        // ------------------------------------------------------------------
        // Tags
        // ------------------------------------------------------------------

        public Tags Tags { get; } = new Tags();

        // ------------------------------------------------------------------
        // Slices
        // ------------------------------------------------------------------

        public Slices Slices { get; } = new Slices();

        // ------------------------------------------------------------------
        // Tilesets
        // ------------------------------------------------------------------

        public IReadOnlyList<Tileset> Tilesets => _tilesets;

        public void AddTileset(Tileset tileset)
        {
            if (tileset == null) throw new ArgumentNullException(nameof(tileset));
            _tilesets.Add(tileset);
        }

        public bool RemoveTileset(Tileset tileset) => _tilesets.Remove(tileset);

        /// <summary>
        /// Get the tileset used by a <see cref="LayerTilemap"/>.
        /// Helper for code that needs to resolve from the sprite level.
        /// </summary>
        public Tileset? GetTilesetFor(LayerTilemap layer) => layer.Tileset;

        // ------------------------------------------------------------------
        // User data
        // ------------------------------------------------------------------

        public UserData UserData { get; } = new();

        // ------------------------------------------------------------------
        // Filename / metadata
        // ------------------------------------------------------------------

        /// <summary>Absolute file path the sprite was last saved to / loaded from.</summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// True if the sprite has unsaved changes.
        /// The editor / undo system is responsible for setting this.
        /// </summary>
        public bool IsModified { get; set; } = false;

        // ------------------------------------------------------------------
        // Whole-sprite helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Return every cel in the sprite, across all layers and frames.
        /// Useful for batch operations.
        /// </summary>
        public IEnumerable<(Layer Layer, Cel Cel)> AllCels()
        {
            foreach (var layer in RootGroup.FlattenLeafLayers())
                foreach (var cel in layer.Cels)
                    yield return (layer, cel);
        }

        /// <summary>
        /// Collect all cels visible at a single frame for compositing.
        /// Layers are returned in bottom-to-top order.
        /// </summary>
        public IEnumerable<(Layer Layer, Cel Cel)> CelsAtFrame(int frame)
        {
            foreach (var layer in RootGroup.FlattenLeafLayers())
            {
                var cel = layer.GetCel(frame);
                if (cel != null)
                    yield return (layer, cel);
            }
        }

        // ------------------------------------------------------------------
        // Private helpers
        // ------------------------------------------------------------------

        private static Palette BuildDefaultPalette()
        {
            var p = new Palette(256) { Frame = 0, Name = "Default" };
            // Entry 0 = transparent
            p.SetColor(0, Rgba32.Transparent);
            // Entry 1 = black
            p.SetColor(1, Rgba32.Black);
            // Entry 255 = white
            p.SetColor(255, Rgba32.White);
            return p;
        }
    }
}
