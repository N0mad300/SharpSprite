using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharpSprite.Core.Models;

namespace SharpSprite.App.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private SpriteDocument _activeDocument;

        [ObservableProperty]
        private string _statusText = "Ready";

        public MainWindowViewModel()
        {
            ActiveDocument = new SpriteDocument(32, 32);
        }


        [RelayCommand]
        private void NewDocument()
        {
            ActiveDocument = new SpriteDocument(32, 32);
            StatusText = "New document created (32x32)";
        }
        [RelayCommand]
        private void OpenDocument()
        {
            StatusText = "Open — not yet implemented";
        }
        [RelayCommand]
        private void SaveDocument()
        {
            StatusText = "Save — not yet implemented";
        }
        [RelayCommand]
        private void SaveAsDocument()
        {
            StatusText = "Save As — not yet implemented";
        }
        [RelayCommand]
        private void ExportPng()
        {
            StatusText = "Export PNG — not yet implemented";
        }
        [RelayCommand]
        private void Exit()
        {
            if (Application.Current?.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                lifetime.Shutdown();
            }
        }


        [RelayCommand]
        private void Undo()
        {
            StatusText = "Undo — not yet implemented";
        }
        [RelayCommand]
        private void Redo()
        {
            StatusText = "Redo — not yet implemented";
        }
        [RelayCommand]
        private void ClearCanvas()
        {
            ActiveDocument?.ActiveLayer.Clear();
            StatusText = "Canvas cleared";
        }


        [RelayCommand]
        private void FlipHorizontal()
        {
            StatusText = "Flip Horizontal — not yet implemented ";
        }
        [RelayCommand]
        private void FlipVertical()
        {
            StatusText = "Flip Vertical — not yet implemented";
        }
        [RelayCommand]
        private void Rotate()
        {
            StatusText = "Rotate — not yet implemented";
        }
        [RelayCommand]
        private void Resize()
        {
            StatusText = "Resize — not yet implemented";
        }


        [RelayCommand]
        private void NewLayer()
        {
            StatusText = "NewLayer — not yet implemented";
        }
        [RelayCommand]
        private void DeleteLayer()
        {
            StatusText = "Delete Layer — not yet implemented";
        }
        [RelayCommand]
        private void MoveLayerDown()
        {
            StatusText = "Move Layer Down — not yet implement";
        }
        [RelayCommand]
        private void MoveLayerUp()
        {
            StatusText = "Move Layer Up — not yet implemented";
        }
        [RelayCommand]
        private void DuplicateLayer()
        {
            StatusText = "Duplicate Layer — not yet implemented";
        }


        [RelayCommand]
        private void NewFrame()
        {             
            StatusText = "New Frame — not yet implemented";
        }
        [RelayCommand]
        private void DeleteFrame()
        {
            StatusText = "Delete Frame — not yet implemented";
        }
        [RelayCommand]
        private void DuplicateFrame()
        {             
            StatusText = "Duplicate Frame — not yet implemented";
        }


        [RelayCommand]
        private void SelectAll()
        {             
            StatusText = "Select All — not yet implemented";
        }
        [RelayCommand]
        private void Deselect()
        {             
            StatusText = "Deselect — not yet implemented";
        }
        [RelayCommand]
        private void InvertSelection()
        {             
            StatusText = "Invert Selection — not yet implemented";
        }

        [RelayCommand]
        private void ZoomIn()
        {
            StatusText = "Zoom In — not yet implemented";
        }

        [RelayCommand]
        private void ZoomOut()
        {
            StatusText = "Zoom Out — not yet implemented";
        }

        [RelayCommand]
        private void ZoomFit()
        {
            StatusText = "Fit to Screen — not yet implemented";
        }

        [RelayCommand]
        private void ToggleGrid()
        {
            StatusText = "Toggle Grid — not yet implemented";
        }
        [RelayCommand]
        private void About()
        {
            StatusText = "SharpSprite v1.0 — A simple pixel art editor built with Avalonia";
        }



        [RelayCommand]
        private void SelectPencil() => StatusText = "Pencil selected";

        [RelayCommand]
        private void SelectEraser() => StatusText = "Eraser selected";

        [RelayCommand]
        private void SelectFill() => StatusText = "Fill selected";

        [RelayCommand]
        private void SelectLine() => StatusText = "Line selected";
    }
}