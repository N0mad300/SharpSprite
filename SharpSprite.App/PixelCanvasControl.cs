using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SharpSprite.Core.Models;
using SharpSprite.Rendering;
using SkiaSharp;

namespace SharpSprite.App.Controls
{
    public class PixelCanvasControl : Control
    {
        // Dependency Injection would be better here, but let's keep it simple
        private BitmapAdapter _adapter = new BitmapAdapter();

        // This property allows binding the Document from ViewModel
        public static readonly StyledProperty<SpriteDocument> DocumentProperty =
            AvaloniaProperty.Register<PixelCanvasControl, SpriteDocument>(nameof(Document));

        public SpriteDocument Document
        {
            get => GetValue(DocumentProperty);
            set => SetValue(DocumentProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == DocumentProperty && change.NewValue is SpriteDocument doc)
            {
                // Initialize the adapter with the new document's buffer
                _adapter.UpdateFromBuffer(doc.ActiveLayer);
                InvalidateVisual();
            }
        }

        public override void Render(DrawingContext context)
        {
            if (Document == null || _adapter.Bitmap == null) return;

            // Define the custom drawing operation
            var customDraw = new PixelCanvasDrawOperation(new Rect(0, 0, Bounds.Width, Bounds.Height), _adapter.Bitmap);
            context.Custom(customDraw);
        }

        // Inner class handling the actual Skia drawing
        class PixelCanvasDrawOperation : ICustomDrawOperation
        {
            private readonly SKBitmap _bitmap;
            public Rect Bounds { get; }

            public PixelCanvasDrawOperation(Rect bounds, SKBitmap bitmap)
            {
                Bounds = bounds;
                _bitmap = bitmap;
            }

            public void Dispose() { /* No cleanup needed for this simple op */ }

            public bool HitTest(Point p) => Bounds.Contains(p);

            public bool Equals(ICustomDrawOperation? other) => false; // Always redraw for now

            public void Render(ImmediateDrawingContext context)
            {
                // This is where we get the raw Skia Canvas
                var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (leaseFeature == null) return;

                using var lease = leaseFeature.Lease();
                var canvas = lease.SkCanvas;

                // 1. Save state
                canvas.Save();

                // 2. Handle Zoom/Centering (Simple "Fit to Screen" logic for Phase 1)
                float scale = Math.Min((float)Bounds.Width / _bitmap.Width, (float)Bounds.Height / _bitmap.Height);
                // Keep it pixel perfect (nearest neighbor)
                float pixelScale = (float)Math.Floor(scale);
                if (pixelScale < 1) pixelScale = 1;

                float offsetX = ((float)Bounds.Width - (_bitmap.Width * pixelScale)) / 2;
                float offsetY = ((float)Bounds.Height - (_bitmap.Height * pixelScale)) / 2;

                canvas.Translate(offsetX, offsetY);
                canvas.Scale(pixelScale);

                // 3. Draw the bitmap with Nearest Neighbor sampling for that "Pixel Art" look
                using var paint = new SKPaint
                {
                    IsAntialias = false
                };

                var image = SKImage.FromBitmap(_bitmap);

                canvas.DrawImage(image, 0, 0, new SKSamplingOptions(), paint);

                // 4. Restore state
                canvas.Restore();
            }
        }
    }
}
