using SharpSprite.Core.Document;
using SkiaSharp;

namespace SharpSprite.Rendering
{
    /// <summary>
    /// Maps the document-model <see cref="BlendMode"/> enum to SkiaSharp's
    /// <see cref="SKBlendMode"/>.  Keeping this in a single place means any
    /// future mode additions only touch one file.
    /// </summary>
    internal static class BlendModeConverter
    {
        public static SKBlendMode ToSkia(BlendMode mode) => mode switch
        {
            BlendMode.Normal => SKBlendMode.SrcOver,
            BlendMode.Multiply => SKBlendMode.Multiply,
            BlendMode.Screen => SKBlendMode.Screen,
            BlendMode.Overlay => SKBlendMode.Overlay,
            BlendMode.Darken => SKBlendMode.Darken,
            BlendMode.Lighten => SKBlendMode.Lighten,
            BlendMode.ColorDodge => SKBlendMode.ColorDodge,
            BlendMode.ColorBurn => SKBlendMode.ColorBurn,
            BlendMode.HardLight => SKBlendMode.HardLight,
            BlendMode.SoftLight => SKBlendMode.SoftLight,
            BlendMode.Difference => SKBlendMode.Difference,
            BlendMode.Exclusion => SKBlendMode.Exclusion,
            BlendMode.Hue => SKBlendMode.Hue,
            BlendMode.Saturation => SKBlendMode.Saturation,
            BlendMode.Color => SKBlendMode.Color,
            BlendMode.Luminosity => SKBlendMode.Luminosity,
            BlendMode.Addition => SKBlendMode.Plus,
            // Subtract / Divide have no direct Skia equivalent; fall back
            BlendMode.Subtract => SKBlendMode.Difference,
            BlendMode.Divide => SKBlendMode.Screen,
            _ => SKBlendMode.SrcOver,
        };
    }
}
