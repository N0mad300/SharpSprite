using CommunityToolkit.Mvvm.ComponentModel;
using SharpSprite.Core.Models;

namespace SharpSprite.App.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private SpriteDocument _activeDocument;

        public MainWindowViewModel()
        {
            // Create a 32x32 canvas for testing
            ActiveDocument = new SpriteDocument(32, 32);
        }
    }
}
