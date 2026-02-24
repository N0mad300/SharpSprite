namespace SharpSprite.Core.Document
{
    // -------------------------------------------------------------------------
    // CelData – shared pixel data (enables linked cels)
    // -------------------------------------------------------------------------

    /// <summary>
    /// The actual pixel data referenced by one or more <see cref="Cel"/>s.
    /// <para>
    /// Aseprite separates <c>CelData</c> from the <c>Cel</c> header so that
    /// multiple cels across different frames can share the same image memory
    /// ("linked cels").  When a <see cref="Cel"/> is linked, it points to
    /// another cel's <see cref="CelData"/> rather than owning its own copy.
    /// </para>
    /// </summary>
    public sealed class CelData
    {
        private Image _image;

        public CelData(Image image)
        {
            _image = image ?? throw new ArgumentNullException(nameof(image));
        }

        /// <summary>The pixel data for this cel.</summary>
        public Image Image
        {
            get => _image;
            set => _image = value ?? throw new ArgumentNullException(nameof(value));
        }

        public CelData Clone() => new(Image.Clone());
    }

    // -------------------------------------------------------------------------
    // Cel – a layer × frame intersection
    // -------------------------------------------------------------------------

    /// <summary>
    /// A cel is the unit of content at a specific (layer, frame) intersection.
    /// It carries position, opacity and a reference to its <see cref="CelData"/>.
    /// <para>
    /// A <em>linked cel</em> shares its <see cref="CelData"/> with the cel at
    /// <see cref="LinkedToFrame"/>.  When reading back, callers should resolve
    /// the link by walking the owning layer.
    /// </para>
    /// </summary>
    public sealed class Cel
    {
        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        /// <summary>Create a normal (non-linked) cel.</summary>
        public Cel(int frame, Image image, int x = 0, int y = 0)
        {
            Frame = frame;
            Data = new CelData(image);
            X = x;
            Y = y;
        }

        /// <summary>Create a linked cel that shares data with another frame.</summary>
        public static Cel CreateLinked(int frame, int linkedToFrame)
        {
            if (frame == linkedToFrame)
                throw new ArgumentException("A cel cannot link to itself.");
            return new Cel(frame, linkedToFrame);
        }

        // Private constructor for linked cels.
        private Cel(int frame, int linkedToFrame)
        {
            Frame = frame;
            LinkedToFrame = linkedToFrame;
            Data = null!; // resolved at runtime
        }

        // ------------------------------------------------------------------
        // Properties
        // ------------------------------------------------------------------

        /// <summary>Zero-based frame index this cel belongs to.</summary>
        public int Frame { get; internal set; }

        /// <summary>
        /// The <see cref="CelData"/> containing pixel data.
        /// For linked cels, this is <c>null</c> until resolved.
        /// </summary>
        public CelData Data { get; internal set; }

        /// <summary>True if this cel is linked to another cel's data.</summary>
        public bool IsLinked => LinkedToFrame.HasValue;

        /// <summary>
        /// If this is a linked cel, the frame whose data is referenced.
        /// </summary>
        public int? LinkedToFrame { get; private set; }

        /// <summary>Cel X position in sprite canvas coordinates.</summary>
        public int X { get; set; }

        /// <summary>Cel Y position in sprite canvas coordinates.</summary>
        public int Y { get; set; }

        /// <summary>Cel opacity 0-255 (255 = fully opaque).</summary>
        public byte Opacity { get; set; } = 255;

        /// <summary>Z-index offset within a single frame (range -32768..32767).</summary>
        public short ZIndex { get; set; } = 0;

        /// <summary>User-defined metadata (text + color).</summary>
        public UserData UserData { get; } = new();

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>Width of the cel image (0 if linked and not yet resolved).</summary>
        public int Width => Data?.Image.Width ?? 0;

        /// <summary>Height of the cel image (0 if linked and not yet resolved).</summary>
        public int Height => Data?.Image.Height ?? 0;

        /// <summary>
        /// Unlinks this cel by giving it its own copy of the referenced data.
        /// Callers must supply the resolved image from the link target.
        /// </summary>
        public void Unlink(Image imageFromTarget)
        {
            Data = new CelData(imageFromTarget.Clone());
            LinkedToFrame = null;
        }

        /// <summary>
        /// Point this cel's data at <paramref name="other"/>'s data, making it linked.
        /// </summary>
        public void LinkTo(Cel other)
        {
            if (other.IsLinked)
                throw new InvalidOperationException("Cannot link to another linked cel.");
            Data = other.Data;
            LinkedToFrame = other.Frame;
        }

        /// <summary>Deep-clone this cel (always produces an unlinked copy).</summary>
        public Cel Clone()
        {
            var img = Data?.Image.Clone() ?? new Image(1, 1, ColorMode.Rgba);
            var clone = new Cel(Frame, img, X, Y)
            {
                Opacity = Opacity,
                ZIndex = ZIndex,
            };
            clone.UserData.Text = UserData.Text;
            clone.UserData.Color = UserData.Color;
            return clone;
        }
    }
}
