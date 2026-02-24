using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SharpSprite.Core.Document;
using SharpSprite.Rendering;
using SkiaSharp;

namespace SharpSprite.App.Controls
{
    /// <summary>
    /// Avalonia custom control that renders a <see cref="Document"/> onto
    /// a Skia canvas with pan and integer-zoom support.
    ///
    /// Data flow:
    ///   Document changed  →  Composite()  →  InvalidateVisual()
    ///                                              ↓
    ///                                  Render() → PixelCanvasDrawOperation
    ///                                              ↓
    ///                                    canvas.DrawBitmap (nearest-neighbour)
    ///
    /// The compositor and its backing bitmap are owned by this control and
    /// disposed when the control is detached.
    /// </summary>
    public sealed class PixelCanvasControl : Control
    {
        // ------------------------------------------------------------------
        // Styled / Avalonia properties
        // ------------------------------------------------------------------

        /// <summary>The document to display.</summary>
        public static readonly StyledProperty<Document?> DocumentProperty =
            AvaloniaProperty.Register<PixelCanvasControl, Document?>(nameof(Document));

        /// <summary>
        /// The active frame index (0-based) to composite.
        /// Bind this to the timeline's current-frame observable.
        /// </summary>
        public static readonly StyledProperty<int> ActiveFrameProperty =
            AvaloniaProperty.Register<PixelCanvasControl, int>(nameof(ActiveFrame), defaultValue: 0);

        /// <summary>
        /// Zoom level expressed as an integer pixel multiplier (1 = 1:1, 2 = 2×, …).
        /// 0 means "fit to window" (auto-zoom).
        /// </summary>
        public static readonly StyledProperty<int> ZoomProperty =
            AvaloniaProperty.Register<PixelCanvasControl, int>(nameof(Zoom), defaultValue: 0);

        /// <summary>Canvas pan offset in screen pixels.</summary>
        public static readonly StyledProperty<Vector> PanOffsetProperty =
            AvaloniaProperty.Register<PixelCanvasControl, Vector>(nameof(PanOffset), defaultValue: default);

        // ------------------------------------------------------------------
        // CLR wrappers
        // ------------------------------------------------------------------

        public Document? Document
        {
            get => GetValue(DocumentProperty);
            set => SetValue(DocumentProperty, value);
        }

        public int ActiveFrame
        {
            get => GetValue(ActiveFrameProperty);
            set => SetValue(ActiveFrameProperty, value);
        }

        public int Zoom
        {
            get => GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
        }

        public Vector PanOffset
        {
            get => GetValue(PanOffsetProperty);
            set => SetValue(PanOffsetProperty, value);
        }

        // ------------------------------------------------------------------
        // Private state
        // ------------------------------------------------------------------

        private readonly SpriteCompositor _compositor = new();

        // Snapshot of the last composed bitmap, passed to the draw operation.
        // Updated on the UI thread before InvalidateVisual().
        private SKBitmap? _latestBitmap;

        private Document? _subscribedDocument;

        // ------------------------------------------------------------------
        // Avalonia overrides
        // ------------------------------------------------------------------

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == DocumentProperty)
            {
                // Unsubscribe from old document
                if (_subscribedDocument != null)
                    _subscribedDocument.Changed -= OnDocumentChanged;

                _subscribedDocument = change.NewValue as Document;

                // Subscribe to new document
                if (_subscribedDocument != null)
                    _subscribedDocument.Changed += OnDocumentChanged;

                RefreshComposite();
            }
            else if (change.Property == ActiveFrameProperty)
            {
                RefreshComposite();
            }
            else if (change.Property == ZoomProperty || change.Property == PanOffsetProperty)
            {
                // No need to re-composite, just redraw with updated transform
                InvalidateVisual();
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            if (_subscribedDocument != null)
                _subscribedDocument.Changed -= OnDocumentChanged;

            _compositor.Dispose();
        }

        // ------------------------------------------------------------------
        // Rendering
        // ------------------------------------------------------------------

        public override void Render(DrawingContext context)
        {
            if (_latestBitmap == null) return;

            var op = new PixelCanvasDrawOperation(
                new Rect(Bounds.Size),
                _latestBitmap,
                ComputeTransform(_latestBitmap.Width, _latestBitmap.Height));

            context.Custom(op);
        }

        // ------------------------------------------------------------------
        // Composition trigger
        // ------------------------------------------------------------------

