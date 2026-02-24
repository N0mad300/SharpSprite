namespace SharpSprite.Core.Document
{
    // -------------------------------------------------------------------------
    // Layer (abstract base)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Abstract base for all layer types.
    /// </summary>
    public abstract class Layer
    {
        private static int _nextId = 1;

        protected Layer(string name = "")
        {
            Id = _nextId++;
            Name = name;
        }

        // ------------------------------------------------------------------
        // Identity / metadata
        // ------------------------------------------------------------------

        /// <summary>Unique runtime identifier for this layer (not persisted).</summary>
        public int Id { get; }

        /// <summary>Display name of the layer.</summary>
        public string Name { get; set; } = "Layer";

        /// <summary>Bitfield of <see cref="LayerFlags"/>.</summary>
        public LayerFlags Flags { get; set; } = LayerFlags.Visible | LayerFlags.Editable;

        // ------------------------------------------------------------------
        // Compositing
        // ------------------------------------------------------------------

        /// <summary>Layer opacity 0-255.</summary>
        public byte Opacity { get; set; } = 255;

        /// <summary>Blend mode for compositing this layer over layers below it.</summary>
        public BlendMode BlendMode { get; set; } = BlendMode.Normal;

        // ------------------------------------------------------------------
        // Hierarchy
        // ------------------------------------------------------------------

        /// <summary>Parent group layer, or <c>null</c> if top-level.</summary>
        public LayerGroup? Parent { get; internal set; }

        /// <summary>The sprite that owns this layer (set when added to a sprite).</summary>
        public Sprite? Sprite { get; internal set; }

        // ------------------------------------------------------------------
        // Flag helpers
        // ------------------------------------------------------------------

        public bool IsVisible
        {
            get => Flags.HasFlag(LayerFlags.Visible);
            set => SetFlag(LayerFlags.Visible, value);
        }

        public bool IsEditable
        {
            get => Flags.HasFlag(LayerFlags.Editable);
            set => SetFlag(LayerFlags.Editable, value);
        }

        public bool IsBackground
        {
            get => Flags.HasFlag(LayerFlags.Background);
            set => SetFlag(LayerFlags.Background, value);
        }

        public bool IsReference
        {
            get => Flags.HasFlag(LayerFlags.Reference);
            set => SetFlag(LayerFlags.Reference, value);
        }

        public bool IsCollapsed
        {
            get => Flags.HasFlag(LayerFlags.Collapsed);
            set => SetFlag(LayerFlags.Collapsed, value);
        }

        public bool PreferLinkedCels
        {
            get => Flags.HasFlag(LayerFlags.PreferLinkedCels);
            set => SetFlag(LayerFlags.PreferLinkedCels, value);
        }

        // ------------------------------------------------------------------
        // Type helpers
        // ------------------------------------------------------------------

        public bool IsImage => this is LayerImage;
        public bool IsGroup => this is LayerGroup;
        public bool IsTilemap => this is LayerTilemap;

        // ------------------------------------------------------------------
        // User data
        // ------------------------------------------------------------------

        public UserData UserData { get; } = new();

        // ------------------------------------------------------------------
        // Abstract members
        // ------------------------------------------------------------------

        /// <summary>Return all cels in this layer (empty for groups).</summary>
        public abstract IReadOnlyList<Cel> Cels { get; }

        /// <summary>Get the cel at the given frame index, or <c>null</c>.</summary>
        public abstract Cel? GetCel(int frame);

        // ------------------------------------------------------------------
        // Private
        // ------------------------------------------------------------------

        private void SetFlag(LayerFlags flag, bool value)
        {
            if (value) Flags |= flag;
            else Flags &= ~flag;
        }
    }

    // -------------------------------------------------------------------------
    // LayerImage – standard raster layer
    // -------------------------------------------------------------------------

    /// <summary>
    /// A standard layer that holds one optional <see cref="Cel"/> per frame.
    /// </summary>
    public sealed class LayerImage : Layer
    {
        private readonly SortedList<int, Cel> _cels = new();

        public LayerImage(string name = "Layer") : base(name) { }

        public override IReadOnlyList<Cel> Cels => (IReadOnlyList<Cel>)_cels.Values;

        public override Cel? GetCel(int frame)
            => _cels.TryGetValue(frame, out var cel) ? cel : null;

        /// <summary>Add or replace the cel at the specified frame.</summary>
        public void AddCel(Cel cel)
        {
            if (cel == null) throw new ArgumentNullException(nameof(cel));
            _cels[cel.Frame] = cel;
        }

        /// <summary>Remove and return the cel at the specified frame.</summary>
        public bool RemoveCel(int frame) => _cels.Remove(frame);

        /// <summary>Remove the cel at the specified frame and return it, or null.</summary>
        public Cel? RemoveAndGetCel(int frame)
        {
            if (_cels.TryGetValue(frame, out var cel))
            {
                _cels.Remove(frame);
                return cel;
            }
            return null;
        }

        /// <summary>
        /// Move all cels at frame index &gt;= <paramref name="fromFrame"/> by
        /// <paramref name="delta"/> frames.  Used when inserting/removing frames.
        /// </summary>
        public void ShiftCels(int fromFrame, int delta)
        {
            var toMove = _cels.Where(kv => kv.Key >= fromFrame).OrderByDescending(kv => kv.Key).ToList();
            foreach (var kv in toMove)
            {
                _cels.Remove(kv.Key);
                kv.Value.Frame = kv.Key + delta;
                _cels[kv.Value.Frame] = kv.Value;
            }
        }

        public LayerImage Clone()
        {
            var l = new LayerImage(Name) { Flags = Flags, Opacity = Opacity, BlendMode = BlendMode };
            l.UserData.Text = UserData.Text;
            l.UserData.Color = UserData.Color;
            foreach (var cel in _cels.Values)
                l.AddCel(cel.Clone());
            return l;
        }
    }

    // -------------------------------------------------------------------------
    // LayerGroup – folder / group layer
    // -------------------------------------------------------------------------

    /// <summary>
    /// A layer group that contains child layers.
    /// Groups do not hold cels directly; they composite their children.
    /// </summary>
    public sealed class LayerGroup : Layer
    {
        private readonly List<Layer> _layers = new();

        public LayerGroup(string name = "Group") : base(name) { }

        // ------------------------------------------------------------------
        // Children
        // ------------------------------------------------------------------

        /// <summary>Child layers, bottom-to-top (index 0 = bottom).</summary>
        public IReadOnlyList<Layer> Layers => _layers;

        public void AddLayer(Layer layer)
        {
            if (layer == null) throw new ArgumentNullException(nameof(layer));
            if (layer.Parent != null)
                throw new InvalidOperationException("Layer already belongs to a group.");
            layer.Parent = this;
            layer.Sprite = Sprite;
            _layers.Add(layer);
        }

        public void InsertLayer(int index, Layer layer)
        {
            if (layer.Parent != null)
                throw new InvalidOperationException("Layer already belongs to a group.");
            layer.Parent = this;
            layer.Sprite = Sprite;
            _layers.Insert(index, layer);
        }

        public bool RemoveLayer(Layer layer)
        {
            if (!_layers.Remove(layer)) return false;
            layer.Parent = null;
            layer.Sprite = null;
            return true;
        }

        public void MoveLayer(Layer layer, int newIndex)
        {
            int current = _layers.IndexOf(layer);
            if (current < 0) throw new ArgumentException("Layer not found in group.");
            _layers.RemoveAt(current);
            _layers.Insert(Math.Clamp(newIndex, 0, _layers.Count), layer);
        }

        // ------------------------------------------------------------------
        // Layer (abstract) implementation
        // ------------------------------------------------------------------

        public override IReadOnlyList<Cel> Cels => Array.Empty<Cel>();
        public override Cel? GetCel(int frame) => null;

        // ------------------------------------------------------------------
        // Traversal helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Enumerate all descendant layers depth-first (groups included).
        /// Bottom-to-top order.
        /// </summary>
        public IEnumerable<Layer> FlattenLayers()
        {
            foreach (var child in _layers)
            {
                if (child is LayerGroup grp)
                {
                    yield return grp;
                    foreach (var desc in grp.FlattenLayers())
                        yield return desc;
                }
                else
                {
                    yield return child;
                }
            }
        }

        /// <summary>Enumerate only leaf (non-group) layers, bottom-to-top.</summary>
        public IEnumerable<Layer> FlattenLeafLayers()
            => FlattenLayers().Where(l => !l.IsGroup);
    }

    // -------------------------------------------------------------------------
    // LayerTilemap – tilemap layer
    // -------------------------------------------------------------------------

    /// <summary>
    /// A layer where each cel's <see cref="Image"/> stores tile references
    /// (uint32 per cell) rather than direct pixel data.
    /// Each cell value encodes a tile index plus flip/rotate flags.
    /// </summary>
    public sealed class LayerTilemap : Layer
    {
        private readonly SortedList<int, Cel> _cels = new();

        public LayerTilemap(string name = "Tilemap") : base(name) { }

        // ------------------------------------------------------------------
        // Tileset association
        // ------------------------------------------------------------------

        /// <summary>
        /// The tileset used by this layer.
        /// All cels on this layer reference tiles in this tileset.
        /// </summary>
        public Tileset? Tileset { get; set; }

        // ------------------------------------------------------------------
        // Grid
        // ------------------------------------------------------------------

        /// <summary>The tile grid dimensions for this layer's tilemap.</summary>
        public Grid Grid { get; set; } = new Grid(16, 16);

        // ------------------------------------------------------------------
        // Cel management (same as LayerImage)
        // ------------------------------------------------------------------

        public override IReadOnlyList<Cel> Cels => (IReadOnlyList<Cel>)_cels.Values;

        public override Cel? GetCel(int frame)
            => _cels.TryGetValue(frame, out var cel) ? cel : null;

        public void AddCel(Cel cel)
        {
            if (cel == null) throw new ArgumentNullException(nameof(cel));
            _cels[cel.Frame] = cel;
        }

        public bool RemoveCel(int frame) => _cels.Remove(frame);
    }
}
