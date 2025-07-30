using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SkiaSharp;
using System;

namespace ACadSharp.Viewer.Controls;

/// <summary>
/// Custom Avalonia control for SkiaSharp drawing
/// </summary>
public class SkiaCanvas : Control
{
    public static readonly StyledProperty<Action<SKCanvas>?> PaintSurfaceProperty =
        AvaloniaProperty.Register<SkiaCanvas, Action<SKCanvas>?>(nameof(PaintSurface));

    public Action<SKCanvas>? PaintSurface
    {
        get => GetValue(PaintSurfaceProperty);
        set => SetValue(PaintSurfaceProperty, value);
    }

    static SkiaCanvas()
    {
        AffectsRender<SkiaCanvas>(PaintSurfaceProperty);
    }

    public override void Render(DrawingContext context)
    {
        if (PaintSurface == null) return;

        var bounds = new Rect(Bounds.Size);
        
        // Always use fallback method - create offscreen surface and draw image
        var width = Math.Max(1, (int)bounds.Width);
        var height = Math.Max(1, (int)bounds.Height);
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        
        using var surface = SKSurface.Create(info);
        if (surface?.Canvas != null)
        {
            surface.Canvas.Clear(SKColors.Transparent);
            surface.Canvas.Save();
            try
            {
                // Clip to control bounds
                surface.Canvas.ClipRect(new SKRect(0, 0, (float)bounds.Width, (float)bounds.Height));
                
                PaintSurface.Invoke(surface.Canvas);
            }
            finally
            {
                surface.Canvas.Restore();
            }
            
            // Create bitmap from surface and draw it
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = data.AsStream();
            
            var bitmap = new Avalonia.Media.Imaging.Bitmap(stream);
            context.DrawImage(bitmap, bounds);
        }
    }
}