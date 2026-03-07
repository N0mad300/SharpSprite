using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharpSprite.Core.Document;

namespace SharpSprite.App.ViewModels
{
    /// <summary>
    /// Represents one color swatch in the palette panel.
    /// </summary>
    public sealed class SwatchViewModel : ObservableObject
    {
        private Rgba32 _color;
        public Rgba32 Color
        {
            get => _color;
            set
            {
                _color = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HexColor));
                OnPropertyChanged(nameof(AvaloniaColor));
            }
        }

        public string HexColor => $"#{Color.R:X2}{Color.G:X2}{Color.B:X2}";

        /// <summary>Avalonia color string for binding to Background brushes.</summary>
        public string AvaloniaColor => $"#{Color.A:X2}{Color.R:X2}{Color.G:X2}{Color.B:X2}";

        public SwatchViewModel(Rgba32 color) => _color = color;
    }

    public partial class PaletteViewModel : ObservableObject
    {
        // ── Swatches ──────────────────────────────────────────────────────
        public ObservableCollection<SwatchViewModel> Swatches { get; } = new();

        // ── Selected colors ───────────────────────────────────────────────
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ForegroundHex))]
        [NotifyPropertyChangedFor(nameof(ForegroundAvaloniaColor))]
        private Rgba32 _foregroundColor = new Rgba32(0, 0, 0, 255);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BackgroundHex))]
        [NotifyPropertyChangedFor(nameof(BackgroundAvaloniaColor))]
        private Rgba32 _backgroundColor = new Rgba32(255, 255, 255, 255);

        public string ForegroundHex => $"#{ForegroundColor.R:X2}{ForegroundColor.G:X2}{ForegroundColor.B:X2}";
        public string BackgroundHex => $"#{BackgroundColor.R:X2}{BackgroundColor.G:X2}{BackgroundColor.B:X2}";
        public string ForegroundAvaloniaColor => $"#{ForegroundColor.A:X2}{ForegroundColor.R:X2}{ForegroundColor.G:X2}{ForegroundColor.B:X2}";
        public string BackgroundAvaloniaColor => $"#{BackgroundColor.A:X2}{BackgroundColor.R:X2}{BackgroundColor.G:X2}{BackgroundColor.B:X2}";

        // ── Selected palette index ────────────────────────────────────────
        [ObservableProperty]
        private int _selectedIndex = -1;

        [ObservableProperty]
        private string _indexLabel = "Idx-0";

        // ── Commands ──────────────────────────────────────────────────────
        [RelayCommand]
        public void SelectSwatch(SwatchViewModel swatch)
        {
            ForegroundColor = swatch.Color;
            SelectedIndex = Swatches.IndexOf(swatch);
            IndexLabel = $"Idx-{SelectedIndex}";
        }

        [RelayCommand]
        public void RightClickSwatch(SwatchViewModel swatch)
        {
            BackgroundColor = swatch.Color;
        }

        [RelayCommand]
        public void SwapColors()
        {
            (ForegroundColor, BackgroundColor) = (BackgroundColor, ForegroundColor);
        }

        [RelayCommand]
        public void ResetColors()
        {
            ForegroundColor = new Rgba32(0, 0, 0, 255);
            BackgroundColor = new Rgba32(255, 255, 255, 255);
        }

        // ── Construction ──────────────────────────────────────────────────
        public PaletteViewModel()
        {
        }

        public void LoadFromPalette(Palette palette)
        {
            Swatches.Clear();
            for (int i = 0; i < palette.Count; i++)
            {
                var c = palette.GetColor(i);
                if (c.A > 0 && i < 32) // skip trailing transparent entries
                    Swatches.Add(new SwatchViewModel(c));
            }
        }

        public void LoadDefaultPalette()
        {
            // Aseprite-style default palette (DB16 + grays + common colors)
            var colors = new uint[]
            {
                // Row 1 – Aseprite default 16
                0xFF1A1C2C, 0xFF5D275D, 0xFF870025, 0xFFB13E53,
                0xFFEF7D57, 0xFFFFCD75, 0xFFA7F070, 0xFF38B764,
                0xFF257179, 0xFF29366F, 0xFF3B5DC9, 0xFF41A6F6,
                0xFF73EFF7, 0xFFF4F4F4, 0xFF94B0C2, 0xFF566C86,
                // Row 2 – grays
                0xFF333333, 0xFF555555, 0xFF777777, 0xFF999999,
                0xFFBBBBBB, 0xFFDDDDDD, 0xFFEEEEEE, 0xFFFFFFFF,
                // Row 3 – primaries + secondaries
                0xFFFF0000, 0xFF00FF00, 0xFF0000FF, 0xFFFFFF00,
                0xFFFF00FF, 0xFF00FFFF, 0xFFFF8800, 0xFF8800FF,
            };

            foreach (var packed in colors)
            {
                byte a = (byte)((packed >> 24) & 0xFF);
                byte r = (byte)((packed >> 16) & 0xFF);
                byte g = (byte)((packed >> 8) & 0xFF);
                byte b = (byte)(packed & 0xFF);
                Swatches.Add(new SwatchViewModel(new Rgba32(r, g, b, a)));
            }
        }
    }
}
