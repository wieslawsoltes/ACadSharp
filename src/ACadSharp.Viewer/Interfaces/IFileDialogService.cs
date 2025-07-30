using System.Threading.Tasks;

namespace ACadSharp.Viewer.Interfaces;

/// <summary>
/// Interface for file dialog operations
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Shows a file picker dialog for DWG files
    /// </summary>
    /// <returns>Selected file path or null if cancelled</returns>
    Task<string?> ShowDwgFilePickerAsync();

    /// <summary>
    /// Shows a file picker dialog for DXF files
    /// </summary>
    /// <returns>Selected file path or null if cancelled</returns>
    Task<string?> ShowDxfFilePickerAsync();

    /// <summary>
    /// Shows a folder picker dialog for batch search
    /// </summary>
    /// <returns>Selected folder path or null if cancelled</returns>
    Task<string?> ShowFolderPickerAsync();

    /// <summary>
    /// Shows a save file dialog for DWG files
    /// </summary>
    /// <param name="defaultFileName">Default file name (optional)</param>
    /// <param name="currentVersion">Current document version for default selection</param>
    /// <returns>Save result with file path and selected version, or null if cancelled</returns>
    Task<SaveDialogResult?> ShowDwgSaveDialogAsync(string? defaultFileName = null, ACadVersion? currentVersion = null);

    /// <summary>
    /// Shows a save file dialog for DXF files
    /// </summary>
    /// <param name="defaultFileName">Default file name (optional)</param>
    /// <param name="currentVersion">Current document version for default selection</param>
    /// <returns>Save result with file path and selected version, or null if cancelled</returns>
    Task<SaveDialogResult?> ShowDxfSaveDialogAsync(string? defaultFileName = null, ACadVersion? currentVersion = null);
}

/// <summary>
/// Result from a save file dialog
/// </summary>
public class SaveDialogResult
{
    public string FilePath { get; set; } = string.Empty;
    public ACadVersion SelectedVersion { get; set; }
    public bool IsBinary { get; set; } // For DXF files only

    public SaveDialogResult(string filePath, ACadVersion selectedVersion, bool isBinary = false)
    {
        FilePath = filePath;
        SelectedVersion = selectedVersion;
        IsBinary = isBinary;
    }
}
