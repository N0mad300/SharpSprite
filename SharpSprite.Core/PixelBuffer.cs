namespace SharpSprite.Core.Models
{
    // A hardware-agnostic Color struct (RGBA)
    public struct Rgba32
    {
        public byte R, G, B, A;

        public Rgba32(byte r, byte g, byte b, byte a)
        {
            R = r; G = g; B = b; A = a;
        }

        public static readonly Rgba32 Transparent = new(0, 0, 0, 0);
        public static readonly Rgba32 Red = new(255, 0, 0, 255);
        public static readonly Rgba32 White = new(255, 255, 255, 255);
    }

    public class PixelBuffer
    {
        public int Width { get; }
        public int Height { get; }
        public Rgba32[] Pixels { get; }

        public PixelBuffer(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new Rgba32[width * height];
        }

        public void SetPixel(int x, int y, Rgba32 color)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            Pixels[y * Width + x] = color;
        }

        public Rgba32 GetPixel(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return Rgba32.Transparent;
            return Pixels[y * Width + x];
        }
    }

    // Simple placeholder for the Document
    public class SpriteDocument
    {
        public PixelBuffer ActiveLayer { get; private set; }

        public SpriteDocument(int width, int height)
        {
            ActiveLayer = new PixelBuffer(width, height);

            // Initialization Test: Draw a diagonal line
            for (int i = 0; i < Math.Min(width, height); i++)
            {
                ActiveLayer.SetPixel(i, i, Rgba32.Red);
                ActiveLayer.SetPixel(i, height - i - 1, Rgba32.Red);
            }
        }
    }
}
