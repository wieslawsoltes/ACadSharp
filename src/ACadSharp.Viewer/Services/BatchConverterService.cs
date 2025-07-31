using ACadSharp;
using ACadSharp.IO;
using ACadSharp.Viewer.Interfaces;
using ACadSharp.Viewer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ACadSharp.Viewer.Services;

/// <summary>
/// Service for batch file conversion operations
/// </summary>
public class BatchConverterService : IBatchConverterService
{
    public event EventHandler<BatchConversionProgress>? ProgressChanged;
    public event EventHandler<string>? FileConversionStarted;
    public event EventHandler<string>? FileConversionCompleted;
    public event EventHandler<(string FileName, string Error)>? FileConversionFailed;

    private readonly ICadFileService _cadFileService;

    public BatchConverterService(ICadFileService cadFileService)
    {
        _cadFileService = cadFileService ?? throw new ArgumentNullException(nameof(cadFileService));
    }

    /// <inheritdoc/>
    public (bool IsValid, string[] Errors) ValidateConfiguration(BatchConverterConfiguration configuration)
    {
        var errors = new List<string>();

        // Validate input folder
        if (string.IsNullOrWhiteSpace(configuration.InputFolder))
        {
            errors.Add("Input folder is required");
        }
        else if (!Directory.Exists(configuration.InputFolder))
        {
            errors.Add("Input folder does not exist");
        }

        // Validate output folder
        if (string.IsNullOrWhiteSpace(configuration.OutputFolder))
        {
            errors.Add("Output folder is required");
        }
        else
        {
            try
            {
                // Try to create the output directory if it doesn't exist
                Directory.CreateDirectory(configuration.OutputFolder);
            }
            catch (Exception ex)
            {
                errors.Add($"Cannot access or create output folder: {ex.Message}");
            }
        }

        // Check if input and output folders are the same
        if (!string.IsNullOrWhiteSpace(configuration.InputFolder) && 
            !string.IsNullOrWhiteSpace(configuration.OutputFolder))
        {
            var inputPath = Path.GetFullPath(configuration.InputFolder);
            var outputPath = Path.GetFullPath(configuration.OutputFolder);
            
            if (string.Equals(inputPath, outputPath, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Input and output folders cannot be the same");
            }
        }

        // Check if there are files to process
        if (errors.Count == 0)
        {
            var filesToProcess = GetFilesToProcess(configuration);
            if (filesToProcess.Length == 0)
            {
                errors.Add("No files found to process in the input folder");
            }
        }

        return (errors.Count == 0, errors.ToArray());
    }

    /// <inheritdoc/>
    public string[] GetFilesToProcess(BatchConverterConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.InputFolder) || !Directory.Exists(configuration.InputFolder))
        {
            return Array.Empty<string>();
        }

        var searchOption = configuration.RecurseFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var patterns = GetSearchPatterns(configuration.InputFileFilter);
        
        var files = new List<string>();
        
        foreach (var pattern in patterns)
        {
            try
            {
                files.AddRange(Directory.GetFiles(configuration.InputFolder, pattern, searchOption));
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we don't have access to
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                // Skip if directory was deleted during enumeration
                continue;
            }
        }

