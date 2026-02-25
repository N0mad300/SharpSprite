namespace SharpSprite.Core.Document
{
    // -------------------------------------------------------------------------
    // Rgba32 – packed 32-bit color helper
    // -------------------------------------------------------------------------

    /// <summary>
    /// A 32-bit RGBA color value.
    /// Stored as 0xAABBGGRR in memory (little-endian), exposed as individual
    /// channels for convenience.
    /// </summary>
    public readonly struct Rgba32 : IEquatable<Rgba32>
    {
        /// <summary>Raw packed value (ABGR little-endian).</summary>
        public readonly uint Packed;

        public byte R => (byte)(Packed & 0xFF);
        public byte G => (byte)((Packed >> 8) & 0xFF);
        public byte B => (byte)((Packed >> 16) & 0xFF);
        public byte A => (byte)((Packed >> 24) & 0xFF);

        public static readonly Rgba32 Transparent = new(0, 0, 0, 0);
        public static readonly Rgba32 Black = new(0, 0, 0, 255);
        public static readonly Rgba32 White = new(255, 255, 255, 255);

        public Rgba32(byte r, byte g, byte b, byte a = 255)
        {
            Packed = (uint)(r | (g << 8) | (b << 16) | (a << 24));
        }

        public Rgba32(uint packed) { Packed = packed; }

        public static Rgba32 FromArgb(byte a, byte r, byte g, byte b) => new(r, g, b, a);

        public bool Equals(Rgba32 other) => Packed == other.Packed;
        public override bool Equals(object? obj) => obj is Rgba32 c && Equals(c);
        public override int GetHashCode() => (int)Packed;
        public static bool operator ==(Rgba32 a, Rgba32 b) => a.Packed == b.Packed;
        public static bool operator !=(Rgba32 a, Rgba32 b) => a.Packed != b.Packed;
        public override string ToString() => $"rgba({R},{G},{B},{A})";
    }

    // -------------------------------------------------------------------------
    // Palette entry
    // -------------------------------------------------------------------------

    /// <summary>
    /// A single entry in a palette: a color plus optional user data.
    /// </summary>
    public sealed class PaletteEntry
    {
        public Rgba32 Color { get; set; }
        public UserData UserData { get; } = new();

        public PaletteEntry(Rgba32 color) { Color = color; }
        public PaletteEntry Clone()
        {
            var e = new PaletteEntry(Color);
            e.UserData.Text = UserData.Text;
            e.UserData.Color = UserData.Color;
            return e;
        }
    }

    // -------------------------------------------------------------------------
    // Palette
    // -------------------------------------------------------------------------

    /// <summary>
    /// A color palette for a sprite.
    /// Each palette is associated with the frame it takes effect from.
    /// </summary>
    public sealed class Palette
    {
        private readonly List<PaletteEntry> _entries;

        /// <summary>
        /// The first frame this palette applies to (0-based).
        /// Palette changes take effect at this frame and persist until the next
        /// palette change.
        /// </summary>
        public int Frame { get; set; } = 0;

        /// <summary>
        /// Optional name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Number of colors in this palette.</summary>
        public int Count => _entries.Count;

        public const int MaxEntries = 256;

        public Palette(int size = 256)
        {
            if (size < 1 || size > MaxEntries)
                throw new ArgumentOutOfRangeException(nameof(size));
            _entries = new List<PaletteEntry>(size);
            for (int i = 0; i < size; i++)
                _entries.Add(new PaletteEntry(Rgba32.Transparent));
        }

        public PaletteEntry this[int index] => _entries[index];

        public Rgba32 GetColor(int index) => _entries[index].Color;
        public void SetColor(int index, Rgba32 color) => _entries[index].Color = color;

        /// <summary>Resize the palette (adds transparent entries or removes from end).</summary>
        public void Resize(int newSize)
        {
            if (newSize < 1 || newSize > MaxEntries)
                throw new ArgumentOutOfRangeException(nameof(newSize));
            while (_entries.Count < newSize)
                _entries.Add(new PaletteEntry(Rgba32.Transparent));
            while (_entries.Count > newSize)
                _entries.RemoveAt(_entries.Count - 1);
        }

        public Palette Clone()
        {
            var p = new Palette(_entries.Count) { Frame = Frame, Name = Name };
            for (int i = 0; i < _entries.Count; i++)
                p._entries[i] = _entries[i].Clone();
            return p;
        }

        /// <summary>
        /// Finds the closest palette entry index for a given RGBA color
        /// using simple Euclidean distance in RGB space.
        /// </summary>
        public int FindBestMatch(Rgba32 color)
        {
            int best = 0;
            long bestDist = long.MaxValue;
            for (int i = 0; i < _entries.Count; i++)
            {
                var c = _entries[i].Color;
                int dr = c.R - color.R;
                int dg = c.G - color.G;
                int db = c.B - color.B;
                long dist = (long)dr * dr + (long)dg * dg + (long)db * db;
                if (dist < bestDist) { bestDist = dist; best = i; }
            }
            return best;
        }
    }
}
