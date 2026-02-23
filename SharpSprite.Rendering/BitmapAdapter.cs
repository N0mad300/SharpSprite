using SkiaSharp;
using SharpSprite.Core.Models;
using System.Runtime.InteropServices;

namespace SharpSprite.Rendering
{
    public class BitmapAdapter : IDisposable
    {
        private SKBitmap _bitmap;
        private GCHandle _handle;

        public SKBitmap Bitmap => _bitmap;

        public void UpdateFromBuffer(PixelBuffer buffer)
        {
            if (_bitmap == null || _bitmap.Width != buffer.Width || _bitmap.Height != buffer.Height)
            {
                Dispose();

                _handle = GCHandle.Alloc(buffer.Pixels, GCHandleType.Pinned);
                var ptr = _handle.AddrOfPinnedObject();

                var info = new SKImageInfo(buffer.Width, buffer.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                _bitmap = new SKBitmap();
                _bitmap.InstallPixels(info, ptr, info.RowBytes);
            }
        }

        public void Dispose()
        {
            _bitmap?.Dispose();
            if (_handle.IsAllocated) _handle.Free();
        }
    }
}
