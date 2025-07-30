using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Viewer.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CSMath;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace ACadSharp.Viewer.Controls;

/// <summary>
/// Avalonia control for CAD document preview using SkiaSharp
/// </summary>
public class CadPreviewControl : UserControl
{
    private SkiaCanvas? _canvasView;
    private SkiaSharpCadRenderer? _renderer;
    private CadDocument? _document;
    private CompositeDisposable? _subscriptions;
    
    // Entity selection
    private Entity? _selectedEntity;
    private readonly List<Entity> _visibleEntities = new();

    // Zoom and pan state
    private double _zoom = 1.0;
    private Avalonia.Point _panOffset = new Avalonia.Point(0, 0);
    private Avalonia.Point? _lastPointerPosition;
    private bool _isPanning;
    // private bool _isZooming; // Removed unused field

    // Dependency properties
    public static readonly StyledProperty<CadDocument?> DocumentProperty =
        AvaloniaProperty.Register<CadPreviewControl, CadDocument?>(nameof(Document));

    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<CadPreviewControl, double>(nameof(Zoom), 1.0);

    public static readonly StyledProperty<Avalonia.Point> PanOffsetProperty =
        AvaloniaProperty.Register<CadPreviewControl, Avalonia.Point>(nameof(PanOffset), new Avalonia.Point(0, 0));

    public static readonly StyledProperty<bool> ShowGridProperty =
        AvaloniaProperty.Register<CadPreviewControl, bool>(nameof(ShowGrid), true);

    public static readonly StyledProperty<Brush> BackgroundBrushProperty =
        AvaloniaProperty.Register<CadPreviewControl, Brush>(nameof(BackgroundBrush), new SolidColorBrush(Avalonia.Media.Colors.White));

    public static readonly StyledProperty<Entity?> SelectedEntityProperty =
        AvaloniaProperty.Register<CadPreviewControl, Entity?>(nameof(SelectedEntity));

    public static readonly DirectProperty<CadPreviewControl, IReadOnlyList<Entity>> VisibleEntitiesProperty =
        AvaloniaProperty.RegisterDirect<CadPreviewControl, IReadOnlyList<Entity>>(
            nameof(VisibleEntities),
            o => o.VisibleEntities);

