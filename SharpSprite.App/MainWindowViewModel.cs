using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharpSprite.Core.Document;

namespace SharpSprite.App.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private Document? _activeDocument;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FrameLabel))]
        private int _activeFrame;

        public string FrameLabel =>
            ActiveDocument == null
                ? "0 / 0"
                : $"{ActiveFrame + 1} / {ActiveDocument.Sprite.FrameCount}";

        [ObservableProperty]
        private string _statusText = "Ready";

        public MainWindowViewModel()
        {
            ActiveDocument = CreateDefaultDocument();
        }

        // FILE 
        [RelayCommand]
        private void NewDocument()
        {
            ActiveDocument = CreateDefaultDocument();
            ActiveFrame = 0;
            StatusText = "New document created (32x32)";
        }
        [RelayCommand] private void SaveDocument() => StatusText = "Save Document — not yet implemented";
        [RelayCommand] private void OpenDocument() => StatusText = "Open Document — not yet implemented";
        [RelayCommand] private void SaveAsDocument() => StatusText = "Save As Document — not yet implemented";
        [RelayCommand] private void ExportDocument() => StatusText = "Export Document — not yet implemented";
        [RelayCommand] private void ShareDocument() => StatusText = "Share Document — not yet implemented";
        [RelayCommand] private void CloseDocument() => StatusText = "Close Document — not yet implemented";
        [RelayCommand] private void CloseAllDocument() => StatusText = "Close All Document — not yet implemented";
        [RelayCommand] private void ImportSpriteSheet() => StatusText = "Import Sprite Sheet — not yet implemented";
        [RelayCommand] private void ExportSpriteSheet() => StatusText = "Export Sprite Sheet — not yet implemented";
        [RelayCommand] private void RepeatLastExport() => StatusText = "Repeat Last Export — not yet implemented";
        [RelayCommand] private void ExportTileset() => StatusText = "Export Tileset — not yet implemented";
        [RelayCommand]
        private void Exit()
        {
            if (Application.Current?.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime lifetime)
                lifetime.Shutdown();
        }

        // EDIT 
        [RelayCommand] private void Undo() => StatusText = "Undo — not yet implemented";
        [RelayCommand] private void Redo() => StatusText = "Redo — not yet implemented";
        [RelayCommand] private void UndoHistory() => StatusText = "Undo History — not yet implemented";
        [RelayCommand] private void Cut() => StatusText = "Cut — not yet implemented";
        [RelayCommand] private void Copy() => StatusText = "Copy — not yet implemented";
        [RelayCommand] private void CopyMerged() => StatusText = "Copy Merged — not yet implemented";
        [RelayCommand] private void Paste() => StatusText = "Paste — not yet implemented";
        [RelayCommand] private void PasteasNewSprite() => StatusText = "Paste as New Sprite — not yet implemented";
        [RelayCommand] private void PasteasNewLayer() => StatusText = "Paste as New Layer — not yet implemented";
        [RelayCommand] private void PasteasNewReferenceLayer() => StatusText = "Paste as New Reference Layer — not yet implemented";
        [RelayCommand] private void Delete() => StatusText = "Delete — not yet implemented";
        [RelayCommand] private void Fill() => StatusText = "Fill — not yet implemented";
        [RelayCommand] private void Stroke() => StatusText = "Stroke — not yet implemented";
        [RelayCommand] private void Rotate180() => StatusText = "Rotate 180 — not yet implemented";
        [RelayCommand] private void Rotate90CW() => StatusText = "Rotate 90 CW — not yet implemented";
        [RelayCommand] private void Rotate90CCW() => StatusText = "Rotate 90 CCW — not yet implemented";
        [RelayCommand] private void FlipHorizontal() => StatusText = "Flip Horizontal — not yet implemented";
        [RelayCommand] private void FlipVertical() => StatusText = "Flip Vertical — not yet implemented";
        [RelayCommand] private void Transform() => StatusText = "Transform — not yet implemented";
        [RelayCommand] private void ShiftLeft() => StatusText = "Shift Left — not yet implemented";
        [RelayCommand] private void ShiftRight() => StatusText = "Shift Right — not yet implemented";
        [RelayCommand] private void ShiftUp() => StatusText = "Shift Up — not yet implemented";
        [RelayCommand] private void ShiftDown() => StatusText = "Shift Down — not yet implemented";
        [RelayCommand] private void NewBrush() => StatusText = "New Brush — not yet implemented";
        [RelayCommand] private void NewSpriteFromSelection() => StatusText = "New Sprite From Selection — not yet implemented";
        [RelayCommand] private void ReplaceColor() => StatusText = "Replace Color — not yet implemented";
        [RelayCommand] private void Invert() => StatusText = "Invert — not yet implemented";
        [RelayCommand] private void AdjustmentsBrighnessContrast() => StatusText = "Brightness/Contrast — not yet implemented";
        [RelayCommand] private void AdjustmentsHueSaturation() => StatusText = "Hue/Saturation — not yet implemented";
        [RelayCommand] private void AdjustmentsColorCurve() => StatusText = "Color Curve — not yet implemented";
        [RelayCommand] private void FXOutline() => StatusText = "FX Outline — not yet implemented";
        [RelayCommand] private void FXConvulutionMatrix() => StatusText = "FX Convolution Matrix — not yet implemented";
        [RelayCommand] private void FXDespeckle() => StatusText = "FX Despeckle — not yet implemented";
        [RelayCommand] private void KeyboardShortcuts() => StatusText = "Keyboard Shortcuts — not yet implemented";
        [RelayCommand] private void Preferences() => StatusText = "Preferences — not yet implemented";

        // SPRITE 
        [RelayCommand] private void SpriteProperties() => StatusText = "Sprite Properties — not yet implemented";
        [RelayCommand] private void ColorModeRgbColor() => StatusText = "Color Mode: RGB";
        [RelayCommand] private void ColorModeGrayscaleColor() => StatusText = "Color Mode: Grayscale";
        [RelayCommand] private void ColorModeIndexed() => StatusText = "Color Mode: Indexed";
        [RelayCommand] private void ColorModeMoreOption() => StatusText = "Color Mode Options — not yet implemented";
        [RelayCommand] private void Duplicate() => StatusText = "Duplicate — not yet implemented";
        [RelayCommand] private void Spritesize() => StatusText = "Sprite Size — not yet implemented";
        [RelayCommand] private void Canvasize() => StatusText = "Canvas Size — not yet implemented";
        [RelayCommand] private void RotateCanva180() => StatusText = "Rotate Canvas 180 — not yet implemented";
        [RelayCommand] private void RotateCanva90CW() => StatusText = "Rotate Canvas 90 CW — not yet implemented";
        [RelayCommand] private void RotateCanva90CCW() => StatusText = "Rotate Canvas 90 CCW — not yet implemented";
        [RelayCommand] private void FlipCanvaHorizontal() => StatusText = "Flip Canvas Horizontal — not yet implemented";
        [RelayCommand] private void FlipCanvaVertical() => StatusText = "Flip Canvas Vertical — not yet implemented";
        [RelayCommand] private void Crop() => StatusText = "Crop — not yet implemented";
        [RelayCommand] private void Trim() => StatusText = "Trim — not yet implemented";

        // LAYER 
        [RelayCommand] private void LayerProperties() => StatusText = "Layer Properties — not yet implemented";
        [RelayCommand] private void LayerVisible() => StatusText = "Layer Visible — not yet implemented";
        [RelayCommand] private void LockLayer() => StatusText = "Lock Layer — not yet implemented";
        [RelayCommand] private void OpenGroup() => StatusText = "Open Group — not yet implemented";
        [RelayCommand] private void NewLayer() => StatusText = "New Layer — not yet implemented";
        [RelayCommand] private void NewGroup() => StatusText = "New Group — not yet implemented";
        [RelayCommand] private void NewLayerViaCopy() => StatusText = "New Layer Via Copy — not yet implemented";
        [RelayCommand] private void NewLayerViaCut() => StatusText = "New Layer Via Cut — not yet implemented";
        [RelayCommand] private void NewReferenceLayerFromFile() => StatusText = "New Reference Layer From File — not yet implemented";
        [RelayCommand] private void NewTilemapLayer() => StatusText = "New Tilemap Layer — not yet implemented";
        [RelayCommand] private void DeleteLayer() => StatusText = "Delete Layer — not yet implemented";
        [RelayCommand] private void ConvertToBackgroundLayer() => StatusText = "Convert To Background Layer — not yet implemented";
        [RelayCommand] private void ConvertToTilemap() => StatusText = "Convert To Tilemap — not yet implemented";
        [RelayCommand] private void DuplicateLayer() => StatusText = "Duplicate Layer — not yet implemented";
        [RelayCommand] private void MergeDown() => StatusText = "Merge Down — not yet implemented";
        [RelayCommand] private void Flatten() => StatusText = "Flatten — not yet implemented";
        [RelayCommand] private void FlattenVisible() => StatusText = "Flatten Visible — not yet implemented";


        // FRAME 
        [RelayCommand] private void FrameProperties() => StatusText = "Frame Properties — not yet implemented";
        [RelayCommand] private void CelProperties() => StatusText = "Cel Properties — not yet implemented";
        [RelayCommand] private void NewFrame() => StatusText = "New Frame — not yet implemented";
        [RelayCommand] private void NewEmptyFrame() => StatusText = "New Empty Frame — not yet implemented";
        [RelayCommand] private void DuplicateCels() => StatusText = "Duplicate Cel(s) — not yet implemented";
        [RelayCommand] private void DuplicateLinkedCels() => StatusText = "Duplicate Linked Cel(s) — not yet implemented";
        [RelayCommand] private void DeleteFrame() => StatusText = "Delete Frame — not yet implemented";
        [RelayCommand] private void PlayAnimation() => StatusText = "Play Animation — not yet implemented";
        [RelayCommand] private void PlayPreviewAnimation() => StatusText = "Play Preview Animation — not yet implemented";
        [RelayCommand] private void PlaybackSpeed025() => StatusText = "Playback Speed: 0.25x";
        [RelayCommand] private void PlaybackSpeed05() => StatusText = "Playback Speed: 0.5x";
        [RelayCommand] private void PlaybackSpeed1() => StatusText = "Playback Speed: 1x";
        [RelayCommand] private void PlaybackSpeed15() => StatusText = "Playback Speed: 1.5x";
        [RelayCommand] private void PlaybackSpeed2() => StatusText = "Playback Speed: 2x";
        [RelayCommand] private void PlaybackSpeed3() => StatusText = "Playback Speed: 3x";
        [RelayCommand] private void PlayOnce() => StatusText = "Play Once — not yet implemented";
        [RelayCommand] private void PlayAllFrames() => StatusText = "Play All Frames — not yet implemented";
        [RelayCommand] private void PlaySubtags() => StatusText = "Play Subtags — not yet implemented";
        [RelayCommand] private void RewindOnStop() => StatusText = "Rewind on Stop — not yet implemented";
        [RelayCommand] private void TagProperties() => StatusText = "Tag Properties — not yet implemented";
        [RelayCommand] private void NewTag() => StatusText = "New Tag — not yet implemented";
        [RelayCommand] private void DeleteTag() => StatusText = "Delete Tag — not yet implemented";
        [RelayCommand] private void FirstFrame() => StatusText = "First Frame — not yet implemented";
        [RelayCommand] private void PreviousFrame() => StatusText = "Previous Frame — not yet implemented";
        [RelayCommand] private void NextFrame() => StatusText = "Next Frame — not yet implemented";
        [RelayCommand] private void LastFrame() => StatusText = "Last Frame — not yet implemented";
        [RelayCommand] private void FirstFrameInTag() => StatusText = "First Frame in Tag — not yet implemented";
        [RelayCommand] private void LastFrameInTag() => StatusText = "Last Frame in Tag — not yet implemented";
        [RelayCommand] private void GoToFrame() => StatusText = "Go to Frame — not yet implemented";
        [RelayCommand] private void ConstantFrameRate() => StatusText = "Constant Frame Rate — not yet implemented";
        [RelayCommand] private void ReverseFrames() => StatusText = "Reverse Frames — not yet implemented";

        // SELECT
        [RelayCommand] private void SelectAll() => StatusText = "Select All — not yet implemented";
        [RelayCommand] private void Deselect() => StatusText = "Deselect — not yet implemented";
        [RelayCommand] private void Reselect() => StatusText = "Reselect — not yet implemented";
        [RelayCommand] private void InverseSelection() => StatusText = "Inverse Selection — not yet implemented";
        [RelayCommand] private void ColorRange() => StatusText = "Color Range — not yet implemented";
        [RelayCommand] private void ModifyBorder() => StatusText = "Modify Border — not yet implemented";
        [RelayCommand] private void ModifyExpand() => StatusText = "Modify Expand — not yet implemented";
        [RelayCommand] private void ModifyContract() => StatusText = "Modify Contract — not yet implemented";
        [RelayCommand] private void LoadFromMsk() => StatusText = "Load from MSK — not yet implemented";
        [RelayCommand] private void SaveToMsk() => StatusText = "Save to MSK — not yet implemented";

        // VIEW
        [RelayCommand] private void DuplicateView() => StatusText = "Duplicate View — not yet implemented";
        [RelayCommand] private void WorkspaceLayout() => StatusText = "Workspace Layout — not yet implemented";
        [RelayCommand] private void RunCommand() => StatusText = "Run Command — not yet implemented";
        [RelayCommand] private void Extras() => StatusText = "Extras — not yet implemented";
        [RelayCommand] private void ShowLayerEdges() => StatusText = "Show Layer Edges — not yet implemented";
        [RelayCommand] private void ShowSelectionEdges() => StatusText = "Show Selection Edges — not yet implemented";
        [RelayCommand] private void ShowGrid() => StatusText = "Show Grid — not yet implemented";
        [RelayCommand] private void ShowAutoGuides() => StatusText = "Show Auto Guides — not yet implemented";
        [RelayCommand] private void ShowSlices() => StatusText = "Show Slices — not yet implemented";
        [RelayCommand] private void ShowPixelGrid() => StatusText = "Show Pixel Grid — not yet implemented";
        [RelayCommand] private void ShowTileNumbers() => StatusText = "Show Tile Numbers — not yet implemented";
        [RelayCommand] private void ShowBrushPreview() => StatusText = "Show Brush Preview — not yet implemented";
        [RelayCommand] private void GridSettings() => StatusText = "Grid Settings — not yet implemented";
        [RelayCommand] private void SelectionAsGrid() => StatusText = "Selection as Grid — not yet implemented";
        [RelayCommand] private void SnapToGrid() => StatusText = "Snap to Grid — not yet implemented";
        [RelayCommand] private void TiledModeNone() => StatusText = "Tiled Mode: None";
        [RelayCommand] private void TiledBothAxes() => StatusText = "Tiled Mode: Both Axes";
        [RelayCommand] private void TiledXAxis() => StatusText = "Tiled Mode: X Axis";
        [RelayCommand] private void TiledYAxis() => StatusText = "Tiled Mode: Y Axis";
        [RelayCommand] private void SymmetryOptions() => StatusText = "Symmetry Options — not yet implemented";
        [RelayCommand] private void SetLoopSection() => StatusText = "Set Loop Section — not yet implemented";
        [RelayCommand] private void ShowOnionSkin() => StatusText = "Show Onion Skin — not yet implemented";
        [RelayCommand] private void Timeline() => StatusText = "Timeline — not yet implemented";
        [RelayCommand] private void Preview() => StatusText = "Preview — not yet implemented";
        [RelayCommand] private void PreviewHideOtherLayers() => StatusText = "Preview Hide Other Layers — not yet implemented";
        [RelayCommand] private void PreviewBrushPreview() => StatusText = "Preview Brush Preview — not yet implemented";
        [RelayCommand] private void AdvancedMode() => StatusText = "Advanced Mode — not yet implemented";
        [RelayCommand] private void FullScreenMode() => StatusText = "Full Screen Mode — not yet implemented";
        [RelayCommand] private void FullScreenPreview() => StatusText = "Full Screen Preview — not yet implemented";
        [RelayCommand] private void Home() => StatusText = "Home — not yet implemented";
        [RelayCommand] private void RefreshReloadTheme() => StatusText = "Refresh & Reload Theme — not yet implemented";

        // HELP 
        [RelayCommand] private void Readme() => StatusText = "Readme — not yet implemented";
        [RelayCommand] private void QuickReference() => StatusText = "Quick Reference — not yet implemented";
        [RelayCommand] private void Documentation() => StatusText = "Documentation — not yet implemented";
        [RelayCommand] private void Tutorial() => StatusText = "Tutorial — not yet implemented";
        [RelayCommand] private void ReleaseNotes() => StatusText = "Release Notes — not yet implemented";
        [RelayCommand] private void Twitter() => StatusText = "Twitter — not yet implemented";
        [RelayCommand] private void About() => StatusText = "About SharpSprite";

        private static Document CreateDefaultDocument()
        {
            // 32 × 32 RGBA sprite, one layer, one frame
            var doc = SpriteFactory.CreateBlankRgba(32, 32);

            // Paint a simple test pattern so the canvas is immediately visible
            var sprite = doc.Sprite;
            var layer = (LayerImage)sprite.Layers[0];
            var cel = layer.GetCel(0)!;
            var image = cel.Data.Image;

            for (int y = 0; y < image.Height; y++)
                for (int x = 0; x < image.Width; x++)
                {
                    // Quadrant colours
                    bool left = x < image.Width / 2;
                    bool top = y < image.Height / 2;
                    Rgba32 color = (left, top) switch
                    {
                        (true, true) => new Rgba32(0xFF, 0x00, 0x5F),   // hot pink
                        (false, true) => new Rgba32(0x00, 0xC8, 0xFF),   // cyan
                        (true, false) => new Rgba32(0xFF, 0xC8, 0x00),   // yellow
                        (false, false) => new Rgba32(0x00, 0xE0, 0x50),   // green
                    };
                    image.SetPixelRgba(x, y, color);
                }

            doc.IsModified = false; // fresh document
            return doc;
        }
    }
}