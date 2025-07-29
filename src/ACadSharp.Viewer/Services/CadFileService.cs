using ACadSharp;
using ACadSharp.IO;
using ACadSharp.Viewer.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ACadSharp.Viewer.Services
{
    /// <summary>
    /// Service for loading and managing CAD files
    /// </summary>
    public class CadFileService : ICadFileService
    {
        public event EventHandler<FileLoadProgressEventArgs>? LoadProgressChanged;

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
    }
} 