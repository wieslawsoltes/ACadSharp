using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Objects;
using ACadSharp.Tables;
using ACadSharp.Viewer.Interfaces;
using CSMath;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ACadSharp.Viewer.Services;

/// <summary>
/// SkiaSharp-based CAD renderer implementation
/// </summary>
public class SkiaSharpCadRenderer : ICadRenderer
{
    private SKCanvas? _canvas;
    private SKPaint _strokePaint;
    private SKPaint _fillPaint;
    private SKPaint _textPaint;
    private readonly Stack<SKMatrix> _transformStack;
    private BoundingBox _boundingBox;
    private SKMatrix _currentTransform;
    
    // Entity selection and visibility
    private Entity? _selectedEntity;
    private readonly HashSet<string> _visibleLayers = new();
    private readonly Dictionary<Entity, SKRect> _entityBounds = new();
    
    // Performance optimization
    private bool _enableLevelOfDetail = true;
    private readonly List<Entity> _visibleEntities = new();

    public SkiaSharpCadRenderer()
    {
        _strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

        _fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        _textPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            Typeface = SKTypeface.Default,
            TextAlign = SKTextAlign.Left
        };

        _transformStack = new Stack<SKMatrix>();
        _currentTransform = SKMatrix.Identity;
    }

    public void BeginRender(BoundingBox boundingBox, Color backgroundColor = default)
    {
        _boundingBox = boundingBox;
        
        // Clear background if specified
        if (!backgroundColor.Equals(default(Color)) && _canvas != null)
        {
            var bgColor = ToSkiaColor(backgroundColor);
            _canvas.Clear(bgColor);
        }
    }

    public void EndRender()
    {
        // Any cleanup operations
    }

    public void SetCanvas(SKCanvas canvas)
    {
        _canvas = canvas;
    }

    public void RenderDocument(CadDocument document)
    {
        if (_canvas == null) return;

        // Calculate bounding box for the document
        var docBoundingBox = CalculateDocumentBoundingBox(document);
        BeginRender(docBoundingBox, new Color(255, 255, 255));

        // Initialize layer visibility
        InitializeLayerVisibility(document);

        // Clear entity tracking
        _entityBounds.Clear();
        _visibleEntities.Clear();

        var totalEntities = document.Entities.Count;
        var visibleEntities = 0;
        
        System.Diagnostics.Debug.WriteLine($"Rendering document with {totalEntities} total entities");
        System.Diagnostics.Debug.WriteLine($"Document bounding box: {docBoundingBox.Min} to {docBoundingBox.Max}");
        System.Diagnostics.Debug.WriteLine($"Visible layers: {string.Join(", ", _visibleLayers)}");

        // Render all entities in model space with layer filtering
        foreach (var entity in document.Entities)
        {
            if (IsEntityVisible(entity))
            {
                visibleEntities++;
                _visibleEntities.Add(entity);
                RenderEntity(entity);
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"Rendered {visibleEntities} out of {totalEntities} entities");

        EndRender();
    }

    public void RenderEntity(Entity entity)
    {
        if (_canvas == null) return;

        // Set entity properties
        var color = entity.GetActiveColor();
        var lineWeight = GetEntityLineWeight(entity);
        
        // Highlight selected entity
        if (entity == _selectedEntity)
        {
            SetPen(new Color(255, 0, 0), lineWeight + 2); // Highlight with red and thicker line
        }
        else
        {
            SetPen(color, lineWeight);
        }

        // Track entity bounds for hit testing
        var entityBounds = CalculateEntityBounds(entity);
        if (entityBounds.HasValue)
        {
            _entityBounds[entity] = ToSkiaRect(entityBounds.Value);
        }

        // Level of detail - skip small entities when zoomed out
        if (_enableLevelOfDetail && ShouldSkipEntityForLOD(entity, entityBounds))
        {
            return;
        }

        // Render based on entity type
        switch (entity)
        {
            case Line line:
                RenderLine(line);
                break;
            case Arc arc:
                RenderArc(arc);
                break;
            case Circle circle:
                RenderCircle(circle);
                break;
            case Ellipse ellipse:
                RenderEllipse(ellipse);
                break;
            case IPolyline polyline:
                RenderPolyline(polyline);
                break;
            case MText mtext:
                RenderMText(mtext);
                break;
            case IText text:
                RenderText(text);
                break;
            case ACadSharp.Entities.Point point:
                RenderPoint(point);
                break;
            case Insert insert:
                RenderInsert(insert);
                break;
            case Hatch hatch:
                RenderHatch(hatch);
                break;
            case Dimension dimension:
                RenderDimension(dimension);
                break;
            default:
                // Render unknown entity as a bounding box
                RenderUnknownEntity(entity);
                break;
        }
    }

    public void RenderLine(Line line)
    {
        if (_canvas == null) return;

        var start = TransformPoint(line.StartPoint);
        var end = TransformPoint(line.EndPoint);
        _canvas.DrawLine(start.X, start.Y, end.X, end.Y, _strokePaint);
    }

    public void RenderCircle(Circle circle)
    {
        if (_canvas == null) return;

        var center = TransformPoint(circle.Center);
        var radius = (float)(circle.Radius * GetScaleFactor());

        _canvas.DrawCircle(
            (float)center.X, (float)center.Y,
            radius,
            _strokePaint);
    }

    public void RenderArc(Arc arc)
    {
        if (_canvas == null) return;

        var center = TransformPoint(arc.Center);
        var radius = (float)(arc.Radius * GetScaleFactor());
        var startAngle = (float)(arc.StartAngle * 180.0 / Math.PI);
        var endAngle = (float)(arc.EndAngle * 180.0 / Math.PI);
        var sweepAngle = endAngle - startAngle;

        // Normalize angles
        if (sweepAngle < 0)
            sweepAngle += 360;

        var rect = new SKRect(
            (float)center.X - radius,
            (float)center.Y - radius,
            (float)center.X + radius,
            (float)center.Y + radius);

        using var path = new SKPath();
        path.AddArc(rect, startAngle, sweepAngle);
        _canvas.DrawPath(path, _strokePaint);
    }

    public void RenderEllipse(Ellipse ellipse)
    {
        if (_canvas == null) return;

        // Convert ellipse to polyline for rendering
        var vertices = GenerateEllipseVertices(ellipse, 64);
        RenderPolylineVertices(vertices);
    }

    public void RenderPolyline(IPolyline polyline)
    {
        if (_canvas == null) return;

        var vertices = polyline.Vertices.Select(v => v.Location);
        RenderPolylineVertices(vertices);
    }

    public void RenderText(IText text)
    {
        if (_canvas == null) return;

        var position = TransformPoint(text.InsertPoint);
        var textString = text.Value ?? string.Empty;
        var height = (float)(text.Height * GetScaleFactor());

        _textPaint.TextSize = height;
        _textPaint.Color = _strokePaint.Color;

        _canvas.DrawText(textString, position.X, position.Y, _textPaint);
    }

    public void RenderPoint(ACadSharp.Entities.Point point)
    {
        if (_canvas == null) return;

        var position = TransformPoint(point.Location);
        var pointSize = 2.0f * GetScaleFactor();

        _canvas.DrawCircle(
            position.X, position.Y,
            pointSize,
            _fillPaint);
    }

    public void RenderInsert(Insert insert)
    {
        if (_canvas == null) return;

        PushTransform();

        // Apply insert transformation
        var insertTransform = insert.GetTransform();
        ApplyTransform(insertTransform);

        // Render all entities in the block
        foreach (var entity in insert.Block.Entities)
        {
            RenderEntity(entity);
        }

        PopTransform();
    }

    public void SetTransform(Matrix4 transform)
    {
        _currentTransform = ToSkiaMatrix(transform);
        _canvas?.SetMatrix(_currentTransform);
    }

    public void PushTransform()
    {
        _transformStack.Push(_currentTransform);
    }

    public void PopTransform()
    {
        if (_transformStack.Count > 0)
        {
            _currentTransform = _transformStack.Pop();
            _canvas?.SetMatrix(_currentTransform);
        }
    }

    public void SetPen(Color color, float width, string lineType = "Continuous")
    {
        _strokePaint.Color = ToSkiaColor(color);
        _strokePaint.StrokeWidth = width;
        
        // TODO: Implement line type patterns
        // For now, just use solid lines
    }

    public void SetBrush(Color color)
    {
        _fillPaint.Color = ToSkiaColor(color);
    }

    private void RenderPolylineVertices(IEnumerable<IVector> vertices)
    {
        if (_canvas == null) return;

        var points = vertices.Select(v => TransformPoint(v)).ToArray();
        if (points.Length < 2) return;

        using var path = new SKPath();
        var firstPoint = points[0];
        path.MoveTo((float)firstPoint.X, (float)firstPoint.Y);

        for (int i = 1; i < points.Length; i++)
        {
            var point = points[i];
            path.LineTo((float)point.X, (float)point.Y);
        }

        _canvas.DrawPath(path, _strokePaint);
    }

    private BoundingBox CalculateDocumentBoundingBox(CadDocument document)
    {
        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        foreach (var entity in document.Entities)
        {
            var entityBounds = GetEntityBounds(entity);
            if (entityBounds.HasValue)
            {
                minX = Math.Min(minX, entityBounds.Value.Min.X);
                minY = Math.Min(minY, entityBounds.Value.Min.Y);
                maxX = Math.Max(maxX, entityBounds.Value.Max.X);
                maxY = Math.Max(maxY, entityBounds.Value.Max.Y);
            }
        }

        if (minX == double.MaxValue)
        {
            // No entities found, return default bounds
            return new BoundingBox(XYZ.Zero, new XYZ(100, 100, 0));
        }

        return new BoundingBox(new XYZ(minX, minY, 0), new XYZ(maxX, maxY, 0));
    }

    private BoundingBox? GetEntityBounds(Entity entity)
    {
        try
        {
            return entity.GetBoundingBox();
        }
        catch
        {
            // Some entities might not support bounding box calculation
            return null;
        }
    }

    private SKPoint TransformPoint(IVector point)
    {
        // Convert IVector to SKPoint - IVector is implemented by XYZ/XY in CSMath
        SKPoint skPoint;
        if (point is XYZ xyz)
        {
            skPoint = new SKPoint((float)xyz.X, (float)xyz.Y);
        }
        else if (point is XY xy)
        {
            skPoint = new SKPoint((float)xy.X, (float)xy.Y);
        }
        else
        {
            // Fallback - this shouldn't happen in normal use
            skPoint = new SKPoint(0, 0);
        }
        
        // Apply current transformation to the point
        return _currentTransform.MapPoint(skPoint);
    }

    private void ApplyTransform(Transform transform)
    {
        var skMatrix = ToSkiaMatrix(transform.Matrix);
        _currentTransform = SKMatrix.Concat(_currentTransform, skMatrix);
        _canvas?.SetMatrix(_currentTransform);
    }

    private float GetEntityLineWeight(Entity entity)
    {
        var lineWeight = entity.LineWeight;
        
        if (lineWeight == LineweightType.ByLayer && entity.Layer != null)
        {
            lineWeight = entity.Layer.LineWeight;
        }

        // Convert lineweight to pixels (basic conversion)
        return lineWeight switch
        {
            LineweightType.W0 => 0.25f,
            LineweightType.W5 => 0.5f,
            LineweightType.W9 => 0.9f,
            LineweightType.W13 => 1.3f,
            LineweightType.W15 => 1.5f,
            LineweightType.W18 => 1.8f,
            LineweightType.W20 => 2.0f,
            LineweightType.W25 => 2.5f,
            LineweightType.W30 => 3.0f,
            LineweightType.W35 => 3.5f,
            LineweightType.W40 => 4.0f,
            LineweightType.W50 => 5.0f,
            LineweightType.W53 => 5.3f,
            LineweightType.W60 => 6.0f,
            LineweightType.W70 => 7.0f,
            LineweightType.W80 => 8.0f,
            LineweightType.W90 => 9.0f,
            LineweightType.W100 => 10.0f,
            LineweightType.W106 => 10.6f,
            LineweightType.W120 => 12.0f,
            LineweightType.W140 => 14.0f,
            LineweightType.W158 => 15.8f,
            LineweightType.W200 => 20.0f,
            LineweightType.W211 => 21.1f,
            _ => 1.0f
        };
    }

    private float GetScaleFactor()
    {
        // Extract scale from current transform matrix
        return Math.Max(_currentTransform.ScaleX, _currentTransform.ScaleY);
    }

    private static SKColor ToSkiaColor(Color color)
    {
        return new SKColor((byte)color.R, (byte)color.G, (byte)color.B, 255);
    }

    private static SKMatrix ToSkiaMatrix(Matrix4 matrix)
    {
        // Matrix4 in CSMath has field properties like m00, m01, etc.
        return new SKMatrix(
            (float)matrix.m00, (float)matrix.m10, (float)matrix.m30,
            (float)matrix.m01, (float)matrix.m11, (float)matrix.m31,
            (float)matrix.m03, (float)matrix.m13, (float)matrix.m33);
    }

    public void Dispose()
    {
        _strokePaint?.Dispose();
        _fillPaint?.Dispose();
        _textPaint?.Dispose();
    }

    private IEnumerable<IVector> GenerateEllipseVertices(Ellipse ellipse, int numSegments)
    {
        // Use the ellipse's built-in method to generate vertices
        return ellipse.PolygonalVertexes(numSegments).Cast<IVector>();
    }

    // New methods for enhanced functionality

    public void SetSelectedEntity(Entity? entity)
    {
        _selectedEntity = entity;
    }

    public Entity? GetEntityAt(XY point)
    {
        var skPoint = new SKPoint((float)point.X, (float)point.Y);
        return _entityBounds.FirstOrDefault(kvp => kvp.Value.Contains(skPoint)).Key;
    }

    public IReadOnlyList<Entity> GetVisibleEntities()
    {
        return _visibleEntities;
    }

    public void SetLayerVisibility(string layerName, bool visible)
    {
        if (visible)
        {
            _visibleLayers.Add(layerName);
        }
        else
        {
            _visibleLayers.Remove(layerName);
        }
    }

    public void SetLevelOfDetailEnabled(bool enabled)
    {
        _enableLevelOfDetail = enabled;
    }

    private void InitializeLayerVisibility(CadDocument document)
    {
        _visibleLayers.Clear();
        
        // Always make layer "0" visible as it's the default layer
        _visibleLayers.Add("0");
        
        foreach (var layer in document.Layers)
        {
            if (!layer.Flags.HasFlag(LayerFlags.Frozen) && layer.IsOn)
            {
                _visibleLayers.Add(layer.Name);
            }
        }
    }

    private bool IsEntityVisible(Entity entity)
    {
        // Check layer visibility
        var layerName = entity.Layer?.Name ?? "0";
        
        // Always show entities on default layer "0" or without a layer
        if (string.IsNullOrEmpty(layerName) || layerName == "0")
            return true;
            
        return _visibleLayers.Contains(layerName);
    }

    private BoundingBox? CalculateEntityBounds(Entity entity)
    {
        try
        {
            return entity switch
            {
                Line line => new BoundingBox(line.StartPoint, line.EndPoint),
                Arc arc => new BoundingBox(
                    arc.Center - new XYZ(arc.Radius, arc.Radius, 0),
                    arc.Center + new XYZ(arc.Radius, arc.Radius, 0)),
                Circle circle => new BoundingBox(
                    circle.Center - new XYZ(circle.Radius, circle.Radius, 0),
                    circle.Center + new XYZ(circle.Radius, circle.Radius, 0)),
                ACadSharp.Entities.Point point => new BoundingBox(point.Location, point.Location),
                _ => BoundingBox.Null
            };
        }
        catch
        {
            return null;
        }
    }

    private SKRect ToSkiaRect(BoundingBox box)
    {
        return new SKRect(
            (float)box.Min.X, (float)box.Min.Y,
            (float)box.Max.X, (float)box.Max.Y);
    }

    private bool ShouldSkipEntityForLOD(Entity entity, BoundingBox? bounds)
    {
        if (!bounds.HasValue) return false;

        // Calculate entity size in screen space
        var size = Math.Max(bounds.Value.Width, bounds.Value.Height);
        
        // Skip very small entities (less than 1 pixel when rendered)
        return size < 0.01; // Adjust this threshold as needed
    }

    private void RenderMText(MText mtext)
    {
        if (_canvas == null) return;

        // Render MText as regular text for now
        // In a full implementation, you'd parse RTF formatting
        var startPoint = new SKPoint((float)mtext.InsertPoint.X, (float)mtext.InsertPoint.Y);
        _canvas.DrawText(mtext.Value ?? "", startPoint, _textPaint);
    }

    private void RenderHatch(Hatch hatch)
    {
        if (_canvas == null) return;

        // Render hatch pattern (simplified implementation)
        // In a full implementation, you'd render the actual hatch pattern
        
        // For now, just draw the boundary
        foreach (var boundary in hatch.Paths)
        {
            foreach (var edge in boundary.Edges)
            {
                switch (edge)
                {
                    case Hatch.BoundaryPath.Line line:
                        var start = new SKPoint((float)line.Start.X, (float)line.Start.Y);
                        var end = new SKPoint((float)line.End.X, (float)line.End.Y);
                        _canvas.DrawLine(start, end, _strokePaint);
                        break;
                    case Hatch.BoundaryPath.Arc arc:
                        RenderHatchArc(arc);
                        break;
                }
            }
        }
    }

    private void RenderHatchArc(Hatch.BoundaryPath.Arc arc)
    {
        if (_canvas == null) return;

        var center = TransformPoint(arc.Center);
        var radius = (float)arc.Radius;
        var rect = new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius);
        
        var startAngle = (float)(arc.StartAngle * 180.0 / Math.PI);
        var endAngle = (float)(arc.EndAngle * 180.0 / Math.PI);
        var sweepAngle = endAngle - startAngle;
        
        if (sweepAngle < 0) sweepAngle += 360;
        
        using var path = new SKPath();
        path.AddArc(rect, startAngle, sweepAngle);
        _canvas.DrawPath(path, _strokePaint);
    }

    private void RenderDimension(Dimension dimension)
    {
        if (_canvas == null) return;

        // Render dimension (simplified implementation)
        // In a full implementation, you'd render dimension lines, text, arrows, etc.
        
        // For now, just render the dimension text if available
        if (!string.IsNullOrEmpty(dimension.Text))
        {
            var textPoint = new SKPoint((float)dimension.TextMiddlePoint.X, (float)dimension.TextMiddlePoint.Y);
            _canvas.DrawText(dimension.Text, textPoint, _textPaint);
        }
        
        // Draw basic dimension lines based on dimension type
        switch (dimension)
        {
            case DimensionLinear linear:
                RenderLinearDimension(linear);
                break;
            case DimensionAngular2Line angular2Line:
                RenderAngularDimension(angular2Line);
                break;
            case DimensionAngular3Pt angular3Pt:
                RenderAngularDimension(angular3Pt);
                break;
            case DimensionRadius radius:
                RenderRadiusDimension(radius);
                break;
        }
    }

    private void RenderLinearDimension(DimensionLinear dimension)
    {
        if (_canvas == null) return;

        // Draw dimension line
        var start = new SKPoint((float)dimension.FirstPoint.X, (float)dimension.FirstPoint.Y);
        var end = new SKPoint((float)dimension.SecondPoint.X, (float)dimension.SecondPoint.Y);
        _canvas.DrawLine(start, end, _strokePaint);
    }

    private void RenderAngularDimension(Dimension dimension)
    {
        if (_canvas == null) return;

        // Draw angular dimension arc (simplified)
        // Note: Generic Dimension doesn't have CenterPoint, using a simplified approach
        var center = TransformPoint(dimension.InsertionPoint);
        var radius = 50f; // Fixed radius for visual representation
        var rect = new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius);
        
        using var path = new SKPath();
        path.AddArc(rect, 0, 90); // Simplified arc
        _canvas.DrawPath(path, _strokePaint);
    }

    private void RenderRadiusDimension(DimensionRadius dimension)
    {
        if (_canvas == null) return;

        // Draw radius dimension line using available properties
        var center = TransformPoint(dimension.DefinitionPoint);
        var endPoint = TransformPoint(dimension.InsertionPoint);
        
        _canvas.DrawLine(center.X, center.Y, endPoint.X, endPoint.Y, _strokePaint);
    }

    private void RenderUnknownEntity(Entity entity)
    {
        if (_canvas == null) return;

        // Render unknown entity as a small rectangle at its insertion point
        var bounds = CalculateEntityBounds(entity);
        if (bounds.HasValue)
        {
            var rect = ToSkiaRect(bounds.Value);
            using var dashPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Gray,
                StrokeWidth = 1,
                PathEffect = SKPathEffect.CreateDash(new[] { 5f, 5f }, 0)
            };
            _canvas.DrawRect(rect, dashPaint);
        }
    }
}