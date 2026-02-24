using SharpSprite.Core.Document;

namespace SharpSprite.Rendering
{
    /// <summary>
    /// Converts pixels from the document's <see cref="Image"/> format
    /// into 32-bit RGBA8888 bytes suitable for a <see cref="SkiaSharp.SKBitmap"/>.
    /// </summary>
    internal static class ImageConverter
    {
        /// <summary>
        /// Write the contents of <paramref name="src"/> into <paramref name="dst"/>,
        /// starting at (<paramref name="dstX"/>, <paramref name="dstY"/>) within a
        /// canvas of <paramref name="dstWidth"/> × <paramref name="dstHeight"/> pixels.
        ///
        /// Only pixels that fall within the canvas bounds are written.
        /// The destination buffer is assumed to be RGBA8888 (4 bytes per pixel).
        /// </summary>
        public static void BlitToRgba(
            Image src,
            Palette palette,
            int transparentIndex,
            byte[] dst,
            int dstWidth,
            int dstHeight,
            int dstX,
            int dstY,
            byte opacity)
        {
            int srcW = src.Width;
            int srcH = src.Height;

            for (int sy = 0; sy < srcH; sy++)
            {
                int dy = dstY + sy;
                if (dy < 0 || dy >= dstHeight) continue;

                for (int sx = 0; sx < srcW; sx++)
                {
                    int dx = dstX + sx;
                    if (dx < 0 || dx >= dstWidth) continue;

                    Rgba32 srcColor = SamplePixel(src, palette, transparentIndex, sx, sy);

                    // Apply layer opacity
                    if (opacity < 255)
                    {
                        srcColor = new Rgba32(
                            srcColor.R, srcColor.G, srcColor.B,
                            (byte)((srcColor.A * opacity + 127) / 255));
                    }

                    if (srcColor.A == 0) continue; // Fully transparent – nothing to composite

                    int dstOffset = (dy * dstWidth + dx) * 4;

                    if (srcColor.A == 255)
                    {
                        // Opaque fast-path
                        dst[dstOffset] = srcColor.R;
                        dst[dstOffset + 1] = srcColor.G;
                        dst[dstOffset + 2] = srcColor.B;
                        dst[dstOffset + 3] = 255;
                    }
                    else
                    {
                        // Alpha-blend (SrcOver) in software for the CPU compositor
                        AlphaBlend(dst, dstOffset, srcColor);
                    }
                }
            }
        }

        // -----------------------------------------------------------------------

        private static Rgba32 SamplePixel(Image img, Palette palette, int transparentIndex, int x, int y)
        {
            switch (img.ColorMode)
            {
                case ColorMode.Rgba:
                    return img.GetPixelRgba(x, y);

                case ColorMode.Grayscale:
                    {
                        var (v, a) = img.GetPixelGrayscale(x, y);
                        return new Rgba32(v, v, v, a);
                    }

                case ColorMode.Indexed:
                    {
                        byte idx = img.GetPixelIndexed(x, y);
                        if (idx == transparentIndex) return Rgba32.Transparent;
                        return palette.GetColor(idx);
                    }

                case ColorMode.Tilemap:
                    // Tilemap images are expanded by SpriteCompositor before this method is called.
                    // If we get here, just return transparent.
                    return Rgba32.Transparent;

                default:
                    return Rgba32.Transparent;
            }
        }

        private static void AlphaBlend(byte[] dst, int offset, Rgba32 src)
        {
            float sa = src.A / 255f;
            float da = dst[offset + 3] / 255f;
            float outA = sa + da * (1f - sa);
            if (outA < 1e-6f) return;

            dst[offset] = (byte)((src.R * sa + dst[offset] * da * (1f - sa)) / outA);
            dst[offset + 1] = (byte)((src.G * sa + dst[offset + 1] * da * (1f - sa)) / outA);
            dst[offset + 2] = (byte)((src.B * sa + dst[offset + 2] * da * (1f - sa)) / outA);
            dst[offset + 3] = (byte)(outA * 255f);
        }
    }
}
