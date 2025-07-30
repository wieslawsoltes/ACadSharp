using ACadSharp;
using ACadSharp.IO;
using ACadSharp.Viewer.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ACadSharp.Viewer.Services;

/// <summary>
/// Service for loading and managing CAD files
/// </summary>
public class CadFileService : ICadFileService
{
    public event EventHandler<FileLoadProgressEventArgs>? LoadProgressChanged;
    public event EventHandler<FileLoadProgressEventArgs>? SaveProgressChanged;

    /// <summary>
    /// Loads a CAD file (DWG or DXF) asynchronously
    /// </summary>
    /// <param name="filePath">Path to the CAD file</param>
    /// <returns>Loaded CAD document</returns>
    public async Task<CadDocument> LoadFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        return await Task.Run(() =>
        {
            try
            {
                OnLoadProgressChanged(10, "Starting file load...");

                CadDocument document;

                if (IsDwgFile(filePath))
                {
                    OnLoadProgressChanged(20, "Loading DWG file...");
                    var configuration = new DwgReaderConfiguration
                    {
                        KeepUnknownEntities = true,
                        KeepUnknownNonGraphicalObjects = true
                    };
                    document = DwgReader.Read(filePath, configuration);
                    OnLoadProgressChanged(90, "DWG file loaded successfully");
                }
                else if (IsDxfFile(filePath))
                {
                    OnLoadProgressChanged(20, "Loading DXF file...");
                    var configuration = new DxfReaderConfiguration
                    {
                        KeepUnknownEntities = true,
                        KeepUnknownNonGraphicalObjects = true
                    };
                    document = DxfReader.Read(filePath, configuration);
                    OnLoadProgressChanged(90, "DXF file loaded successfully");
                }
                else
                {
                    throw new NotSupportedException($"Unsupported file format: {Path.GetExtension(filePath)}");
                }

                OnLoadProgressChanged(100, "File processing completed");
                return document;
            }
            catch (Exception ex)
            {
                OnLoadProgressChanged(0, $"Error loading file: {ex.Message}");
                throw;
            }
        });
    }

    /// <summary>
    /// Determines if the file is a DWG file
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>True if DWG, false otherwise</returns>
    public bool IsDwgFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension == ".dwg";
    }

    /// <summary>
    /// Determines if the file is a DXF file
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>True if DXF, false otherwise</returns>
    public bool IsDxfFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension == ".dxf";
    }

    /// <summary>
    /// Saves a CAD document as DWG file asynchronously
    /// </summary>
    /// <param name="document">The document to save</param>
    /// <param name="filePath">Path where to save the file</param>
    /// <param name="targetVersion">Target CAD version (null to use document's current version)</param>
    /// <returns>Task representing the async operation</returns>
    public async Task SaveDwgAsync(CadDocument document, string filePath, ACadVersion? targetVersion = null)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        await Task.Run(() =>
        {
            var originalVersion = document.Header.Version;
            try
            {
                // Set target version if specified
                if (targetVersion.HasValue && targetVersion.Value != originalVersion)
                {
                    OnSaveProgressChanged(5, $"Converting to {targetVersion.Value}...");
                    document.Header.Version = targetVersion.Value;
                }

                OnSaveProgressChanged(10, "Starting DWG file save...");
                
                DwgWriter.Write(filePath, document, notification: (sender, e) =>
                {
                    // Since NotificationEventArgs doesn't have Percentage, we'll use a simpler progress indication
                    OnSaveProgressChanged(50, e.Message ?? "Saving DWG file...");
                });
                
                OnSaveProgressChanged(100, "DWG file saved successfully");
            }
            catch (Exception ex)
            {
                OnSaveProgressChanged(0, $"Error saving DWG file: {ex.Message}");
                throw;
            }
            finally
            {
                // Always restore original version
                if (targetVersion.HasValue && targetVersion.Value != originalVersion)
                {
                    document.Header.Version = originalVersion;
                }
            }
        });
    }

    /// <summary>
    /// Saves a CAD document as DXF file asynchronously
    /// </summary>
    /// <param name="document">The document to save</param>
    /// <param name="filePath">Path where to save the file</param>
    /// <param name="binary">True for binary DXF, false for ASCII</param>
    /// <param name="targetVersion">Target CAD version (null to use document's current version)</param>
    /// <returns>Task representing the async operation</returns>
    public async Task SaveDxfAsync(CadDocument document, string filePath, bool binary = false, ACadVersion? targetVersion = null)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        await Task.Run(() =>
        {
            var originalVersion = document.Header.Version;
            try
            {
                // Set target version if specified
                if (targetVersion.HasValue && targetVersion.Value != originalVersion)
                {
                    OnSaveProgressChanged(5, $"Converting to {targetVersion.Value}...");
                    document.Header.Version = targetVersion.Value;
                }

                var formatType = binary ? "binary" : "ASCII";
                OnSaveProgressChanged(10, $"Starting {formatType} DXF file save...");
                
                DxfWriter.Write(filePath, document, binary, notification: (sender, e) =>
                {
                    // Since NotificationEventArgs doesn't have Percentage, we'll use a simpler progress indication
                    OnSaveProgressChanged(50, e.Message ?? $"Saving {formatType} DXF file...");
                });
                
                OnSaveProgressChanged(100, $"{formatType} DXF file saved successfully");
            }
            catch (Exception ex)
            {
                OnSaveProgressChanged(0, $"Error saving DXF file: {ex.Message}");
                throw;
            }
            finally
            {
                // Always restore original version
                if (targetVersion.HasValue && targetVersion.Value != originalVersion)
                {
                    document.Header.Version = originalVersion;
                }
            }
        });
    }

    /// <summary>
    /// Raises the LoadProgressChanged event
    /// </summary>
    /// <param name="progressPercentage">Progress percentage (0-100)</param>
    /// <param name="statusMessage">Status message</param>
    protected virtual void OnLoadProgressChanged(int progressPercentage, string statusMessage)
    {
        LoadProgressChanged?.Invoke(this, new FileLoadProgressEventArgs
        {
            ProgressPercentage = progressPercentage,
            StatusMessage = statusMessage
        });
    }

    /// <summary>
    /// Raises the SaveProgressChanged event
    /// </summary>
    /// <param name="progressPercentage">Progress percentage (0-100)</param>
    /// <param name="statusMessage">Status message</param>
    protected virtual void OnSaveProgressChanged(int progressPercentage, string statusMessage)
    {
        SaveProgressChanged?.Invoke(this, new FileLoadProgressEventArgs
        {
            ProgressPercentage = progressPercentage,
            StatusMessage = statusMessage
        });
    }
}
