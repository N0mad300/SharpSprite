using System.Runtime.CompilerServices;

namespace SharpSprite.Core.Document
{
    // -------------------------------------------------------------------------
    // Image
    // -------------------------------------------------------------------------

    /// <summary>
    /// A rectangular pixel buffer.  This is the raw pixel data held by a <see cref="Cel"/>
    /// or a tile inside a <see cref="Tileset"/>.
    /// <para>
    /// <list type="bullet">
    ///   <item><see cref="ColorMode.Rgba"/>      – 4 bytes / pixel (ABGR packed as uint)</item>
    ///   <item><see cref="ColorMode.Grayscale"/> – 2 bytes / pixel (value + alpha, packed as ushort)</item>
    ///   <item><see cref="ColorMode.Indexed"/>   – 1 byte  / pixel (palette index as byte)</item>
    ///   <item><see cref="ColorMode.Tilemap"/>   – 4 bytes / pixel (tile reference as uint)</item>
    /// </list>
    /// </para>
    /// <para>
    /// All four cases are stored in a single <c>byte[]</c> buffer; typed accessors are
    /// provided for each mode so callers do not need to cast manually.
    /// </para>
    /// </summary>
    public sealed class Image
    {
        // ------------------------------------------------------------------
        // Fields
        // ------------------------------------------------------------------

        private byte[] _data;

        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        public Image(int width, int height, ColorMode colorMode)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            Width = width;
            Height = height;
            ColorMode = colorMode;
            BytesPerPixel = GetBytesPerPixel(colorMode);
            _data = new byte[width * height * BytesPerPixel];
        }

        // ------------------------------------------------------------------
        // Properties
        // ------------------------------------------------------------------

        public int Width { get; private set; }
        public int Height { get; private set; }
        public ColorMode ColorMode { get; }
        public int BytesPerPixel { get; }

        /// <summary>Row stride in bytes.</summary>
        public int RowBytes => Width * BytesPerPixel;

        /// <summary>Direct access to the underlying byte buffer.</summary>
        public ReadOnlySpan<byte> Data => _data;

        /// <summary>Writable span of the underlying byte buffer.</summary>
        public Span<byte> DataWritable => _data;

        // ------------------------------------------------------------------
        // Pixel access – RGBA mode
        // ------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Rgba32 GetPixelRgba(int x, int y)
        {
            AssertMode(ColorMode.Rgba);
            int offset = (y * Width + x) * 4;
            return new Rgba32(
                (uint)(_data[offset] | (_data[offset + 1] << 8) | (_data[offset + 2] << 16) | (_data[offset + 3] << 24)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPixelRgba(int x, int y, Rgba32 color)
        {
            AssertMode(ColorMode.Rgba);
            int offset = (y * Width + x) * 4;
            uint p = color.Packed;
            _data[offset] = (byte)(p & 0xFF);
            _data[offset + 1] = (byte)((p >> 8) & 0xFF);
            _data[offset + 2] = (byte)((p >> 16) & 0xFF);
            _data[offset + 3] = (byte)((p >> 24) & 0xFF);
        }

        // ------------------------------------------------------------------
        // Pixel access – Grayscale mode  (value=low byte, alpha=high byte)
        // ------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (byte Value, byte Alpha) GetPixelGrayscale(int x, int y)
        {
            AssertMode(ColorMode.Grayscale);
            int offset = (y * Width + x) * 2;
            return (_data[offset], _data[offset + 1]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPixelGrayscale(int x, int y, byte value, byte alpha = 255)
        {
            AssertMode(ColorMode.Grayscale);
            int offset = (y * Width + x) * 2;
            _data[offset] = value;
            _data[offset + 1] = alpha;
        }

        // ------------------------------------------------------------------
        // Pixel access – Indexed mode
        // ------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetPixelIndexed(int x, int y)
        {
            AssertMode(ColorMode.Indexed);
            return _data[y * Width + x];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPixelIndexed(int x, int y, byte index)
        {
            AssertMode(ColorMode.Indexed);
            _data[y * Width + x] = index;
        }

        // ------------------------------------------------------------------
        // Pixel access – Tilemap mode (uint tile reference)
        // ------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetTile(int x, int y)
        {
            AssertMode(ColorMode.Tilemap);
            int offset = (y * Width + x) * 4;
            return (uint)(_data[offset] | (_data[offset + 1] << 8) | (_data[offset + 2] << 16) | (_data[offset + 3] << 24));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTile(int x, int y, uint tileRef)
        {
            AssertMode(ColorMode.Tilemap);
            int offset = (y * Width + x) * 4;
            _data[offset] = (byte)(tileRef & 0xFF);
            _data[offset + 1] = (byte)((tileRef >> 8) & 0xFF);
            _data[offset + 2] = (byte)((tileRef >> 16) & 0xFF);
            _data[offset + 3] = (byte)((tileRef >> 24) & 0xFF);
        }

        // ------------------------------------------------------------------
        // Bulk operations
        // ------------------------------------------------------------------

        /// <summary>Fill the entire image with a transparent / zero pixel.</summary>
        public void Clear() => Array.Clear(_data, 0, _data.Length);

        /// <summary>Fill every pixel with the raw byte pattern <paramref name="value"/>.</summary>
        public void Fill(byte value) => _data.AsSpan().Fill(value);

        /// <summary>Copy all pixels from <paramref name="src"/> into this image.</summary>
        public void CopyFrom(Image src)
        {
            if (src.Width != Width || src.Height != Height || src.ColorMode != ColorMode)
                throw new ArgumentException("Source image dimensions/mode mismatch.");
            src._data.AsSpan().CopyTo(_data);
        }

        /// <summary>Create a deep copy of this image.</summary>
        public Image Clone()
        {
            var img = new Image(Width, Height, ColorMode);
            _data.AsSpan().CopyTo(img._data);
            return img;
        }

        /// <summary>
        /// Resize the image buffer.  Existing pixels are preserved in the
        /// intersection; new pixels are zeroed.
        /// </summary>
        public void Resize(int newWidth, int newHeight)
        {
            if (newWidth == Width && newHeight == Height) return;
            var newData = new byte[newWidth * newHeight * BytesPerPixel];
            int copyW = Math.Min(Width, newWidth) * BytesPerPixel;
            int copyH = Math.Min(Height, newHeight);
            for (int row = 0; row < copyH; row++)
            {
                int srcOff = row * Width * BytesPerPixel;
                int dstOff = row * newWidth * BytesPerPixel;
                Array.Copy(_data, srcOff, newData, dstOff, copyW);
            }
            _data = newData;
            Width = newWidth;
            Height = newHeight;
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        public static int GetBytesPerPixel(ColorMode mode) => mode switch
        {
            ColorMode.Rgba => 4,
            ColorMode.Grayscale => 2,
            ColorMode.Indexed => 1,
            ColorMode.Tilemap => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AssertMode(ColorMode expected)
        {
#if DEBUG
            if (ColorMode != expected)
                throw new InvalidOperationException(
                    $"Image is in {ColorMode} mode; expected {expected}.");
#endif
        }
    }
}
