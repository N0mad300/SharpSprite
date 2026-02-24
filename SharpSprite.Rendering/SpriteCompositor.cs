using System.Runtime.InteropServices;
using SharpSprite.Core.Document;
using SkiaSharp;

namespace SharpSprite.Rendering
{
    /// <summary>
    /// Composites all visible layers of a sprite frame into a single
    /// <see cref="SKBitmap"/> that the canvas control can draw.
    ///
    /// Strategy
    /// --------
    /// We own a pinned <c>byte[]</c> compositing buffer (RGBA8888).  On each
    /// Composite() call we clear it to the checkerboard / transparent colour,
    /// then walk layers bottom-to-top, alpha-blending each cel into the buffer.
    ///
    /// For blend modes other than Normal the compositing is delegated to an
    /// off-screen <see cref="SKSurface"/> so we get Skia's GPU-quality blending
    /// for free.  Normal mode is handled in software for speed (avoids surface
    /// round-trips on every keystroke).
    ///
    /// Thread safety: not thread-safe.  Call only from the UI thread, or
    /// guard externally.
    /// </summary>
    public sealed class SpriteCompositor : IDisposable
    {
        // ------------------------------------------------------------------
        // Private state
        // ------------------------------------------------------------------

        // The flat RGBA8888 buffer that backs _bitmap.
        private byte[] _buffer = Array.Empty<byte>();
        private GCHandle _bufferHandle;

        // The SKBitmap that wraps _buffer via InstallPixels (zero-copy).
        private SKBitmap? _bitmap;

        // Off-screen surface used only for non-Normal blend modes.
        private SKSurface? _blendSurface;
        private int _blendSurfaceW, _blendSurfaceH;

        private bool _disposed;

        // ------------------------------------------------------------------
        // Public surface
        // ------------------------------------------------------------------

        /// <summary>
        /// The composited output bitmap.
        /// Valid after the first <see cref="Composite"/> call.
        /// Do not dispose this bitmap – it is owned by the compositor.
        /// </summary>
        public SKBitmap? Bitmap => _bitmap;

        // ------------------------------------------------------------------
        // Main composite entry point
        // ------------------------------------------------------------------

        /// <summary>
        /// Composite all visible layers of <paramref name="sprite"/> at
        /// <paramref name="frame"/> into <see cref="Bitmap"/>.
        /// </summary>
        public void Composite(Sprite sprite, int frame)
        {
            EnsureBuffer(sprite.Width, sprite.Height);

            // 1. Clear to transparent (checkerboard is drawn by the canvas control)
            Array.Clear(_buffer, 0, _buffer.Length);

            var palette = sprite.GetPalette(frame);

            // 2. Walk layers bottom-to-top
            foreach (var (layer, cel) in sprite.CelsAtFrame(frame))
            {
                if (!IsLayerChainVisible(layer)) continue;

                // Resolve linked cel
                var image = ResolveImage(layer, cel, sprite, frame);
                if (image == null) continue;

                byte layerOpacity = layer.Opacity;
                byte celOpacity = cel.Opacity;
                // Combined opacity: (layerOpacity * celOpacity) / 255
                byte combinedOpacity = (byte)((layerOpacity * celOpacity + 127) / 255);

                if (layer.BlendMode == BlendMode.Normal)
                {
                    // Fast software path
                    ImageConverter.BlitToRgba(
                        image, palette, sprite.TransparentIndex,
                        _buffer, sprite.Width, sprite.Height,
                        cel.X, cel.Y, combinedOpacity);
                }
                else
                {
                    // Skia off-screen blend path
                    CompositeWithSkiaBlend(
                        image, palette, sprite, frame,
                        cel.X, cel.Y, combinedOpacity, layer.BlendMode);
                }
            }

            // 3. Notify SKBitmap that pixels changed
            _bitmap!.NotifyPixelsChanged();
        }

        // ------------------------------------------------------------------
        // Buffer management
        // ------------------------------------------------------------------

