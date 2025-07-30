using ACadSharp;
using ACadSharp.Viewer.Interfaces;
using ACadSharp.Viewer.ViewModels;
using ACadSharp.Viewer.Views;
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

    /// <summary>
    /// Shows a save file dialog for DWG files
    /// </summary>
    /// <param name="defaultFileName">Default file name (optional)</param>
    /// <param name="currentVersion">Current document version for default selection</param>
    /// <returns>Save result with file path and selected version, or null if cancelled</returns>
    public async Task<SaveDialogResult?> ShowDwgSaveDialogAsync(string? defaultFileName = null, ACadVersion? currentVersion = null)
    {
        var viewModel = new SaveFileDialogViewModel(isDwg: true, defaultFileName, currentVersion);
        var dialog = new Views.SaveFileDialog
        {
            DataContext = viewModel
        };
        
        viewModel.Parent = dialog;

        var result = await dialog.ShowDialog<SaveDialogResult?>(_mainWindow);
        return result;
    }

    /// <summary>
    /// Shows a save file dialog for DXF files
    /// </summary>
    /// <param name="defaultFileName">Default file name (optional)</param>
    /// <param name="currentVersion">Current document version for default selection</param>
    /// <returns>Save result with file path and selected version, or null if cancelled</returns>
    public async Task<SaveDialogResult?> ShowDxfSaveDialogAsync(string? defaultFileName = null, ACadVersion? currentVersion = null)
    {
        var viewModel = new SaveFileDialogViewModel(isDwg: false, defaultFileName, currentVersion);
        var dialog = new Views.SaveFileDialog
        {
            DataContext = viewModel
        };
        
        viewModel.Parent = dialog;

        var result = await dialog.ShowDialog<SaveDialogResult?>(_mainWindow);
        return result;
    }
}
