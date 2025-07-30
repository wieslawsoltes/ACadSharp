using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Viewer.Services;
using ACadSharp.Viewer.Controls;
using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace ACadSharp.Viewer.ViewModels;

/// <summary>
/// ViewModel for the CAD Preview Window
/// </summary>
public class CadPreviewWindowViewModel : ViewModelBase
{
    private CadDocument? _document;
    private double _previewZoom = 1.0;
    private Avalonia.Point _previewPanOffset = new Avalonia.Point(0, 0);
    private bool _showGrid = true;
    private Entity? _selectedEntity;
    private CadPreviewControl? _previewControl;

    public CadPreviewWindowViewModel()
    {
        // Commands
        FitToViewCommand = ReactiveCommand.Create(FitToView);
        ResetViewCommand = ReactiveCommand.Create(ResetView);
        PrintPreviewCommand = ReactiveCommand.CreateFromTask(ShowPrintPreviewAsync);

        // Update zoom text when preview zoom changes
        this.WhenAnyValue(x => x.PreviewZoom)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(ZoomText)));
    }

    /// <summary>
    /// CAD document to preview
    /// </summary>
    public CadDocument? Document
    {
        get => _document;
        set => this.RaiseAndSetIfChanged(ref _document, value);
    }

    /// <summary>
    /// Document title for display
    /// </summary>
    public string DocumentTitle => Document != null ? "CAD Preview" : "No Document Loaded";

    /// <summary>
    /// Preview zoom level
    /// </summary>
    public double PreviewZoom
    {
        get => _previewZoom;
        set => this.RaiseAndSetIfChanged(ref _previewZoom, value);
    }

    /// <summary>
    /// Preview pan offset
    /// </summary>
    public Avalonia.Point PreviewPanOffset
    {
        get => _previewPanOffset;
        set => this.RaiseAndSetIfChanged(ref _previewPanOffset, value);
    }

    /// <summary>
    /// Whether to show grid in preview
    /// </summary>
    public bool ShowGrid
    {
        get => _showGrid;
        set => this.RaiseAndSetIfChanged(ref _showGrid, value);
    }

    /// <summary>
    /// Currently selected entity in the preview
    /// </summary>
    public Entity? SelectedEntity
    {
        get => _selectedEntity;
        set => this.RaiseAndSetIfChanged(ref _selectedEntity, value);
    }

    /// <summary>
    /// Zoom percentage text for display
    /// </summary>
    public string ZoomText => $"{(PreviewZoom * 100):F0}%";

    /// <summary>
    /// Command to fit the CAD preview to view
    /// </summary>
    public ICommand FitToViewCommand { get; }

    /// <summary>
    /// Command to reset the CAD preview view
    /// </summary>
    public ICommand ResetViewCommand { get; }

    /// <summary>
    /// Command to show print preview
    /// </summary>
    public ICommand PrintPreviewCommand { get; }

    /// <summary>
    /// Sets the preview control reference
    /// </summary>
    /// <param name="previewControl">The preview control</param>
    public void SetPreviewControl(CadPreviewControl previewControl)
    {
        _previewControl = previewControl;
    }

    /// <summary>
    /// Fits the CAD preview to the view
    /// </summary>
    private void FitToView()
    {
        _previewControl?.FitToView();
    }

    /// <summary>
    /// Resets the CAD preview view to default
    /// </summary>
    private void ResetView()
    {
        _previewControl?.ResetView();
    }

    /// <summary>
    /// Shows print preview dialog
    /// </summary>
    private async Task ShowPrintPreviewAsync()
    {
        try
        {
            if (Document == null) return;

            // Create renderer for printing with same settings as preview
            var renderer = new SkiaSharpCadRenderer();
            
            // Copy layer visibility settings from preview control if available
            if (_previewControl?.GetRenderer() is SkiaSharpCadRenderer previewRenderer)
            {
                // TODO: Copy layer visibility settings from preview renderer to print renderer
                // This would require adding methods to expose and set layer visibility
            }
            
            var printService = new CadPrintService(renderer);
            
            // Show print preview
            var appLifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            if (appLifetime?.MainWindow is Avalonia.Controls.Window window)
            {
                await printService.ShowPrintPreviewAsync(window, Document);
            }
        }
        catch (Exception ex)
        {
            // Handle error - could show error dialog
            System.Diagnostics.Debug.WriteLine($"Print preview error: {ex.Message}");
        }
    }
}