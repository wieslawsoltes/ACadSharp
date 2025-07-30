using ACadSharp.Viewer.Interfaces;
using ACadSharp.Viewer.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ACadSharp.Viewer.Services;

namespace ACadSharp.Viewer.ViewModels;

/// <summary>
/// ViewModel for batch search results
/// </summary>
public class BatchSearchResultsViewModel : ViewModelBase
{
    private ObservableCollection<BatchSearchResult> _allResults = new();
    private ObservableCollection<BatchSearchResult> _filteredResults = new();
    private BatchSearchSummary _summary = new();
    private string _filterText = string.Empty;
    private bool _showOnlyFilesWithMatches = false;
    private bool _showOnlySuccessfulFiles = false;
    private BatchSearchResult? _selectedResult;
    private BatchSearchMatch? _selectedMatch;

    public BatchSearchResultsViewModel()
    {
        Title = "Batch Search Results";

        // Commands
        ClearFilterCommand = ReactiveCommand.Create(ClearFilter);
        ExportResultsCommand = ReactiveCommand.Create(ExportResults);
        OpenFileCommand = ReactiveCommand.Create<BatchSearchResult>(OpenFile);
        NavigateToMatchCommand = ReactiveCommand.Create<BatchSearchMatch>(NavigateToMatch);

        // Filter changes
        this.WhenAnyValue(x => x.FilterText, x => x.ShowOnlyFilesWithMatches, x => x.ShowOnlySuccessfulFiles)
            .Subscribe(_ => ApplyFilter());
    }

    /// <summary>
    /// All batch search results
    /// </summary>
    public ObservableCollection<BatchSearchResult> AllResults
    {
        get => _allResults;
        set => this.RaiseAndSetIfChanged(ref _allResults, value);
    }

    /// <summary>
    /// Filtered batch search results
    /// </summary>
    public ObservableCollection<BatchSearchResult> FilteredResults
    {
        get => _filteredResults;
        set => this.RaiseAndSetIfChanged(ref _filteredResults, value);
    }

    /// <summary>
    /// Summary of batch search results
    /// </summary>
    public BatchSearchSummary Summary
    {
        get => _summary;
        set => this.RaiseAndSetIfChanged(ref _summary, value);
    }

    /// <summary>
    /// Text to filter results by
    /// </summary>
    public string FilterText
    {
        get => _filterText;
        set => this.RaiseAndSetIfChanged(ref _filterText, value);
    }

    /// <summary>
    /// Whether to show only files with matches
    /// </summary>
    public bool ShowOnlyFilesWithMatches
    {
        get => _showOnlyFilesWithMatches;
        set => this.RaiseAndSetIfChanged(ref _showOnlyFilesWithMatches, value);
    }

    /// <summary>
    /// Whether to show only successfully processed files
    /// </summary>
    public bool ShowOnlySuccessfulFiles
    {
        get => _showOnlySuccessfulFiles;
        set => this.RaiseAndSetIfChanged(ref _showOnlySuccessfulFiles, value);
    }

    /// <summary>
    /// Currently selected result
    /// </summary>
    public BatchSearchResult? SelectedResult
    {
        get => _selectedResult;
        set => this.RaiseAndSetIfChanged(ref _selectedResult, value);
    }

    /// <summary>
    /// Currently selected match
    /// </summary>
    public BatchSearchMatch? SelectedMatch
    {
        get => _selectedMatch;
        set => this.RaiseAndSetIfChanged(ref _selectedMatch, value);
    }

    /// <summary>
    /// Command to clear the filter
    /// </summary>
    public ICommand ClearFilterCommand { get; }

    /// <summary>
    /// Command to export results
    /// </summary>
    public ICommand ExportResultsCommand { get; }

    /// <summary>
    /// Command to open a file
    /// </summary>
    public ICommand OpenFileCommand { get; }

    /// <summary>
    /// Command to navigate to a match
    /// </summary>
    public ICommand NavigateToMatchCommand { get; }

    /// <summary>
    /// Sets the batch search results
    /// </summary>
    /// <param name="results">The batch search results</param>
    /// <param name="summary">The batch search summary</param>
    public void SetResults(IEnumerable<BatchSearchResult> results, BatchSearchSummary summary)
    {
        AllResults.Clear();
        foreach (var result in results)
        {
            AllResults.Add(result);
        }

        Summary = summary;
        ApplyFilter();
    }

    /// <summary>
    /// Adds a single result to the collection
    /// </summary>
    /// <param name="result">The result to add</param>
    public void AddResult(BatchSearchResult result)
    {
        AllResults.Add(result);
        ApplyFilter();
    }

    /// <summary>
    /// Clears all results
    /// </summary>
    public void ClearResults()
    {
        AllResults.Clear();
        FilteredResults.Clear();
        Summary = new BatchSearchSummary();
        SelectedResult = null;
        SelectedMatch = null;
    }

