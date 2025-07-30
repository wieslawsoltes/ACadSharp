using ACadSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ACadSharp.Viewer.Models;

/// <summary>
/// Configuration for batch search operations
/// </summary>
public class BatchSearchConfiguration
{
    /// <summary>
    /// Root folder to search in
    /// </summary>
    public string RootFolder { get; set; } = string.Empty;

    /// <summary>
    /// Whether to search in subdirectories
    /// </summary>
    public bool IncludeSubdirectories { get; set; } = true;

    /// <summary>
    /// File types to search (DWG, DXF, or both)
    /// </summary>
    public List<string> FileTypes { get; set; } = new List<string> { ".dwg", ".dxf" };

    /// <summary>
    /// Maximum number of files to process (0 for unlimited)
    /// </summary>
    public int MaxFiles { get; set; } = 0;

    /// <summary>
    /// Whether to stop on first error
    /// </summary>
    public bool StopOnError { get; set; } = false;
}

/// <summary>
/// Result of a batch search operation
/// </summary>
public class BatchSearchResult : INotifyPropertyChanged
{
    private string _filePath = string.Empty;
    private string _fileName = string.Empty;
    private string _fileType = string.Empty;
    private bool _isLoaded;
    private string _statusMessage = string.Empty;
    private int _loadProgress;
    private int _matchCount;
    private TimeSpan _processingTime;
    private Exception? _error;
    private ObservableCollection<BatchSearchMatch> _matches = new();

    /// <summary>
    /// Full path to the file
    /// </summary>
    public string FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value);
    }

    /// <summary>
    /// Name of the file
    /// </summary>
    public string FileName
    {
        get => _fileName;
        set => SetProperty(ref _fileName, value);
    }

    /// <summary>
    /// Type of the file (DWG or DXF)
    /// </summary>
    public string FileType
    {
        get => _fileType;
        set => SetProperty(ref _fileType, value);
    }

    /// <summary>
    /// Indicates if the file was successfully loaded
    /// </summary>
    public bool IsLoaded
    {
        get => _isLoaded;
        set => SetProperty(ref _isLoaded, value);
    }

    /// <summary>
    /// Status message for the file processing
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// Progress percentage for file loading
    /// </summary>
    public int LoadProgress
    {
        get => _loadProgress;
        set => SetProperty(ref _loadProgress, value);
    }

    /// <summary>
    /// Number of matches found in this file
    /// </summary>
    public int MatchCount
    {
        get => _matchCount;
        set => SetProperty(ref _matchCount, value);
    }

    /// <summary>
    /// Time taken to process this file
    /// </summary>
    public TimeSpan ProcessingTime
    {
        get => _processingTime;
        set => SetProperty(ref _processingTime, value);
    }

    /// <summary>
    /// Error that occurred during processing (if any)
    /// </summary>
    public Exception? Error
    {
        get => _error;
        set => SetProperty(ref _error, value);
    }

    /// <summary>
    /// Collection of matches found in this file
    /// </summary>
    public ObservableCollection<BatchSearchMatch> Matches
    {
        get => _matches;
        set => SetProperty(ref _matches, value);
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

/// <summary>
/// Represents a single match found during batch search
/// </summary>
public class BatchSearchMatch : INotifyPropertyChanged
{
    private string _objectType = string.Empty;
    private ulong _handle;
    private string _objectName = string.Empty;
    private string _matchType = string.Empty;
    private string _matchValue = string.Empty;
    private CadObject? _cadObject;

    /// <summary>
    /// Type of the CAD object
    /// </summary>
    public string ObjectType
    {
        get => _objectType;
        set => SetProperty(ref _objectType, value);
    }

    /// <summary>
    /// Handle of the CAD object
    /// </summary>
    public ulong Handle
    {
        get => _handle;
        set => SetProperty(ref _handle, value);
    }

    /// <summary>
    /// Name of the CAD object
    /// </summary>
    public string ObjectName
    {
        get => _objectName;
        set => SetProperty(ref _objectName, value);
    }

    /// <summary>
    /// Type of match (Handle, ObjectType, ObjectData, etc.)
    /// </summary>
    public string MatchType
    {
        get => _matchType;
        set => SetProperty(ref _matchType, value);
    }

    /// <summary>
    /// Value that matched the search criteria
    /// </summary>
    public string MatchValue
    {
        get => _matchValue;
        set => SetProperty(ref _matchValue, value);
    }

    /// <summary>
    /// The actual CAD object
    /// </summary>
    public CadObject? CadObject
    {
        get => _cadObject;
        set => SetProperty(ref _cadObject, value);
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

/// <summary>
/// Summary of batch search results
/// </summary>
public class BatchSearchSummary : INotifyPropertyChanged
{
    private int _totalFiles;
    private int _processedFiles;
    private int _successfulFiles;
    private int _failedFiles;
    private int _totalMatches;
    private TimeSpan _totalProcessingTime;
    private string _searchText = string.Empty;
    private string _searchType = string.Empty;

    /// <summary>
    /// Total number of files found
    /// </summary>
    public int TotalFiles
    {
        get => _totalFiles;
        set => SetProperty(ref _totalFiles, value);
    }

    /// <summary>
    /// Number of files processed
    /// </summary>
    public int ProcessedFiles
    {
        get => _processedFiles;
        set => SetProperty(ref _processedFiles, value);
    }

    /// <summary>
    /// Number of files successfully processed
    /// </summary>
    public int SuccessfulFiles
    {
        get => _successfulFiles;
        set => SetProperty(ref _successfulFiles, value);
    }

    /// <summary>
    /// Number of files that failed to process
    /// </summary>
    public int FailedFiles
    {
        get => _failedFiles;
        set => SetProperty(ref _failedFiles, value);
    }

    /// <summary>
    /// Total number of matches found across all files
    /// </summary>
    public int TotalMatches
    {
        get => _totalMatches;
        set => SetProperty(ref _totalMatches, value);
    }

    /// <summary>
    /// Total time taken for the batch search
    /// </summary>
    public TimeSpan TotalProcessingTime
    {
        get => _totalProcessingTime;
        set => SetProperty(ref _totalProcessingTime, value);
    }

    /// <summary>
    /// Search text used
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    /// <summary>
    /// Search type used
    /// </summary>
    public string SearchType
    {
        get => _searchType;
        set => SetProperty(ref _searchType, value);
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