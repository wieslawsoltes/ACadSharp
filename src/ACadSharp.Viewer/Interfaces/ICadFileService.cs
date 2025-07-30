using ACadSharp;
using System;
using System.Threading.Tasks;

namespace ACadSharp.Viewer.Interfaces;

/// <summary>
/// Interface for CAD file operations following SOLID principles
/// </summary>
public interface ICadFileService
{
    /// <summary>
    /// Loads a CAD file (DWG or DXF) asynchronously
    /// </summary>
    /// <param name="filePath">Path to the CAD file</param>
    /// <returns>Loaded CAD document</returns>
    Task<CadDocument> LoadFileAsync(string filePath);

    /// <summary>
    /// Determines if the file is a DWG file
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>True if DWG, false otherwise</returns>
    bool IsDwgFile(string filePath);

    /// <summary>
    /// Determines if the file is a DXF file
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>True if DXF, false otherwise</returns>
    bool IsDxfFile(string filePath);

    /// <summary>
    /// Event raised when file loading progress changes
    /// </summary>
    event EventHandler<FileLoadProgressEventArgs> LoadProgressChanged;
}

/// <summary>
/// Event arguments for file loading progress
/// </summary>
public class FileLoadProgressEventArgs : EventArgs
{
    public int ProgressPercentage { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
}