        return files.Distinct().OrderBy(f => f).ToArray();
    }

    /// <inheritdoc/>
    public TimeSpan EstimateConversionTime(BatchConverterConfiguration configuration)
    {
        var files = GetFilesToProcess(configuration);
        // Rough estimate: 2 seconds per file on average
        return TimeSpan.FromSeconds(files.Length * 2);
    }

    /// <inheritdoc/>
    public async Task<BatchConversionResult> ConvertAsync(BatchConverterConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new BatchConversionResult();
        
        try
        {
            // Validate configuration
            var validation = ValidateConfiguration(configuration);
            if (!validation.IsValid)
            {
                result.Success = false;
                result.Errors.AddRange(validation.Errors);
                return result;
            }

            // Get files to process
            var filesToProcess = GetFilesToProcess(configuration);
            result.TotalFiles = filesToProcess.Length;

            if (filesToProcess.Length == 0)
            {
                result.Success = true;
                return result;
            }

            // Create output directory if it doesn't exist
            Directory.CreateDirectory(configuration.OutputFolder);

            var progress = new BatchConversionProgress
            {
                TotalFiles = filesToProcess.Length,
                StatusMessage = "Starting batch conversion..."
            };
            
            OnProgressChanged(progress);

            // Process each file
            for (int i = 0; i < filesToProcess.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var inputFile = filesToProcess[i];
                var fileName = Path.GetFileName(inputFile);
                
                progress.CurrentFile = fileName;
                progress.FilesProcessed = i; // Files completed so far (0-based index)
                progress.OverallProgress = (int)((double)i / filesToProcess.Length * 100);
                progress.StatusMessage = $"Processing {fileName}... ({i + 1}/{filesToProcess.Length})";
                
                OnProgressChanged(progress);
                OnFileConversionStarted(fileName);

                try
                {
                    await ConvertSingleFileAsync(inputFile, configuration, cancellationToken);
                    
                    result.SuccessfulConversions++;
                    progress.SuccessfulConversions = result.SuccessfulConversions;
                    
                    // Update progress after successful conversion
                    progress.FilesProcessed = i + 1; // Update to actual completed count
                    OnProgressChanged(progress);
                    OnFileConversionCompleted(fileName);
                }
                catch (Exception ex)
                {
                    result.FailedConversions++;
                    progress.FailedConversions = result.FailedConversions;
                    
                    var errorMessage = $"{fileName}: {ex.Message}";
                    result.Errors.Add(errorMessage);
                    progress.Errors.Add(errorMessage);
                    
                    // Update progress after error, showing actual completed count
                    progress.FilesProcessed = i + 1; // Update to actual completed count
                    OnProgressChanged(progress);
                    OnFileConversionFailed(fileName, ex.Message);
                }
            }

            // Final progress update
            progress.FilesProcessed = filesToProcess.Length;
            progress.OverallProgress = 100;
            progress.StatusMessage = $"Conversion completed. {result.SuccessfulConversions} successful, {result.FailedConversions} failed";
            progress.CurrentFile = string.Empty;
            
            OnProgressChanged(progress);

            result.Success = result.FailedConversions == 0;
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.Errors.Add("Operation was cancelled");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Unexpected error: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }

    private async Task ConvertSingleFileAsync(string inputFile, BatchConverterConfiguration configuration, CancellationToken cancellationToken)
    {
        // Load the document
        var document = await _cadFileService.LoadFileAsync(inputFile);
        
        if (document == null)
        {
            throw new InvalidOperationException("Failed to load document");
        }

        // Generate output file path
        var outputFileName = Path.GetFileNameWithoutExtension(inputFile) + OutputFormatHelper.GetFileExtension(configuration.OutputFormat);
        var outputPath = Path.Combine(configuration.OutputFolder, outputFileName);

        // Ensure unique filename if file already exists
        var counter = 1;
        var baseOutputPath = outputPath;
        while (File.Exists(outputPath))
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(baseOutputPath);
            var extension = Path.GetExtension(baseOutputPath);
            outputPath = Path.Combine(configuration.OutputFolder, $"{nameWithoutExt}_{counter}{extension}");
            counter++;
        }

        // Get target version and format
        var targetVersion = OutputFormatHelper.GetACadVersion(configuration.OutputFormat);
        var isDwg = OutputFormatHelper.IsDwgFormat(configuration.OutputFormat);
        var isBinary = OutputFormatHelper.IsBinaryDxf(configuration.OutputFormat);

        // Debug logging to identify format issues
        System.Diagnostics.Debug.WriteLine($"Converting '{inputFile}' to format '{configuration.OutputFormat}'");
        System.Diagnostics.Debug.WriteLine($"Target version: {targetVersion}, IsDwg: {isDwg}, IsBinary: {isBinary}");
        System.Diagnostics.Debug.WriteLine($"Output path: {outputPath}");
        
        // Also log to console for easier debugging
        Console.WriteLine($"Batch Converter: {Path.GetFileName(inputFile)} -> {configuration.OutputFormat} ({(isDwg ? "DWG" : "DXF")})");

        // Convert and save
        if (isDwg)
        {
            System.Diagnostics.Debug.WriteLine("Saving as DWG format");
            await _cadFileService.SaveDwgAsync(document, outputPath, targetVersion);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Saving as DXF format (binary: {isBinary})");
            await _cadFileService.SaveDxfAsync(document, outputPath, isBinary, targetVersion);
        }
    }

    private static string[] GetSearchPatterns(InputFileFilter filter)
    {
        return filter switch
        {
            InputFileFilter.DwgAndDxf => new[] { "*.dwg", "*.dxf" },
            InputFileFilter.DwgOnly => new[] { "*.dwg" },
            InputFileFilter.DxfOnly => new[] { "*.dxf" },
            _ => new[] { "*.dwg", "*.dxf" }
        };
    }

    protected virtual void OnProgressChanged(BatchConversionProgress progress)
    {
        ProgressChanged?.Invoke(this, progress);
    }

    protected virtual void OnFileConversionStarted(string fileName)
    {
        FileConversionStarted?.Invoke(this, fileName);
    }

    protected virtual void OnFileConversionCompleted(string fileName)
    {
        FileConversionCompleted?.Invoke(this, fileName);
    }

    protected virtual void OnFileConversionFailed(string fileName, string error)
    {
        FileConversionFailed?.Invoke(this, (fileName, error));
    }
}