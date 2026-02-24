namespace SharpSprite.Core.Document
{
    // -------------------------------------------------------------------------
    // Pixel format (color mode)
    // -------------------------------------------------------------------------

    /// <summary>
    /// The color mode of a sprite or image.
    /// </summary>
    public enum ColorMode
    {
        /// <summary>32-bit RGBA, 8 bits per channel.</summary>
        Rgba = 0,
        /// <summary>16-bit grayscale+alpha, 8 bits per channel.</summary>
        Grayscale = 1,
        /// <summary>8-bit indexed color (requires a palette).</summary>
        Indexed = 2,
        /// <summary>Tilemap image (each 32-bit pixel is a tile reference).</summary>
        Tilemap = 3,
    }

    // -------------------------------------------------------------------------
    // Blend modes (layer compositing)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Compositing blend mode applied to a layer.
    /// </summary>
    public enum BlendMode
    {
        Normal = 0,
        Multiply = 1,
        Screen = 2,
        Overlay = 3,
        Darken = 4,
        Lighten = 5,
        ColorDodge = 6,
        ColorBurn = 7,
        HardLight = 8,
        SoftLight = 9,
        Difference = 10,
        Exclusion = 11,
        Hue = 12,
        Saturation = 13,
        Color = 14,
        Luminosity = 15,
        Addition = 16,
        Subtract = 17,
        Divide = 18,
    }

    // -------------------------------------------------------------------------
    // Animation loop direction
    // -------------------------------------------------------------------------

    /// <summary>Direction in which a tag's frames are played.</summary>
    public enum AniDir
    {
        Forward = 0,
        Reverse = 1,
        PingPong = 2,
        PingPongReverse = 3,
    }

    // -------------------------------------------------------------------------
    // Layer flags
    // -------------------------------------------------------------------------

    /// <summary>Bitfield flags stored on every layer.</summary>
    [Flags]
    public enum LayerFlags
    {
        None = 0,
        Visible = 1 << 0,
        Editable = 1 << 1,
        LockMovement = 1 << 2,
        Background = 1 << 3,
        PreferLinkedCels = 1 << 4,
        Collapsed = 1 << 5,
        Reference = 1 << 6,
    }

    // -------------------------------------------------------------------------
    // Tilemap tile flags (per-tile in a tilemap image)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Each cell of a tilemap image is a 32-bit value.
    /// The lower bits hold the tile index; the upper bits hold flip flags.
    /// </summary>
    [Flags]
    public enum TileFlags : uint
    {
        None = 0,
        FlipX = 0x80000000u,
        FlipY = 0x40000000u,
        Rotate90 = 0x20000000u,
    }

    /// <summary>
    /// The tile index mask inside a tilemap cell value.
    /// </summary>
    public static class TileConstants
    {
        public const uint IndexMask = 0x1FFFFFFFu;
        /// <summary>Index 0 is always the empty / transparent tile.</summary>
        public const uint EmptyTile = 0;
    }

    // -------------------------------------------------------------------------
    // Frame index type alias helper
    // -------------------------------------------------------------------------

    // We use a type alias via a using directive
    // at the file level or just document it here.
    // C# doesn't have true type aliases for value types, so we use 'int' directly.

    // -------------------------------------------------------------------------
    // Pixel ratio (non-square pixels)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Pixel aspect ratio for the sprite.
    /// e.g. (1,1) for square pixels, (1,2) for double-height pixels.
    /// </summary>
    public readonly struct PixelRatio : IEquatable<PixelRatio>
    {
        public readonly int Width;
        public readonly int Height;

        public static readonly PixelRatio Square = new(1, 1);

        public PixelRatio(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            Width = width;
            Height = height;
        }

        public bool Equals(PixelRatio other) => Width == other.Width && Height == other.Height;
        public override bool Equals(object? obj) => obj is PixelRatio pr && Equals(pr);
        public override int GetHashCode() => HashCode.Combine(Width, Height);
        public static bool operator ==(PixelRatio a, PixelRatio b) => a.Equals(b);
        public static bool operator !=(PixelRatio a, PixelRatio b) => !a.Equals(b);
        public override string ToString() => $"{Width}:{Height}";
    }

    // -------------------------------------------------------------------------
    // Grid (used by tilemap layers and by the sprite canvas grid)
    // -------------------------------------------------------------------------

    /// <summary>
    /// A grid definition: position and cell size.
    /// Used for the sprite canvas grid and for tilemap tile size.
    /// </summary>
    public sealed class Grid
    {
        /// <summary>Tile / cell width in pixels.</summary>
        public int TileWidth { get; set; } = 16;
        /// <summary>Tile / cell height in pixels.</summary>
        public int TileHeight { get; set; } = 16;
        /// <summary>Grid origin X offset.</summary>
        public int OriginX { get; set; } = 0;
        /// <summary>Grid origin Y offset.</summary>
        public int OriginY { get; set; } = 0;

        public Grid() { }
        public Grid(int tileWidth, int tileHeight, int originX = 0, int originY = 0)
        {
            TileWidth = tileWidth;
            TileHeight = tileHeight;
            OriginX = originX;
            OriginY = originY;
        }

        public Grid Clone() => new(TileWidth, TileHeight, OriginX, OriginY);
    }

    // -------------------------------------------------------------------------
    // User data (custom text + color on almost every doc object)
    // -------------------------------------------------------------------------

    /// <summary>
    /// User-defined metadata that can be attached to cels, layers, slices,
    /// tags, tilesets and the sprite itself.
    /// </summary>
    public sealed class UserData
    {
        /// <summary>Freeform text string set by the user.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Optional RGBA color (packed as 0xRRGGBBAA).
        /// Null means "no color set".
        /// </summary>
        public uint? Color { get; set; }

        public bool HasText => !string.IsNullOrEmpty(Text);
        public bool HasColor => Color.HasValue;

        public UserData Clone() => new() { Text = Text, Color = Color };
    }
}