        private void RefreshComposite()
        {
            var doc = Document;
            if (doc == null)
            {
                _latestBitmap = null;
                InvalidateVisual();
                return;
            }

            int frame = Math.Clamp(ActiveFrame, 0, doc.Sprite.FrameCount - 1);
            _compositor.Composite(doc.Sprite, frame);
            _latestBitmap = _compositor.Bitmap;
            InvalidateVisual();
        }

        private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
            => RefreshComposite();

        // ------------------------------------------------------------------
        // Transform helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Compute the pixel scale and top-left offset for the sprite bitmap
        /// so that it is centered in the control.
        /// </summary>
        private (float scale, float offsetX, float offsetY) ComputeTransform(int bmpW, int bmpH)
        {
            float scale;

            if (Zoom <= 0)
            {
                // Auto-fit: largest integer scale that fits, minimum 1
                float fitX = (float)Bounds.Width / bmpW;
                float fitY = (float)Bounds.Height / bmpH;
                float fit = Math.Min(fitX, fitY);
                scale = Math.Max(1f, (float)Math.Floor(fit));
            }
            else
            {
                scale = Zoom;
            }

            float offsetX = ((float)Bounds.Width - bmpW * scale) / 2f + (float)PanOffset.X;
            float offsetY = ((float)Bounds.Height - bmpH * scale) / 2f + (float)PanOffset.Y;

            return (scale, offsetX, offsetY);
        }

        // ------------------------------------------------------------------
        // Inner draw operation (captured by Avalonia's scene graph)
        // ------------------------------------------------------------------

        private sealed class PixelCanvasDrawOperation : ICustomDrawOperation
        {
            private readonly SKBitmap _bitmap;
            private readonly float _scale;
            private readonly float _offsetX;
            private readonly float _offsetY;

            public Rect Bounds { get; }

            public PixelCanvasDrawOperation(
                Rect bounds, SKBitmap bitmap,
                (float scale, float offsetX, float offsetY) transform)
            {
                Bounds = bounds;
                _bitmap = bitmap;
                (_scale, _offsetX, _offsetY) = transform;
            }

            public void Dispose() { /* bitmap is owned by SpriteCompositor */ }

            public bool HitTest(Point p) => Bounds.Contains(p);

            // Always return false so Avalonia re-renders when the scene changes.
            public bool Equals(ICustomDrawOperation? other) => false;

            public void Render(ImmediateDrawingContext context)
            {
                var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (leaseFeature == null) return;

                using var lease = leaseFeature.Lease();
                var canvas = lease.SkCanvas;

                canvas.Save();

                // Draw checkerboard background behind the sprite
                DrawCheckerboard(canvas, _offsetX, _offsetY,
                    _bitmap.Width * _scale, _bitmap.Height * _scale, _scale);

                // Apply pan + integer zoom
                canvas.Translate(_offsetX, _offsetY);
                canvas.Scale(_scale);

                // Nearest-neighbour sampling (pixel art!)
                var sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
                using var paint = new SKPaint { IsAntialias = false };

                var image = SKImage.FromBitmap(_bitmap);

                canvas.DrawImage(image, 0, 0, sampling, paint);

                canvas.Restore();
            }

            /// <summary>
            /// Draw a classic grey checkerboard to indicate transparency.
            /// The checker cell size is scaled with the zoom level so it always
            /// looks like 8×8 screen pixels at 1× zoom.
            /// </summary>
            private static void DrawCheckerboard(
                SKCanvas canvas, float x, float y,
                float width, float height, float scale)
            {
                const int CellPx = 8; // cell size at zoom 1
                float cell = CellPx * scale;
                if (cell < 2) cell = 2;

                using var darkPaint = new SKPaint { Color = new SKColor(0x80, 0x80, 0x80) };
                using var lightPaint = new SKPaint { Color = new SKColor(0xA0, 0xA0, 0xA0) };

                int cols = (int)Math.Ceiling(width / cell);
                int rows = (int)Math.Ceiling(height / cell);

                for (int row = 0; row < rows; row++)
                    for (int col = 0; col < cols; col++)
                    {
                        float cx = x + col * cell;
                        float cy = y + row * cell;
                        float cw = Math.Min(cell, x + width - cx);
                        float ch = Math.Min(cell, y + height - cy);

                        var rect = new SKRect(cx, cy, cx + cw, cy + ch);
                        var paint = ((row + col) % 2 == 0) ? lightPaint : darkPaint;
                        canvas.DrawRect(rect, paint);
                    }
            }
        }
    }
}
