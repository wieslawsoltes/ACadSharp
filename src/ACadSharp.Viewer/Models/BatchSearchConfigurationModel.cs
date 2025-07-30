using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ACadSharp.Viewer.Interfaces;

namespace ACadSharp.Viewer.Models;

/// <summary>
/// Model for batch search configuration dialog
/// </summary>
public class BatchSearchConfigurationModel : INotifyPropertyChanged
{
    private string _rootFolder = string.Empty;
    private bool _includeSubdirectories = true;
    private bool _includeDwgFiles = true;
    private bool _includeDxfFiles = true;
    private int _maxFiles = 0;
    private bool _stopOnError = false;
    private string _searchText = string.Empty;
    private SearchType _searchType = SearchType.All;
    private bool _caseSensitive = false;

    /// <summary>
    /// Root folder to search in
    /// </summary>
    public string RootFolder
    {
        get => _rootFolder;
        set => SetProperty(ref _rootFolder, value);
    }

    /// <summary>
    /// Whether to search in subdirectories
    /// </summary>
    public bool IncludeSubdirectories
    {
        get => _includeSubdirectories;
        set => SetProperty(ref _includeSubdirectories, value);
    }

    /// <summary>
    /// Whether to include DWG files
    /// </summary>
    public bool IncludeDwgFiles
    {
        get => _includeDwgFiles;
        set => SetProperty(ref _includeDwgFiles, value);
    }

    /// <summary>
    /// Whether to include DXF files
    /// </summary>
    public bool IncludeDxfFiles
    {
        get => _includeDxfFiles;
        set => SetProperty(ref _includeDxfFiles, value);
    }

    /// <summary>
    /// Maximum number of files to process (0 for unlimited)
    /// </summary>
    public int MaxFiles
    {
        get => _maxFiles;
        set => SetProperty(ref _maxFiles, value);
    }

    /// <summary>
    /// Whether to stop on first error
    /// </summary>
    public bool StopOnError
    {
        get => _stopOnError;
        set => SetProperty(ref _stopOnError, value);
    }

    /// <summary>
    /// Search text to find
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    /// <summary>
    /// Type of search to perform
    /// </summary>
    public SearchType SearchType
    {
        get => _searchType;
        set => SetProperty(ref _searchType, value);
    }

    /// <summary>
    /// Whether search should be case sensitive
    /// </summary>
    public bool CaseSensitive
    {
        get => _caseSensitive;
        set => SetProperty(ref _caseSensitive, value);
    }

    /// <summary>
    /// Available search types for the dropdown
    /// </summary>
    public SearchType[] SearchTypeValues => System.Enum.GetValues<SearchType>();

    /// <summary>
    /// Gets the file types to search based on user selection
    /// </summary>
    public List<string> GetFileTypes()
    {
        var fileTypes = new List<string>();
            
        if (IncludeDwgFiles)
            fileTypes.Add(".dwg");
            
        if (IncludeDxfFiles)
            fileTypes.Add(".dxf");
            
        return fileTypes;
    }

    /// <summary>
    /// Converts this model to a BatchSearchConfiguration
    /// </summary>
    /// <returns>BatchSearchConfiguration object</returns>
    public BatchSearchConfiguration ToConfiguration()
    {
        return new BatchSearchConfiguration
        {
            RootFolder = RootFolder,
            IncludeSubdirectories = IncludeSubdirectories,
            FileTypes = GetFileTypes(),
            MaxFiles = MaxFiles,
            StopOnError = StopOnError
        };
    }

    /// <summary>
    /// Converts this model to a SearchCriteria
    /// </summary>
    /// <returns>SearchCriteria object</returns>
    public SearchCriteria ToSearchCriteria()
    {
        return new SearchCriteria
        {
            SearchText = SearchText,
            SearchType = SearchType,
            CaseSensitive = CaseSensitive
        };
    }

    /// <summary>
    /// Validates the configuration
    /// </summary>
    /// <returns>Validation result</returns>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(RootFolder))
        {
            errors.Add("Root folder is required");
        }

        if (!IncludeDwgFiles && !IncludeDxfFiles)
        {
            errors.Add("At least one file type (DWG or DXF) must be selected");
        }

        if (MaxFiles < 0)
        {
            errors.Add("Maximum files must be 0 (unlimited) or positive");
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            errors.Add("Search text is required");
        }

        return (errors.Count == 0, errors);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}