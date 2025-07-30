using ACadSharp;
using ACadSharp.Tables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;

namespace ACadSharp.Viewer.Controls;

public partial class LayerControlPanel : UserControl
{
    public static readonly StyledProperty<CadDocument?> DocumentProperty =
        AvaloniaProperty.Register<LayerControlPanel, CadDocument?>(nameof(Document));

    public static readonly DirectProperty<LayerControlPanel, ObservableCollection<LayerViewModel>> LayersProperty =
        AvaloniaProperty.RegisterDirect<LayerControlPanel, ObservableCollection<LayerViewModel>>(
            nameof(Layers),
            o => o.Layers);

    private readonly ObservableCollection<LayerViewModel> _layers = new();
    private CompositeDisposable? _subscriptions;

    public CadDocument? Document
    {
        get => GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public ObservableCollection<LayerViewModel> Layers => _layers;

    public event EventHandler<LayerVisibilityChangedEventArgs>? LayerVisibilityChanged;

    public LayerControlPanel()
    {
        InitializeComponent();
        DataContext = this;
        SetupSubscriptions();
    }

    private void SetupSubscriptions()
    {
        _subscriptions = new CompositeDisposable();

        this.GetObservable(DocumentProperty)
            .Subscribe(UpdateLayers)
            .DisposeWith(_subscriptions);
    }

    private void UpdateLayers(CadDocument? document)
    {
        _layers.Clear();

        if (document == null) return;

        foreach (var layer in document.Layers)
        {
            var layerVm = new LayerViewModel(layer);
            layerVm.VisibilityChanged += OnLayerVisibilityChanged;
            _layers.Add(layerVm);
        }
    }

    private void OnLayerVisibilityChanged(object? sender, EventArgs e)
    {
        if (sender is LayerViewModel layerVm)
        {
            LayerVisibilityChanged?.Invoke(this, new LayerVisibilityChangedEventArgs(
                layerVm.Name, layerVm.IsVisible));
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _subscriptions?.Dispose();
        
        // Clean up event handlers
        foreach (var layer in _layers)
        {
            layer.VisibilityChanged -= OnLayerVisibilityChanged;
        }
    }
}

public class LayerViewModel
{
    private readonly Layer _layer;
    private bool _isVisible;

    public LayerViewModel(Layer layer)
    {
        _layer = layer;
        _isVisible = layer.IsOn && !layer.Flags.HasFlag(LayerFlags.Frozen);
    }

    public string Name => _layer.Name;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                VisibilityChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool IsFrozen
    {
        get => _layer.Flags.HasFlag(LayerFlags.Frozen);
        set
        {
            var wasFrozen = _layer.Flags.HasFlag(LayerFlags.Frozen);
            if (wasFrozen != value)
            {
                if (value)
                    _layer.Flags |= LayerFlags.Frozen;
                else
                    _layer.Flags &= ~LayerFlags.Frozen;
                IsVisible = !value && _layer.IsOn;
            }
        }
    }

    public Brush ColorBrush
    {
        get
        {
            var color = _layer.Color;
            return new SolidColorBrush(Avalonia.Media.Color.FromRgb(color.R, color.G, color.B));
        }
    }

    public event EventHandler? VisibilityChanged;
}

public class LayerVisibilityChangedEventArgs : EventArgs
{
    public string LayerName { get; }
    public bool IsVisible { get; }

    public LayerVisibilityChangedEventArgs(string layerName, bool isVisible)
    {
        LayerName = layerName;
        IsVisible = isVisible;
    }
}