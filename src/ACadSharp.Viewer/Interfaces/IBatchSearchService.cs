using ACadSharp.Viewer.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ACadSharp.Viewer.Interfaces
{
    /// <summary>
    /// Interface for batch search operations across multiple CAD files
    /// </summary>
    public interface IBatchSearchService
    {
        /// <summary>
        /// Event raised when batch search progress is updated
        /// </summary>
        event EventHandler<BatchSearchProgressEventArgs>? ProgressChanged;

        /// <summary>
        /// Event raised when a file is processed during batch search
        /// </summary>
        event EventHandler<BatchSearchFileProcessedEventArgs>? FileProcessed;

        /// <summary>
        /// Performs a batch search across multiple CAD files
        /// </summary>
        /// <param name="configuration">Batch search configuration</param>
        /// <param name="searchCriteria">Search criteria to apply</param>
        /// <returns>Collection of batch search results</returns>
        Task<IEnumerable<BatchSearchResult>> SearchFilesAsync(BatchSearchConfiguration configuration, SearchCriteria searchCriteria);

        /// <summary>
        /// Gets all CAD files in a directory and its subdirectories
        /// </summary>
        /// <param name="rootFolder">Root folder to search</param>
        /// <param name="includeSubdirectories">Whether to include subdirectories</param>
        /// <param name="fileTypes">File types to include</param>
        /// <returns>Collection of file paths</returns>
        Task<IEnumerable<string>> GetCadFilesAsync(string rootFolder, bool includeSubdirectories, IEnumerable<string> fileTypes);

        /// <summary>
        /// Validates a batch search configuration
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        /// <returns>Validation result</returns>
        Task<BatchSearchValidationResult> ValidateConfigurationAsync(BatchSearchConfiguration configuration);
    }

    /// <summary>
    /// Event arguments for batch search progress updates
    /// </summary>
    public class BatchSearchProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Current file being processed
        /// </summary>
        public string CurrentFile { get; set; } = string.Empty;

        /// <summary>
        /// Current file index (1-based)
        /// </summary>
        public int CurrentFileIndex { get; set; }

        /// <summary>
        /// Total number of files to process
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public int ProgressPercentage { get; set; }

        /// <summary>
        /// Status message
        /// </summary>
        public string StatusMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event arguments for when a file is processed during batch search
    /// </summary>
    public class BatchSearchFileProcessedEventArgs : EventArgs
    {
        /// <summary>
        /// Result of processing the file
        /// </summary>
        public BatchSearchResult Result { get; set; } = new();
    }

    /// <summary>
    /// Result of batch search configuration validation
    /// </summary>
    public class BatchSearchValidationResult
    {
        /// <summary>
        /// Whether the configuration is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation errors
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Validation warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Estimated number of files that will be processed
        /// </summary>
        public int EstimatedFileCount { get; set; }
    }
} 