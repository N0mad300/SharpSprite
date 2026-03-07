using Avalonia.Controls;
using Avalonia.Input;
using SharpSprite.App.ViewModels;

namespace SharpSprite.App.Controls;

public partial class PaletteControl : UserControl
{
    public PaletteControl()
    {
        InitializeComponent();
    }

    private void Swatch_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed) return;
        if (sender is Button { DataContext: SwatchViewModel swatch } &&
            DataContext is PaletteViewModel vm)
        {
            vm.RightClickSwatchCommand.Execute(swatch);
            e.Handled = true;
        }
    }
}