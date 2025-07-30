using ACadSharp.Viewer.Controls;
using ACadSharp.Viewer.ViewModels;
using Avalonia.Controls;

namespace ACadSharp.Viewer.Views;

/// <summary>
/// CAD Preview Window
/// </summary>
public partial class CadPreviewWindow : Window
{
    public CadPreviewWindow()
    {
        InitializeComponent();
        SetupLayerPanel();
    }

    /// <summary>
    /// Constructor with view model
    /// </summary>
    /// <param name="viewModel">The view model to use</param>
    public CadPreviewWindow(CadPreviewWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void SetupLayerPanel()
    {
        // Setup layer visibility event handling
        var layerPanel = this.FindControl<LayerControlPanel>("LayerPanel");
        var cadPreview = this.FindControl<CadPreviewControl>("CadPreview");
        
        if (layerPanel != null && cadPreview != null)
        {
            layerPanel.LayerVisibilityChanged += (sender, args) =>
            {
                // Update renderer layer visibility when layer panel changes
                cadPreview.SetLayerVisibility(args.LayerName, args.IsVisible);
            };
        }
        
        // Set the preview control reference in the ViewModel
        // This needs to be done when DataContext is available
        this.DataContextChanged += (sender, e) =>
        {
            if (DataContext is CadPreviewWindowViewModel viewModel && cadPreview != null)
            {
                viewModel.SetPreviewControl(cadPreview);
            }
        };
        
        // Also set it immediately if DataContext is already available
        if (DataContext is CadPreviewWindowViewModel currentViewModel && cadPreview != null)
        {
            currentViewModel.SetPreviewControl(cadPreview);
        }
    }
}