    public CadDocument? Document
    {
        get => GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public Avalonia.Point PanOffset
    {
        get => GetValue(PanOffsetProperty);
        set => SetValue(PanOffsetProperty, value);
    }

    public bool ShowGrid
    {
        get => GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    public Brush BackgroundBrush
    {
        get => GetValue(BackgroundBrushProperty);
        set => SetValue(BackgroundBrushProperty, value);
    }

    public Entity? SelectedEntity
    {
        get => GetValue(SelectedEntityProperty);
        set => SetValue(SelectedEntityProperty, value);
    }

    public IReadOnlyList<Entity> VisibleEntities => _visibleEntities;

    public CadPreviewControl()
    {
        InitializeComponent();
        SetupPropertySubscriptions();
    }

    private void InitializeComponent()
    {
        _canvasView = new SkiaCanvas();
        _canvasView.PaintSurface = OnPaintSurface;
        
        // Enable input events
        // _canvasView.Background = Avalonia.Media.Brushes.Transparent; // SkiaCanvas doesn't have Background property
        
        Content = _canvasView;

        _renderer = new SkiaSharpCadRenderer();

        // Enable pointer events for interaction
        _canvasView.PointerPressed += OnPointerPressed;
        _canvasView.PointerMoved += OnPointerMoved;
        _canvasView.PointerReleased += OnPointerReleased;
        _canvasView.PointerWheelChanged += OnPointerWheelChanged;
    }

    private void SetupPropertySubscriptions()
    {
        _subscriptions = new CompositeDisposable();

        // Subscribe to property changes
        this.GetObservable(DocumentProperty)
            .Subscribe(doc => 
            {
                _document = doc;
                InvalidateVisual();
            })
            .DisposeWith(_subscriptions);

        this.GetObservable(ZoomProperty)
            .Subscribe(zoom => 
            {
                _zoom = zoom;
                InvalidateVisual();
            })
            .DisposeWith(_subscriptions);

        this.GetObservable(PanOffsetProperty)
            .Subscribe(offset => 
            {
                _panOffset = offset;
                InvalidateVisual();
            })
            .DisposeWith(_subscriptions);

        this.GetObservable(ShowGridProperty)
            .Subscribe(_ => InvalidateVisual())
            .DisposeWith(_subscriptions);

        this.GetObservable(BackgroundBrushProperty)
            .Subscribe(_ => InvalidateVisual())
            .DisposeWith(_subscriptions);

        this.GetObservable(SelectedEntityProperty)
            .Subscribe(entity => 
            {
                _selectedEntity = entity;
                InvalidateVisual();
            })
            .DisposeWith(_subscriptions);
    }

    private void OnPaintSurface(SKCanvas canvas)
    {
        var info = new SKImageInfo((int)_canvasView!.Bounds.Width, (int)_canvasView.Bounds.Height);

        System.Diagnostics.Debug.WriteLine($"OnPaintSurface called with bounds: {_canvasView.Bounds.Width}x{_canvasView.Bounds.Height}");

        // Clear the canvas
        canvas.Clear(GetBackgroundSkColor());

        if (_renderer == null)
        {
            System.Diagnostics.Debug.WriteLine("Renderer is null");
            return;
        }

        if (_document == null)
        {
            System.Diagnostics.Debug.WriteLine("Document is null");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"Rendering document with {_document.Entities.Count} entities");

        try
        {
            // Set up the canvas transform for zoom and pan
            canvas.Save();
            
            // Apply zoom and pan transformations
            canvas.Translate((float)_panOffset.X, (float)_panOffset.Y);
            canvas.Scale((float)_zoom, (float)_zoom);

            // Center the drawing in the view
            var docBounds = CalculateDocumentBounds();
            if (docBounds.HasValue)
            {
                var centerX = (float)(info.Width * 0.5 / _zoom);
                var centerY = (float)(info.Height * 0.5 / _zoom);
                var docCenterX = (float)((docBounds.Value.Min.X + docBounds.Value.Max.X) * 0.5);
                var docCenterY = (float)((docBounds.Value.Min.Y + docBounds.Value.Max.Y) * 0.5);
                
                canvas.Translate(centerX - docCenterX, centerY - docCenterY);
                
                System.Diagnostics.Debug.WriteLine($"Document bounds: {docBounds.Value.Min} to {docBounds.Value.Max}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Document bounds is null");
            }

            // Draw grid if enabled
            if (ShowGrid)
            {
                DrawGrid(canvas, info);
            }

            // Set the canvas for the renderer and render the document
            _renderer.SetCanvas(canvas);
            _renderer.RenderDocument(_document);

            canvas.Restore();
        }
        catch (Exception ex)
        {
            // Log error but don't crash the UI
            System.Diagnostics.Debug.WriteLine($"Error rendering CAD preview: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private void DrawGrid(SKCanvas canvas, SKImageInfo info)
    {
        const float gridSpacing = 10.0f;
        var gridPaint = new SKPaint
        {
            Color = SKColors.LightGray,
            StrokeWidth = 0.5f,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };

        var visibleRect = GetVisibleRect(info);
        
        // Draw vertical lines
        var startX = (float)(Math.Floor(visibleRect.Left / gridSpacing) * gridSpacing);
        for (float x = startX; x <= visibleRect.Right; x += gridSpacing)
        {
            canvas.DrawLine(x, visibleRect.Top, x, visibleRect.Bottom, gridPaint);
        }

        // Draw horizontal lines
        var startY = (float)(Math.Floor(visibleRect.Top / gridSpacing) * gridSpacing);
        for (float y = startY; y <= visibleRect.Bottom; y += gridSpacing)
        {
            canvas.DrawLine(visibleRect.Left, y, visibleRect.Right, y, gridPaint);
        }

        gridPaint.Dispose();
    }

    private SKRect GetVisibleRect(SKImageInfo info)
    {
        var left = (float)(-_panOffset.X / _zoom);
        var top = (float)(-_panOffset.Y / _zoom);
        var right = (float)((info.Width - _panOffset.X) / _zoom);
        var bottom = (float)((info.Height - _panOffset.Y) / _zoom);

        return new SKRect(left, top, right, bottom);
    }

    private BoundingBox? CalculateDocumentBounds()
    {
        if (_document == null)
            return null;

        try
        {
            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var maxX = double.MinValue;
            var maxY = double.MinValue;
            var hasEntities = false;

            foreach (var entity in _document.Entities)
            {
                try
                {
                    var bounds = entity.GetBoundingBox();
                    if (bounds.Extent != BoundingBoxExtent.Null)
                    {
                        minX = Math.Min(minX, bounds.Min.X);
                        minY = Math.Min(minY, bounds.Min.Y);
                        maxX = Math.Max(maxX, bounds.Max.X);
                        maxY = Math.Max(maxY, bounds.Max.Y);
                        hasEntities = true;
                    }
                }
                catch
                {
                    // Some entities might not support bounding box calculation
                }
            }

            if (!hasEntities)
                return new BoundingBox(XYZ.Zero, new XYZ(100, 100, 0));

            return new BoundingBox(new XYZ(minX, minY, 0), new XYZ(maxX, maxY, 0));
        }
        catch
        {
            return new BoundingBox(XYZ.Zero, new XYZ(100, 100, 0));
        }
    }

    private SKColor GetBackgroundSkColor()
    {
        if (BackgroundBrush is SolidColorBrush solidBrush)
        {
            var color = solidBrush.Color;
            return new SKColor(color.R, color.G, color.B, color.A);
        }
        return SKColors.White;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var position = e.GetPosition(_canvasView);
        _lastPointerPosition = position;

        var currentPoint = e.GetCurrentPoint(_canvasView);
        if (currentPoint.Properties.IsLeftButtonPressed || currentPoint.Properties.IsMiddleButtonPressed)
        {
            _isPanning = true;
            e.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var position = e.GetPosition(_canvasView);

        if (_isPanning && _lastPointerPosition.HasValue)
        {
            var deltaX = position.X - _lastPointerPosition.Value.X;
            var deltaY = position.Y - _lastPointerPosition.Value.Y;

            var newOffset = new Avalonia.Point(
                PanOffset.X + deltaX,
                PanOffset.Y + deltaY);

            SetValue(PanOffsetProperty, newOffset);
        }

        _lastPointerPosition = position;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isPanning = false;
        // _isZooming = false; // Removed unused field
        // _canvasView?.ReleasePointerCapture(e.Pointer); // SkiaCanvas doesn't support pointer capture
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        const double zoomFactor = 1.1;
        var delta = e.Delta.Y;

        var newZoom = delta > 0 ? Zoom * zoomFactor : Zoom / zoomFactor;
        newZoom = Math.Max(0.1, Math.Min(10.0, newZoom)); // Clamp zoom range

        SetValue(ZoomProperty, newZoom);
    }

    /// <summary>
    /// Fits the document to the view
    /// </summary>
    public void FitToView()
    {
        if (_document == null || _canvasView == null)
            return;

        var bounds = CalculateDocumentBounds();
        if (!bounds.HasValue)
            return;

        var docWidth = bounds.Value.Max.X - bounds.Value.Min.X;
        var docHeight = bounds.Value.Max.Y - bounds.Value.Min.Y;

        if (docWidth <= 0 || docHeight <= 0)
            return;

        var viewWidth = _canvasView.Bounds.Width;
        var viewHeight = _canvasView.Bounds.Height;

        if (viewWidth <= 0 || viewHeight <= 0)
            return;

        // Calculate zoom to fit
        var zoomX = viewWidth / docWidth * 0.9; // 90% of view to add padding
        var zoomY = viewHeight / docHeight * 0.9;
        var fitZoom = Math.Min(zoomX, zoomY);

        SetValue(ZoomProperty, fitZoom);
        SetValue(PanOffsetProperty, new Avalonia.Point(0, 0));
    }

    /// <summary>
    /// Resets zoom and pan to default values
    /// </summary>
    public void ResetView()
    {
        SetValue(ZoomProperty, 1.0);
        SetValue(PanOffsetProperty, new Avalonia.Point(0, 0));
    }

    private new void InvalidateVisual()
    {
        _canvasView?.InvalidateVisual();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _subscriptions?.Dispose();
        _renderer?.Dispose();
    }

    private void HandleEntitySelection(Avalonia.Point screenPosition)
    {
        if (_renderer == null || _document == null) return;

        // Convert screen coordinates to world coordinates
        var worldPosition = ScreenToWorld(screenPosition);
        
        // Find entity at position
        var selectedEntity = _renderer.GetEntityAt(new XY(worldPosition.X, worldPosition.Y));
        
        if (selectedEntity != null)
        {
            SelectedEntity = selectedEntity;
            _renderer.SetSelectedEntity(selectedEntity);
            InvalidateVisual();
        }
    }

    private XY ScreenToWorld(Avalonia.Point screenPoint)
    {
        // Convert screen coordinates to world coordinates accounting for zoom and pan
        var worldX = (screenPoint.X - _panOffset.X) / _zoom;
        var worldY = (screenPoint.Y - _panOffset.Y) / _zoom;
        return new XY(worldX, worldY);
    }

    private Avalonia.Point WorldToScreen(XY worldPoint)
    {
        // Convert world coordinates to screen coordinates
        var screenX = worldPoint.X * _zoom + _panOffset.X;
        var screenY = worldPoint.Y * _zoom + _panOffset.Y;
        return new Avalonia.Point(screenX, screenY);
    }

    /// <summary>
    /// Gets the renderer instance for external configuration
    /// </summary>
    public SkiaSharpCadRenderer? GetRenderer() => _renderer;

    /// <summary>
    /// Sets layer visibility
    /// </summary>
    public void SetLayerVisibility(string layerName, bool visible)
    {
        _renderer?.SetLayerVisibility(layerName, visible);
        InvalidateVisual();
    }
}