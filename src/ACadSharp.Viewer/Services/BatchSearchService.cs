using ACadSharp;
using ACadSharp.IO;
using ACadSharp.Viewer.Interfaces;
using ACadSharp.Viewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ACadSharp.Viewer.Services
{
    /// <summary>
    /// Service for batch search operations across multiple CAD files
    /// </summary>
    public class BatchSearchService : IBatchSearchService
    {
        private readonly ICadFileService _cadFileService;
        private readonly ICadObjectTreeService _cadObjectTreeService;

        public event EventHandler<BatchSearchProgressEventArgs>? ProgressChanged;
        public event EventHandler<BatchSearchFileProcessedEventArgs>? FileProcessed;

        public BatchSearchService(ICadFileService? cadFileService = null, ICadObjectTreeService? cadObjectTreeService = null)
        {
            _cadFileService = cadFileService ?? new CadFileService();
            _cadObjectTreeService = cadObjectTreeService ?? new CadObjectTreeService();
        }

        /// <summary>
        /// Performs a batch search across multiple CAD files
        /// </summary>
        /// <param name="configuration">Batch search configuration</param>
        /// <param name="searchCriteria">Search criteria to apply</param>
        /// <returns>Collection of batch search results</returns>
        public async Task<IEnumerable<BatchSearchResult>> SearchFilesAsync(BatchSearchConfiguration configuration, SearchCriteria searchCriteria)
        {
            var results = new List<BatchSearchResult>();

            try
            {
                // Get all CAD files to process
                var files = await GetCadFilesAsync(configuration.RootFolder, configuration.IncludeSubdirectories, configuration.FileTypes);
                var fileList = files.ToList();

                // Apply file limit if specified
                if (configuration.MaxFiles > 0 && fileList.Count > configuration.MaxFiles)
                {
                    fileList = fileList.Take(configuration.MaxFiles).ToList();
                }

                OnProgressChanged(0, 0, fileList.Count, "Starting batch search...");

                // Process each file
                for (int i = 0; i < fileList.Count; i++)
                {
                    var filePath = fileList[i];
                    var result = new BatchSearchResult
                    {
                        FilePath = filePath,
                        FileName = Path.GetFileName(filePath),
                        FileType = Path.GetExtension(filePath).ToUpperInvariant()
                    };

                    try
                    {
                        OnProgressChanged(i + 1, i + 1, fileList.Count, $"Processing {result.FileName}...");

                        var startTime = DateTime.UtcNow;

                        // Load the CAD file
                        var document = await _cadFileService.LoadFileAsync(filePath);
                        result.IsLoaded = true;
                        result.StatusMessage = "File loaded successfully";

                        // Search for objects in the document
                        var searchResults = await _cadObjectTreeService.SearchObjectsAsync(document, searchCriteria);
                        var matches = searchResults.ToList();

                        // Create batch search matches
                        foreach (var match in matches)
                        {
                            var batchMatch = new BatchSearchMatch
                            {
                                ObjectType = match.GetType().Name,
                                Handle = match.Handle,
                                ObjectName = GetObjectName(match),
                                MatchType = searchCriteria.SearchType.ToString(),
                                MatchValue = GetMatchValue(match, searchCriteria),
                                CadObject = match
                            };
                            result.Matches.Add(batchMatch);
                        }

                        result.MatchCount = result.Matches.Count;
                        result.ProcessingTime = DateTime.UtcNow - startTime;
                        result.StatusMessage = $"Found {result.MatchCount} matches";

                        OnFileProcessed(result);
                    }
                    catch (Exception ex)
                    {
                        result.IsLoaded = false;
                        result.Error = ex;
                        result.StatusMessage = $"Error: {ex.Message}";

                        if (configuration.StopOnError)
                        {
                            throw;
                        }
                    }

                    results.Add(result);
                }

                OnProgressChanged(fileList.Count, fileList.Count, fileList.Count, "Batch search completed");
            }
            catch (Exception ex)
            {
                OnProgressChanged(0, 0, 0, $"Batch search failed: {ex.Message}");
                throw;
            }

            return results;
        }

        /// <summary>
        /// Gets all CAD files in a directory and its subdirectories
        /// </summary>
        /// <param name="rootFolder">Root folder to search</param>
        /// <param name="includeSubdirectories">Whether to include subdirectories</param>
        /// <param name="fileTypes">File types to include</param>
        /// <returns>Collection of file paths</returns>
        public async Task<IEnumerable<string>> GetCadFilesAsync(string rootFolder, bool includeSubdirectories, IEnumerable<string> fileTypes)
        {
            return await Task.Run(() =>
            {
                if (!Directory.Exists(rootFolder))
                {
                    return Enumerable.Empty<string>();
                }

                var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = new List<string>();

                foreach (var fileType in fileTypes)
                {
                    var pattern = $"*{fileType}";
                    var foundFiles = Directory.GetFiles(rootFolder, pattern, searchOption);
                    files.AddRange(foundFiles);
                }

                return files.Distinct().OrderBy(f => f);
            });
        }

        /// <summary>
        /// Validates a batch search configuration
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        /// <returns>Validation result</returns>
        public async Task<BatchSearchValidationResult> ValidateConfigurationAsync(BatchSearchConfiguration configuration)
        {
            var result = new BatchSearchValidationResult();

            try
            {
                // Validate root folder
                if (string.IsNullOrWhiteSpace(configuration.RootFolder))
                {
                    result.Errors.Add("Root folder is required");
                }
                else if (!Directory.Exists(configuration.RootFolder))
                {
                    result.Errors.Add($"Root folder does not exist: {configuration.RootFolder}");
                }

                // Validate file types
                if (configuration.FileTypes == null || !configuration.FileTypes.Any())
                {
                    result.Errors.Add("At least one file type must be specified");
                }
                else
                {
                    foreach (var fileType in configuration.FileTypes)
                    {
                        if (!fileType.StartsWith("."))
                        {
                            result.Warnings.Add($"File type should start with '.': {fileType}");
                        }
                    }
                }

                // Validate max files
                if (configuration.MaxFiles < 0)
                {
                    result.Errors.Add("Maximum files must be 0 (unlimited) or positive");
                }

                // Estimate file count if root folder is valid
                if (result.Errors.Count == 0 && Directory.Exists(configuration.RootFolder))
                {
                    var files = await GetCadFilesAsync(configuration.RootFolder, configuration.IncludeSubdirectories, configuration.FileTypes ?? new List<string>());
                    result.EstimatedFileCount = files.Count();

                    if (result.EstimatedFileCount == 0)
                    {
                        result.Warnings.Add("No CAD files found in the specified folder");
                    }
                    else if (result.EstimatedFileCount > 1000)
                    {
                        result.Warnings.Add($"Large number of files found ({result.EstimatedFileCount}). This may take a long time to process.");
                    }
                }

                result.IsValid = result.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Validation error: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }

        /// <summary>
        /// Gets a display name for a CAD object
        /// </summary>
        /// <param name="cadObject">The CAD object</param>
        /// <returns>Display name</returns>
        private string GetObjectName(CadObject cadObject)
        {
            try
            {
                // Try to get a meaningful name from common properties
                var type = cadObject.GetType();
                
                // Check for Name property
                var nameProperty = type.GetProperty("Name");
                if (nameProperty != null)
                {
                    var name = nameProperty.GetValue(cadObject)?.ToString();
                    if (!string.IsNullOrEmpty(name))
                        return name;
                }

                // Check for Layer property
                var layerProperty = type.GetProperty("Layer");
                if (layerProperty != null)
                {
                    var layer = layerProperty.GetValue(cadObject)?.ToString();
                    if (!string.IsNullOrEmpty(layer))
                        return $"{type.Name} on {layer}";
                }

                // Fallback to type name
                return type.Name;
            }
            catch
            {
                return cadObject.GetType().Name;
            }
        }

        /// <summary>
        /// Gets the value that matched the search criteria
        /// </summary>
        /// <param name="cadObject">The CAD object</param>
        /// <param name="searchCriteria">The search criteria</param>
        /// <returns>Match value</returns>
        private string GetMatchValue(CadObject cadObject, SearchCriteria searchCriteria)
        {
            try
            {
                switch (searchCriteria.SearchType)
                {
                    case SearchType.Handle:
                        return cadObject.Handle.ToString("X");
                    
                    case SearchType.ObjectType:
                        return cadObject.GetType().Name;
                    
                    case SearchType.ObjectData:
                        // Return a summary of object data
                        var type = cadObject.GetType();
                        var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        var dataValues = new List<string>();
                        
                        foreach (var prop in properties.Take(5)) // Limit to first 5 properties
                        {
                            try
                            {
                                var value = prop.GetValue(cadObject)?.ToString();
                                if (!string.IsNullOrEmpty(value) && value.Length < 50) // Limit length
                                {
                                    dataValues.Add($"{prop.Name}: {value}");
                                }
                            }
                            catch
                            {
                                // Skip properties that can't be read
                            }
                        }
                        
                        return string.Join(", ", dataValues);
                    
                    case SearchType.TagCode:
                        return cadObject.Handle.ToString("X");
                    
                    case SearchType.All:
                        return $"{cadObject.GetType().Name} (Handle: {cadObject.Handle:X})";
                    
                    default:
                        return cadObject.Handle.ToString("X");
                }
            }
            catch
            {
                return cadObject.Handle.ToString("X");
            }
        }

        /// <summary>
        /// Raises the ProgressChanged event
        /// </summary>
        /// <param name="currentFileIndex">Current file index</param>
        /// <param name="processedFiles">Number of processed files</param>
        /// <param name="totalFiles">Total number of files</param>
        /// <param name="statusMessage">Status message</param>
        protected virtual void OnProgressChanged(int currentFileIndex, int processedFiles, int totalFiles, string statusMessage)
        {
            var progressPercentage = totalFiles > 0 ? (processedFiles * 100) / totalFiles : 0;
            
            ProgressChanged?.Invoke(this, new BatchSearchProgressEventArgs
            {
                CurrentFileIndex = currentFileIndex,
                ProgressPercentage = progressPercentage,
                TotalFiles = totalFiles,
                StatusMessage = statusMessage
            });
        }

        /// <summary>
        /// Raises the FileProcessed event
        /// </summary>
        /// <param name="result">The batch search result</param>
        protected virtual void OnFileProcessed(BatchSearchResult result)
        {
            FileProcessed?.Invoke(this, new BatchSearchFileProcessedEventArgs
            {
                Result = result
            });
        }
    }
} 