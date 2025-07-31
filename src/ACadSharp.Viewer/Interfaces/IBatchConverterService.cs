using ACadSharp.Viewer.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ACadSharp.Viewer.Interfaces;

/// <summary>
/// Service interface for batch file conversion operations
/// </summary>
public interface IBatchConverterService
{
    /// <summary>
    /// Event raised when conversion progress changes
    /// </summary>
    event EventHandler<BatchConversionProgress>? ProgressChanged;
    
    /// <summary>
    /// Event raised when a file conversion starts
    /// </summary>
    event EventHandler<string>? FileConversionStarted;
    
    /// <summary>
    /// Event raised when a file conversion completes
    /// </summary>
    event EventHandler<string>? FileConversionCompleted;
    
    /// <summary>
    /// Event raised when a file conversion fails
    /// </summary>
    event EventHandler<(string FileName, string Error)>? FileConversionFailed;
    
    /// <summary>
    /// Validates the conversion configuration
    /// </summary>
    /// <param name="configuration">Configuration to validate</param>
    /// <returns>Validation result with error messages if invalid</returns>
    (bool IsValid, string[] Errors) ValidateConfiguration(BatchConverterConfiguration configuration);
    
    /// <summary>
    /// Gets the list of files that would be processed with the given configuration
    /// </summary>
    /// <param name="configuration">Conversion configuration</param>
    /// <returns>Array of file paths that would be processed</returns>
    string[] GetFilesToProcess(BatchConverterConfiguration configuration);
    
    /// <summary>
    /// Starts the batch conversion process asynchronously
    /// </summary>
    /// <param name="configuration">Conversion configuration</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation</param>
    /// <returns>Task with conversion result</returns>
    Task<BatchConversionResult> ConvertAsync(BatchConverterConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Estimates the time required for conversion based on the number of files
    /// </summary>
    /// <param name="configuration">Conversion configuration</param>
    /// <returns>Estimated duration</returns>
    TimeSpan EstimateConversionTime(BatchConverterConfiguration configuration);
}