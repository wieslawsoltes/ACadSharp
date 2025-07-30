using ACadSharp;
using ACadSharp.Viewer.Interfaces;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using CSMath;
using SkiaSharp;
using System;
using System.Drawing.Printing;
using System.IO;
using System.Threading.Tasks;

namespace ACadSharp.Viewer.Services;

/// <summary>
/// Service for printing CAD documents
/// </summary>
public class CadPrintService
{
    private readonly ICadRenderer _renderer;

    public CadPrintService(ICadRenderer renderer)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    /// <summary>
    /// Shows print preview dialog
    /// </summary>
    public async Task<bool> ShowPrintPreviewAsync(Window parentWindow, CadDocument document)
    {
        try
        {
            // Create print preview image
            var previewImage = await CreatePrintPreviewImageAsync(document, 800, 600);
            
            // Show preview dialog
            var previewWindow = new PrintPreviewWindow(previewImage, document);
            var result = await previewWindow.ShowDialog<bool>(parentWindow);
            
            return result;
        }
        catch (Exception ex)
        {
            // Log error or show message
            System.Diagnostics.Debug.WriteLine($"Print preview error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Prints the document directly
    /// </summary>
    public async Task<bool> PrintDocumentAsync(CadDocument document, PrintSettings? settings = null)
    {
        try
        {
            settings ??= new PrintSettings();
            
            // Create print image at high resolution
            var printImage = await CreatePrintImageAsync(document, settings);
            
            // For now, save as PDF (in a real implementation, you'd send to printer)
            await SaveAsPdfAsync(printImage, settings.OutputPath ?? "output.pdf");
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Print error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Exports document as PDF
    /// </summary>
    public async Task<bool> ExportToPdfAsync(CadDocument document, string filePath, PrintSettings? settings = null)
    {
        try
        {
            settings ??= new PrintSettings();
            
            var printImage = await CreatePrintImageAsync(document, settings);
            await SaveAsPdfAsync(printImage, filePath);
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PDF export error: {ex.Message}");
            return false;
        }
    }

    private async Task<SKBitmap> CreatePrintPreviewImageAsync(CadDocument document, int width, int height)
    {
        return await Task.Run(() =>
        {
            var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);
            
            canvas.Clear(SKColors.White);
            
            // Render document
            if (_renderer is SkiaSharpCadRenderer skiaRenderer)
            {
                skiaRenderer.SetCanvas(canvas);
                skiaRenderer.RenderDocument(document);
            }
            
            return bitmap;
        });
    }

    private async Task<SKBitmap> CreatePrintImageAsync(CadDocument document, PrintSettings settings)
    {
        return await Task.Run(() =>
        {
            // Calculate print dimensions at 300 DPI
            var widthPx = (int)(settings.PageWidth * 300 / 25.4); // Convert mm to pixels at 300 DPI
            var heightPx = (int)(settings.PageHeight * 300 / 25.4);
            
            var bitmap = new SKBitmap(widthPx, heightPx);
            using var canvas = new SKCanvas(bitmap);
            
            canvas.Clear(SKColors.White);
            
            // Set up print scaling and margins
            var marginPx = (int)(settings.Margin * 300 / 25.4);
            var printArea = new SKRect(marginPx, marginPx, widthPx - marginPx, heightPx - marginPx);
            
            canvas.ClipRect(printArea);
            canvas.Translate(marginPx, marginPx);
            
            // Scale to fit print area
            var docBounds = CalculateDocumentBounds(document);
            if (docBounds.HasValue)
            {
                var scaleX = (printArea.Width) / (float)docBounds.Value.Width;
                var scaleY = (printArea.Height) / (float)docBounds.Value.Height;
                var scale = Math.Min(scaleX, scaleY) * settings.Scale;
                
                canvas.Scale((float)scale);
                canvas.Translate((float)-docBounds.Value.Min.X, (float)-docBounds.Value.Min.Y);
            }
            
            // Render document
            if (_renderer is SkiaSharpCadRenderer skiaRenderer)
            {
                skiaRenderer.SetCanvas(canvas);
                skiaRenderer.RenderDocument(document);
            }
            
            return bitmap;
        });
    }

    private async Task SaveAsPdfAsync(SKBitmap bitmap, string filePath)
    {
        await Task.Run(() =>
        {
            using var document = SKDocument.CreatePdf(filePath);
            using var canvas = document.BeginPage(bitmap.Width, bitmap.Height);
            
            canvas.DrawBitmap(bitmap, 0, 0);
            document.EndPage();
            document.Close();
        });
    }

    private BoundingBox? CalculateDocumentBounds(CadDocument document)
    {
        BoundingBox? bounds = null;
        
        foreach (var entity in document.Entities)
        {
            var entityBounds = CalculateEntityBounds(entity);
            if (entityBounds.HasValue)
            {
                bounds = bounds?.Merge(entityBounds.Value) ?? entityBounds.Value;
            }
        }
        
        return bounds;
    }

    private BoundingBox? CalculateEntityBounds(ACadSharp.Entities.Entity entity)
    {
        // This would be similar to the renderer's CalculateEntityBounds method
        // Simplified implementation for now
        try
        {
            return entity switch
            {
                ACadSharp.Entities.Line line => new BoundingBox(line.StartPoint, line.EndPoint),
                ACadSharp.Entities.Circle circle => new BoundingBox(
                    circle.Center - new XYZ(circle.Radius, circle.Radius, 0),
                    circle.Center + new XYZ(circle.Radius, circle.Radius, 0)),
                ACadSharp.Entities.Point point => new BoundingBox(point.Location, point.Location),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Print settings for CAD documents
/// </summary>
public class PrintSettings
{
    public double PageWidth { get; set; } = 210; // A4 width in mm
    public double PageHeight { get; set; } = 297; // A4 height in mm
    public double Margin { get; set; } = 10; // Margin in mm
    public double Scale { get; set; } = 1.0; // Scale factor
    public string? OutputPath { get; set; }
    public bool FitToPage { get; set; } = true;
    public bool PrintInColor { get; set; } = true;
}

/// <summary>
/// Simple print preview window
/// </summary>
public class PrintPreviewWindow : Window
{
    public PrintPreviewWindow(SKBitmap previewImage, CadDocument document)
    {
        Title = "Print Preview";
        Width = 900;
        Height = 700;
        CanResize = true;
        
        var content = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto")
        };
        
        // Header
        var header = new TextBlock
        {
            Text = "Print Preview - CAD Document", // Header doesn't have FileName property
            FontSize = 16,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Margin = new Thickness(10)
        };
        Grid.SetRow(header, 0);
        content.Children.Add(header);
        
        // Preview image
        var scrollViewer = new ScrollViewer
        {
            Margin = new Thickness(10)
            // ZoomToFit property doesn't exist in Avalonia ScrollViewer
        };
        
        // Convert SKBitmap to Avalonia bitmap for display
        var avaloniaImage = new Image
        {
            Source = ConvertSkBitmapToAvaloniaBitmap(previewImage),
            Stretch = Avalonia.Media.Stretch.Uniform
        };
        
        scrollViewer.Content = avaloniaImage;
        Grid.SetRow(scrollViewer, 1);
        content.Children.Add(scrollViewer);
        
        // Buttons
        var buttonPanel = new StackPanel
        {
                            Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Thickness(10)
        };
        
        var printButton = new Button
        {
            Content = "Print",
            Margin = new Thickness(0, 0, 10, 0),
            Padding = new Thickness(20, 5)
        };
        printButton.Click += (s, e) => Close(true);
        
        var cancelButton = new Button
        {
            Content = "Cancel",
            Padding = new Thickness(20, 5)
        };
        cancelButton.Click += (s, e) => Close(false);
        
        buttonPanel.Children.Add(printButton);
        buttonPanel.Children.Add(cancelButton);
        Grid.SetRow(buttonPanel, 2);
        content.Children.Add(buttonPanel);
        
        Content = content;
    }
    
    private Avalonia.Media.Imaging.Bitmap ConvertSkBitmapToAvaloniaBitmap(SKBitmap skBitmap)
    {
        // Convert SKBitmap to byte array
        using var image = SKImage.FromBitmap(skBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        
        // Create Avalonia bitmap from byte array
        return new Avalonia.Media.Imaging.Bitmap(new MemoryStream(data.ToArray()));
    }
}