    /// <summary>
    /// Applies the current filter to the results
    /// </summary>
    private void ApplyFilter()
    {
        var filtered = AllResults.AsEnumerable();

        // Filter by text
        if (!string.IsNullOrWhiteSpace(FilterText))
        {
            var filterLower = FilterText.ToLowerInvariant();
            filtered = filtered.Where(r => 
                r.FileName.ToLowerInvariant().Contains(filterLower) ||
                r.FilePath.ToLowerInvariant().Contains(filterLower) ||
                r.StatusMessage.ToLowerInvariant().Contains(filterLower) ||
                r.Matches.Any(m => 
                    m.ObjectType.ToLowerInvariant().Contains(filterLower) ||
                    m.ObjectName.ToLowerInvariant().Contains(filterLower) ||
                    m.MatchValue.ToLowerInvariant().Contains(filterLower)
                )
            );
        }

        // Filter by matches
        if (ShowOnlyFilesWithMatches)
        {
            filtered = filtered.Where(r => r.MatchCount > 0);
        }

        // Filter by success
        if (ShowOnlySuccessfulFiles)
        {
            filtered = filtered.Where(r => r.IsLoaded && r.Error == null);
        }

        // Update filtered collection
        FilteredResults.Clear();
        foreach (var result in filtered)
        {
            FilteredResults.Add(result);
        }
    }

    /// <summary>
    /// Clears the filter
    /// </summary>
    private void ClearFilter()
    {
        FilterText = string.Empty;
        ShowOnlyFilesWithMatches = false;
        ShowOnlySuccessfulFiles = false;
    }

    /// <summary>
    /// Exports the results to a file
    /// </summary>
    private void ExportResults()
    {
        // TODO: Implement export functionality
        // This could export to CSV, Excel, or other formats
    }

    /// <summary>
    /// Opens a file in the main viewer
    /// </summary>
    /// <param name="result">The result containing the file to open</param>
    private async void OpenFile(BatchSearchResult result)
    {
        if (result == null || string.IsNullOrEmpty(result.FilePath)) return;

        try
        {
            // Find the main window
            var mainWindow = GetMainWindow();
            if (mainWindow?.DataContext is MainWindowViewModel mainViewModel)
            {
                // Load the file into the left panel of the main window
                await mainViewModel.LoadLeftFileAsync(result.FilePath);

                // If there are matches, navigate to the first one
                if (result.Matches.Any())
                {
                    var firstMatch = result.Matches.First();
                    await NavigateToMatchInMainWindow(mainViewModel, firstMatch);
                }

                // Bring main window to front
                mainWindow.Activate();
                mainWindow.BringIntoView();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening file: {ex.Message}");
        }
    }

    /// <summary>
    /// Navigates to a specific match
    /// </summary>
    /// <param name="match">The match to navigate to</param>
    private async void NavigateToMatch(BatchSearchMatch match)
    {
        if (match == null || SelectedResult == null) return;

        try
        {
            // Find the main window
            var mainWindow = GetMainWindow();
            if (mainWindow?.DataContext is MainWindowViewModel mainViewModel)
            {
                // First load the file if it's not already loaded
                if (mainViewModel.LeftDocument?.FilePath != SelectedResult.FilePath)
                {
                    await mainViewModel.LoadLeftFileAsync(SelectedResult.FilePath);
                }

                // Navigate to the specific match
                await NavigateToMatchInMainWindow(mainViewModel, match);

                // Bring main window to front
                mainWindow.Activate();
                mainWindow.BringIntoView();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error navigating to match: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper method to get the main window
    /// </summary>
    /// <returns>The main window instance</returns>
    private Window? GetMainWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    /// <summary>
    /// Helper method to navigate to a specific match in the main window
    /// </summary>
    /// <param name="mainViewModel">The main window view model</param>
    /// <param name="match">The match to navigate to</param>
    private async System.Threading.Tasks.Task NavigateToMatchInMainWindow(MainWindowViewModel mainViewModel, BatchSearchMatch match)
    {
        try
        {
            // Set the search text and type to match the original search
            mainViewModel.SearchText = Summary.SearchText;
            mainViewModel.SearchType = Enum.Parse<SearchType>(Summary.SearchType);

            // Wait a bit for the search to complete
            await System.Threading.Tasks.Task.Delay(500);

            // Try to find and select the tree node for the matched object
            var treeNode = FindTreeNodeByHandle(mainViewModel.LeftDocument?.ObjectTreeNodes, match.Handle);
            if (treeNode != null && mainViewModel.LeftDocument != null)
            {
                mainViewModel.LeftDocument.SelectedTreeNode = treeNode;
                treeNode.IsExpanded = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error navigating to match in main window: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper method to find a tree node by CAD object handle
    /// </summary>
    /// <param name="nodes">The collection of tree nodes to search</param>
    /// <param name="handle">The handle to search for</param>
    /// <returns>The tree node with the matching handle, or null if not found</returns>
    private CadObjectTreeNode? FindTreeNodeByHandle(ObservableCollection<CadObjectTreeNode>? nodes, ulong handle)
    {
        if (nodes == null) return null;

        foreach (var node in nodes)
        {
            if (node.CadObject?.Handle == handle)
            {
                return node;
            }

            // Recursively search children
            var childResult = FindTreeNodeByHandle(node.Children, handle);
            if (childResult != null)
            {
                return childResult;
            }
        }

        return null;
    }
}