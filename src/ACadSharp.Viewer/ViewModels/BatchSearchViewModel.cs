using ACadSharp.Viewer.Interfaces;
using ACadSharp.Viewer.Models;
using ACadSharp.Viewer.Services;
using ACadSharp.Viewer.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;

namespace ACadSharp.Viewer.ViewModels;

/// <summary>
/// ViewModel for batch search functionality
/// </summary>
public class BatchSearchViewModel : ViewModelBase
{
    private readonly IBatchSearchService _batchSearchService;
    private readonly IFileDialogService _fileDialogService;
    private BatchSearchResultsViewModel? _batchSearchResults;
    private bool _isBatchSearching = false;
    private string _batchSearchStatus = string.Empty;
    private int _batchSearchProgress = 0;
    private string _searchText = string.Empty;
    private SearchType _searchType = SearchType.Handle;

    public BatchSearchViewModel(IFileDialogService? fileDialogService = null)
    {
        _batchSearchService = new BatchSearchService();
        _fileDialogService = fileDialogService ?? new FileDialogService(null!);

        Title = "Batch Search";

        // Commands
        StartBatchSearchCommand = ReactiveCommand.CreateFromTask(StartBatchSearchAsync);
        ShowBatchSearchResultsCommand = ReactiveCommand.Create(ShowBatchSearchResults);

        // Subscribe to progress events
        _batchSearchService.ProgressChanged += OnBatchSearchProgressChanged;
        _batchSearchService.FileProcessed += OnBatchSearchFileProcessed;
    }

    /// <summary>
    /// Search text for batch search
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    /// <summary>
    /// Search type for batch search
    /// </summary>
    public SearchType SearchType
    {
        get => _searchType;
        set => this.RaiseAndSetIfChanged(ref _searchType, value);
    }

    /// <summary>
    /// Available search types for the dropdown
    /// </summary>
    public SearchType[] SearchTypeValues => Enum.GetValues<SearchType>();

    /// <summary>
    /// Batch search results ViewModel
    /// </summary>
    public BatchSearchResultsViewModel? BatchSearchResults
    {
        get => _batchSearchResults;
        set => this.RaiseAndSetIfChanged(ref _batchSearchResults, value);
    }

    /// <summary>
    /// Indicates if a batch search is currently in progress
    /// </summary>
    public bool IsBatchSearching
    {
        get => _isBatchSearching;
        set => this.RaiseAndSetIfChanged(ref _isBatchSearching, value);
    }

    /// <summary>
    /// Status message for batch search operations
    /// </summary>
    public string BatchSearchStatus
    {
        get => _batchSearchStatus;
        set => this.RaiseAndSetIfChanged(ref _batchSearchStatus, value);
    }

    /// <summary>
    /// Progress percentage for batch search operations
    /// </summary>
    public int BatchSearchProgress
    {
        get => _batchSearchProgress;
        set => this.RaiseAndSetIfChanged(ref _batchSearchProgress, value);
    }

    /// <summary>
    /// Command to start batch search
    /// </summary>
    public ICommand StartBatchSearchCommand { get; }

    /// <summary>
    /// Command to show batch search results
    /// </summary>
    public ICommand ShowBatchSearchResultsCommand { get; }

    /// <summary>
    /// Starts a batch search operation
    /// </summary>
    public async Task StartBatchSearchAsync()
    {
        try
        {
            // Show folder picker
            var folderPath = await _fileDialogService.ShowFolderPickerAsync();
            if (string.IsNullOrEmpty(folderPath))
                return;

            // Create configuration model
            var configModel = new BatchSearchConfigurationModel
            {
                RootFolder = folderPath,
                IncludeSubdirectories = true,
                IncludeDwgFiles = true,
                IncludeDxfFiles = true,
                MaxFiles = 0,
                StopOnError = false,
                SearchText = SearchText,
                SearchType = SearchType,
                CaseSensitive = false
            };

            // Validate configuration
            var (isValid, errors) = configModel.Validate();
            if (!isValid)
            {
                // TODO: Show validation errors to user
                return;
            }

            // Initialize batch search results
            BatchSearchResults = new BatchSearchResultsViewModel();
            IsBatchSearching = true;
            BatchSearchStatus = "Starting batch search...";
            BatchSearchProgress = 0;

            // Convert to configuration objects
            var configuration = configModel.ToConfiguration();
            var searchCriteria = configModel.ToSearchCriteria();

            // Perform batch search
            var results = await _batchSearchService.SearchFilesAsync(configuration, searchCriteria);
            var resultsList = results.ToList();

            // Create summary
            var summary = new BatchSearchSummary
            {
                TotalFiles = resultsList.Count,
                ProcessedFiles = resultsList.Count,
                SuccessfulFiles = resultsList.Count(r => r.IsLoaded && r.Error == null),
                FailedFiles = resultsList.Count(r => !r.IsLoaded || r.Error != null),
                TotalMatches = resultsList.Sum(r => r.MatchCount),
                TotalProcessingTime = TimeSpan.FromMilliseconds(resultsList.Sum(r => r.ProcessingTime.TotalMilliseconds)),
                SearchText = searchCriteria.SearchText ?? string.Empty,
                SearchType = searchCriteria.SearchType.ToString()
            };

            // Set results
            BatchSearchResults.SetResults(resultsList, summary);

            BatchSearchStatus = $"Batch search completed. Found {summary.TotalMatches} matches in {summary.SuccessfulFiles} files.";
            BatchSearchProgress = 100;
        }
        catch (Exception ex)
        {
            BatchSearchStatus = $"Batch search failed: {ex.Message}";
            BatchSearchProgress = 0;
        }
        finally
        {
            IsBatchSearching = false;
        }
    }

    /// <summary>
    /// Shows the batch search results
    /// </summary>
    public void ShowBatchSearchResults()
    {
        if (BatchSearchResults != null)
        {
            var resultsWindow = new Views.BatchSearchResultsWindow
            {
                DataContext = BatchSearchResults
            };
            resultsWindow.Show();
        }
    }

    /// <summary>
    /// Handles batch search progress updates
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Progress event arguments</param>
    private void OnBatchSearchProgressChanged(object? sender, BatchSearchProgressEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            BatchSearchProgress = e.ProgressPercentage;
            BatchSearchStatus = e.StatusMessage;
        });
    }

    /// <summary>
    /// Handles batch search file processed events
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">File processed event arguments</param>
    private void OnBatchSearchFileProcessed(object? sender, BatchSearchFileProcessedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (BatchSearchResults != null)
            {
                BatchSearchResults.AddResult(e.Result);
            }
        });
    }
} 