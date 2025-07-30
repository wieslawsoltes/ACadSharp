using ACadSharp;
using ACadSharp.Viewer.Interfaces;
using ACadSharp.Viewer.Models;
using Avalonia.Controls;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Windows.Input;

namespace ACadSharp.Viewer.ViewModels;

public class SaveFileDialogViewModel : ViewModelBase
{
    private string _fileName = string.Empty;
    private string _folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    private string _selectedFileType = string.Empty;
    private VersionInfo _selectedVersion;
    private bool _isBinary = false;
    private readonly bool _isDwg;
    private readonly ACadVersion? _currentVersion;

    public SaveFileDialogViewModel(bool isDwg, string? defaultFileName = null, ACadVersion? currentVersion = null)
    {
        _isDwg = isDwg;
        _currentVersion = currentVersion;
        
        // Set up file types
        if (_isDwg)
        {
            FileTypes = new List<string> { "DWG Files (*.dwg)" };
            SelectedFileType = FileTypes.First();
            AvailableVersions = SupportedVersions.SupportedDwgVersions;
            Title = "Save as DWG File";
        }
        else
        {
            FileTypes = new List<string> { "DXF Files (*.dxf)" };
            SelectedFileType = FileTypes.First();
            AvailableVersions = SupportedVersions.SupportedDxfVersions;
            Title = "Save as DXF File";
        }

        // Set default version
        _selectedVersion = currentVersion.HasValue 
            ? SupportedVersions.GetDefaultVersion(currentVersion.Value, _isDwg)
            : AvailableVersions.Last();

        // Set default filename
        if (!string.IsNullOrEmpty(defaultFileName))
        {
            FileName = defaultFileName;
        }

        // Commands
        SaveCommand = ReactiveCommand.Create(ExecuteSave, this.WhenAnyValue(x => x.CanSave));
        CancelCommand = ReactiveCommand.Create(ExecuteCancel);

        // Property change notifications
        this.WhenAnyValue(x => x.SelectedVersion)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(SelectedVersionInfo)));
    }

    public new string Title { get; }

    public string FileName
    {
        get => _fileName;
        set
        {
            this.RaiseAndSetIfChanged(ref _fileName, value);
            this.RaisePropertyChanged(nameof(CanSave));
        }
    }

    public string FolderPath
    {
        get => _folderPath;
        set => this.RaiseAndSetIfChanged(ref _folderPath, value);
    }

    public List<string> FileTypes { get; }

    public string SelectedFileType
    {
        get => _selectedFileType;
        set => this.RaiseAndSetIfChanged(ref _selectedFileType, value);
    }

    public List<VersionInfo> AvailableVersions { get; }

    public VersionInfo SelectedVersion
    {
        get => _selectedVersion;
        set => this.RaiseAndSetIfChanged(ref _selectedVersion, value);
    }

    public bool ShowBinaryOption => !_isDwg;

    public bool IsBinary
    {
        get => _isBinary;
        set => this.RaiseAndSetIfChanged(ref _isBinary, value);
    }

    public bool CanSave => !string.IsNullOrWhiteSpace(FileName) && 
                          !string.IsNullOrWhiteSpace(FolderPath) && 
                          Directory.Exists(FolderPath);

    public string CurrentVersionInfo
    {
        get
        {
            if (!_currentVersion.HasValue)
                return "No document loaded";

            var versionInfo = AvailableVersions.FirstOrDefault(v => v.Version == _currentVersion.Value);
            return $"Current: {versionInfo?.DisplayName ?? _currentVersion.Value.ToString()}";
        }
    }

    public string SelectedVersionInfo => $"Target: {SelectedVersion.DisplayName}";

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public SaveDialogResult? Result { get; private set; }

    private void ExecuteSave()
    {
        var extension = _isDwg ? ".dwg" : ".dxf";
        var fileName = FileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase) 
            ? FileName 
            : FileName + extension;

        var fullPath = Path.Combine(FolderPath, fileName);
        
        Result = new SaveDialogResult(fullPath, SelectedVersion.Version, IsBinary);
        
        if (Parent is Window window)
        {
            window.Close(Result);
        }
    }

    private void ExecuteCancel()
    {
        Result = null;
        
        if (Parent is Window window)
        {
            window.Close(null);
        }
    }

    public object? Parent { get; set; }
}