using Avalonia.Controls;
using SharpSprite.App.ViewModels;

namespace SharpSprite.App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }
}