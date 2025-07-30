using ACadSharp.Viewer.Interfaces;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACadSharp.Viewer.Services;

/// <summary>
/// Service for file dialog operations
/// </summary>
public class FileDialogService : IFileDialogService
{
    private readonly Window _mainWindow;

    public FileDialogService(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    /// <summary>
    /// Shows a file picker dialog for DWG files
    /// </summary>
    /// <returns>Selected file path or null if cancelled</returns>
    public async Task<string?> ShowDwgFilePickerAsync()
    {
        var options = new FilePickerOpenOptions
        {
            Title = "Select DWG File",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("DWG Files") { Patterns = new[] { "*.dwg" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
            }
        };

        var result = await _mainWindow.StorageProvider.OpenFilePickerAsync(options);
        return result.Count > 0 ? result[0].Path.LocalPath : null;
    }

    /// <summary>
    /// Shows a file picker dialog for DXF files
    /// </summary>
    /// <returns>Selected file path or null if cancelled</returns>
    public async Task<string?> ShowDxfFilePickerAsync()
    {
        var options = new FilePickerOpenOptions
        {
            Title = "Select DXF File",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("DXF Files") { Patterns = new[] { "*.dxf" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
            }
        };

        var result = await _mainWindow.StorageProvider.OpenFilePickerAsync(options);
        return result.Count > 0 ? result[0].Path.LocalPath : null;
    }

    /// <summary>
    /// Shows a folder picker dialog for batch search
    /// </summary>
    /// <returns>Selected folder path or null if cancelled</returns>
    public async Task<string?> ShowFolderPickerAsync()
    {
        var options = new FolderPickerOpenOptions
        {
            Title = "Select Folder for Batch Search",
            AllowMultiple = false
        };

        var result = await _mainWindow.StorageProvider.OpenFolderPickerAsync(options);
        return result.Count > 0 ? result[0].Path.LocalPath : null;
    }
}