        private void EnsureBuffer(int width, int height)
        {
            int needed = width * height * 4;
            if (_buffer.Length == needed && _bitmap != null) return;

            // Release old resources
            FreeBuffer();

            _buffer = new byte[needed];
            _bufferHandle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);

            var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            _bitmap = new SKBitmap();
            _bitmap.InstallPixels(info, _bufferHandle.AddrOfPinnedObject(), info.RowBytes);
        }

        private void FreeBuffer()
        {
            _bitmap?.Dispose();
            _bitmap = null;

            if (_bufferHandle.IsAllocated)
                _bufferHandle.Free();

            _buffer = Array.Empty<byte>();
        }

        // ------------------------------------------------------------------
        // Non-Normal blend mode path
        // ------------------------------------------------------------------

        private void CompositeWithSkiaBlend(
            Image image, Palette palette, Sprite sprite, int frame,
            int celX, int celY, byte opacity, BlendMode blendMode)
        {
            int w = sprite.Width;
            int h = sprite.Height;

            // Lazily create/resize the off-screen surface
            if (_blendSurface == null || _blendSurfaceW != w || _blendSurfaceH != h)
            {
                _blendSurface?.Dispose();
                // Use Unpremul so our software composite matches expectations
                var surfInfo = new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Unpremul);
                _blendSurface = SKSurface.Create(surfInfo);
                _blendSurfaceW = w;
                _blendSurfaceH = h;
            }

            // ── Build the cel as an SKBitmap ─────────────────────────────────
            byte[] celBuffer = new byte[image.Width * image.Height * 4];
            ImageConverter.BlitToRgba(
                image, palette, sprite.TransparentIndex,
                celBuffer, image.Width, image.Height,
                0, 0, opacity);

            var celInfo = new SKImageInfo(image.Width, image.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            // Copy into a managed SKBitmap (safe, no pinning needed for temp)
            using var celBitmap = SKBitmap.Decode(celBuffer, celInfo);

            // ── Composite onto the surface ───────────────────────────────────
            var canvas = _blendSurface.Canvas;
            canvas.Clear(SKColors.Transparent);

            // First, stamp the existing _buffer as the destination
            var dstInfo = new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            // _bufferHandle is already pinned; reuse the pointer directly
            IntPtr bufPtr = _bufferHandle.AddrOfPinnedObject();
            using var dstBitmap = new SKBitmap();
            dstBitmap.InstallPixels(dstInfo, bufPtr, dstInfo.RowBytes);
            canvas.DrawBitmap(dstBitmap, 0, 0);

            // Then overlay the cel with the requested blend mode
            using var blendPaint = new SKPaint
            {
                BlendMode = BlendModeConverter.ToSkia(blendMode),
                IsAntialias = false,
            };
            canvas.DrawBitmap(celBitmap, celX, celY, blendPaint);

            // ── Read the result back into _buffer ────────────────────────────
            // ReadPixels writes directly to our pinned buffer pointer
            using var snap = _blendSurface.Snapshot();
            using var readPM = new SKPixmap(dstInfo, bufPtr, dstInfo.RowBytes);
            snap.ReadPixels(readPM);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>Walk up the layer hierarchy; the layer is only visible if
        /// every ancestor group is also visible.</summary>
        private static bool IsLayerChainVisible(Layer layer)
        {
            Layer? current = layer;
            while (current != null)
            {
                if (!current.IsVisible) return false;
                current = current.Parent;
            }
            return true;
        }

        /// <summary>Resolve the image for a cel, following links.</summary>
        private static Image? ResolveImage(Layer layer, Cel cel, Sprite sprite, int frame)
        {
            if (!cel.IsLinked)
                return cel.Data?.Image;

            // Follow the link to the referenced frame
            if (cel.LinkedToFrame is int linkFrame)
            {
                var linkedCel = layer.GetCel(linkFrame);
                return linkedCel?.Data?.Image;
            }
            return null;
        }

        // ------------------------------------------------------------------
        // IDisposable
        // ------------------------------------------------------------------

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            FreeBuffer();
            _blendSurface?.Dispose();
        }
    }
}
