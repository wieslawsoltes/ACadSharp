using ACadSharp;
using ACadSharp.Viewer.Interfaces;
using ACadSharp.Viewer.Models;
using ACadSharp.Viewer.Services;
using ACadSharp.Viewer.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using System.Reactive.Concurrency;

namespace ACadSharp.Viewer.ViewModels;

/// <summary>
/// Main window ViewModel for the CAD file viewer
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private readonly ICadFileService _cadFileService;
    private readonly ICadObjectTreeService _cadObjectTreeService;
    private readonly IFileDialogService _fileDialogService;
    private readonly SearchSuggestionService _searchSuggestionService;
    private CadDocumentModel _leftDocument;
    private CadDocumentModel _rightDocument;
    private string _searchText = string.Empty;
    private SearchType _searchType = SearchType.Handle;
    private bool _isSearching;
    private readonly object _highlightLock = new object();
    private bool _isHighlighting = false;
    private DateTime _lastRestoreTime = DateTime.MinValue;
    private readonly TimeSpan _restoreDebounceTime = TimeSpan.FromMilliseconds(100);
    private ObservableCollection<string> _searchSuggestions = new();
    
    // Expanded state tracking
    private Dictionary<string, bool> _leftDocumentExpandedState = new();
    private Dictionary<string, bool> _rightDocumentExpandedState = new();

    public MainWindowViewModel(IFileDialogService? fileDialogService = null)
    {
        _cadFileService = new CadFileService();
        _cadObjectTreeService = new CadObjectTreeService();
        _searchSuggestionService = new SearchSuggestionService();
        _fileDialogService = fileDialogService ?? new FileDialogService(null!); // Will be set properly
        _leftDocument = new CadDocumentModel();
        _rightDocument = new CadDocumentModel();

        // Set navigation history for documents
        _leftDocument.NavigationHistory = LeftNavigationHistory;
        _rightDocument.NavigationHistory = RightNavigationHistory;

        Title = "ACadSharp Viewer - DWG/DXF File Comparison";

        // Commands - using simple commands to avoid threading issues
        LoadLeftFileCommand = ReactiveCommand.CreateFromTask<string>(LoadLeftFileAsync);
        LoadRightFileCommand = ReactiveCommand.CreateFromTask<string>(LoadRightFileAsync);
        LoadLeftFileNoParamCommand = ReactiveCommand.CreateFromTask(LoadLeftFileNoParamAsync);
        LoadRightFileNoParamCommand = ReactiveCommand.CreateFromTask(LoadRightFileNoParamAsync);
        ClearSearchCommand = ReactiveCommand.Create(ClearSearch);
        NavigateToPropertyCommand = ReactiveCommand.Create<ObjectProperty>(NavigateToProperty);
        NavigateToBreadcrumbCommand = ReactiveCommand.Create<BreadcrumbItem>(NavigateToBreadcrumb);
        OpenBatchSearchCommand = ReactiveCommand.Create(OpenBatchSearch);
        GoBackCommand = ReactiveCommand.Create(GoBack);
        GoForwardCommand = ReactiveCommand.Create(GoForward);


        // Subscribe to progress events
        _cadFileService.LoadProgressChanged += OnLoadProgressChanged;

        // Search text and type changes
        this.WhenAnyValue(x => x.SearchText, x => x.SearchType)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async tuple => 
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(tuple.Item1))
                    {
                        // Clear search results immediately when text is empty
                        // Don't block on IsSearching for clearing
                        ClearSearchResults();
                        // Then restore tree view state
                        RestoreTreeViewState();
                    }
                    else
                    {
                        // Prevent multiple simultaneous search operations
                        if (IsSearching) return;
                        await SearchAsync();
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception but don't crash the application
                    System.Diagnostics.Debug.WriteLine($"Error in search subscription: {ex.Message}");
                }
            });

        // Ensure all property changes that affect UI run on main thread
        this.WhenAnyValue(x => x.IsBusy)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe();

        this.WhenAnyValue(x => x.IsSearching)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe();

        this.WhenAnyValue(x => x.LeftDocument)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe();

        this.WhenAnyValue(x => x.RightDocument)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe();

        // Highlight properties when search text changes and a node is selected
        this.WhenAnyValue(x => x.SearchText, x => x.LeftDocument.SelectedTreeNode, x => x.RightDocument.SelectedTreeNode)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => HighlightSelectedNodeProperties());

        // Update search suggestions when search type changes
        this.WhenAnyValue(x => x.SearchType)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => UpdateSearchSuggestions());

        // Update search suggestions when documents are loaded
        this.WhenAnyValue(x => x.LeftDocument.Document, x => x.RightDocument.Document)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async _ => await UpdateSearchSuggestionsAsync());
    }

    /// <summary>
    /// Left document (DWG)
    /// </summary>
    public CadDocumentModel LeftDocument
    {
        get => _leftDocument;
        set => this.RaiseAndSetIfChanged(ref _leftDocument, value ?? new CadDocumentModel());
    }

    /// <summary>
    /// Right document (DXF)
    /// </summary>
    public CadDocumentModel RightDocument
    {
        get => _rightDocument;
        set => this.RaiseAndSetIfChanged(ref _rightDocument, value ?? new CadDocumentModel());
    }

    private NavigationHistory _leftNavigationHistory = new();
    /// <summary>
    /// Navigation history for the left document
    /// </summary>
    public NavigationHistory LeftNavigationHistory
    {
        get => _leftNavigationHistory;
        set => this.RaiseAndSetIfChanged(ref _leftNavigationHistory, value);
    }

    private NavigationHistory _rightNavigationHistory = new();
    /// <summary>
    /// Navigation history for the right document
    /// </summary>
    public NavigationHistory RightNavigationHistory
    {
        get => _rightNavigationHistory;
        set => this.RaiseAndSetIfChanged(ref _rightNavigationHistory, value);
    }

    /// <summary>
    /// Search text for finding objects
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    /// <summary>
    /// Search type for finding objects
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
    /// Search suggestions for autocomplete
    /// </summary>
    public ObservableCollection<string> SearchSuggestions
    {
        get => _searchSuggestions;
        set => this.RaiseAndSetIfChanged(ref _searchSuggestions, value);
    }

    /// <summary>
    /// Indicates if a search is currently in progress
    /// </summary>
    public bool IsSearching
    {
        get => _isSearching;
        set => this.RaiseAndSetIfChanged(ref _isSearching, value);
    }

    /// <summary>
    /// Command to load the left file
    /// </summary>
    public ICommand LoadLeftFileCommand { get; }

    /// <summary>
    /// Command to load the right file
    /// </summary>
    public ICommand LoadRightFileCommand { get; }

    /// <summary>
    /// Command to load the left file (no parameters)
    /// </summary>
    public ICommand LoadLeftFileNoParamCommand { get; }

    /// <summary>
    /// Command to load the right file (no parameters)
    /// </summary>
    public ICommand LoadRightFileNoParamCommand { get; }



    /// <summary>
    /// Command to clear search results
    /// </summary>
    public ICommand ClearSearchCommand { get; }

    /// <summary>
    /// Command to navigate to an object property
    /// </summary>
    public ICommand NavigateToPropertyCommand { get; }

    /// <summary>
    /// Command to navigate to a breadcrumb item
    /// </summary>
    public ICommand NavigateToBreadcrumbCommand { get; }

    /// <summary>
    /// Command to open batch search window
    /// </summary>
    public ICommand OpenBatchSearchCommand { get; }

    /// <summary>
    /// Command to go back in navigation history
    /// </summary>
    public ICommand GoBackCommand { get; }

    /// <summary>
    /// Command to go forward in navigation history
    /// </summary>
    public ICommand GoForwardCommand { get; }



    /// <summary>
    /// Loads a file into the left panel (DWG)
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    public async Task LoadLeftFileAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return;

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsBusy = true;
                LeftDocument.StatusMessage = "Loading file...";
                    
                // Clear previous document state to prevent duplicates
                LeftDocument.Clear();
            });

            var document = await _cadFileService.LoadFileAsync(filePath);
                
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LeftDocument.Document = document;
                LeftDocument.FilePath = filePath;
                LeftDocument.FileName = Path.GetFileName(filePath);
                LeftDocument.FileType = Path.GetExtension(filePath).ToUpperInvariant();
                LeftDocument.IsLoaded = true;
                LeftDocument.StatusMessage = "File loaded successfully";
            });

            // Build object tree
            var treeNodes = await _cadObjectTreeService.BuildObjectTreeAsync(document);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Clear any existing nodes first
                LeftDocument.ObjectTreeNodes.Clear();
                    
                foreach (var node in treeNodes)
                {
                    LeftDocument.ObjectTreeNodes.Add(node);
                }
                // Initialize filtered collection with all nodes visible
                LeftDocument.UpdateFilteredTreeNodes();
                // Capture initial expanded state (typically all collapsed)
                CaptureExpandedState(LeftDocument, _leftDocumentExpandedState);
                // Clear search results when new file is loaded
                RestoreTreeViewState();
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LeftDocument.StatusMessage = $"Error loading file: {ex.Message}";
                LeftDocument.IsLoaded = false;
            });
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsBusy = false;
            });
        }
    }

    /// <summary>
    /// Loads a file into the right panel (DXF)
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    public async Task LoadRightFileAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return;

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsBusy = true;
                RightDocument.StatusMessage = "Loading file...";
                    
                // Clear previous document state to prevent duplicates
                RightDocument.Clear();
            });

            var document = await _cadFileService.LoadFileAsync(filePath);
                
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                RightDocument.Document = document;
                RightDocument.FilePath = filePath;
                RightDocument.FileName = Path.GetFileName(filePath);
                RightDocument.FileType = Path.GetExtension(filePath).ToUpperInvariant();
                RightDocument.IsLoaded = true;
                RightDocument.StatusMessage = "File loaded successfully";
            });

            // Build object tree
            var treeNodes = await _cadObjectTreeService.BuildObjectTreeAsync(document);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Clear any existing nodes first
                RightDocument.ObjectTreeNodes.Clear();
                    
                foreach (var node in treeNodes)
                {
                    RightDocument.ObjectTreeNodes.Add(node);
                }
                // Initialize filtered collection with all nodes visible
                RightDocument.UpdateFilteredTreeNodes();
                // Capture initial expanded state (typically all collapsed)
                CaptureExpandedState(RightDocument, _rightDocumentExpandedState);
                // Clear search results when new file is loaded
                RestoreTreeViewState();
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                RightDocument.StatusMessage = $"Error loading file: {ex.Message}";
                RightDocument.IsLoaded = false;
            });
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsBusy = false;
            });
        }
    }

    /// <summary>
    /// Searches for objects in both documents
    /// </summary>
    private async Task SearchAsync()
    {
        // Prevent multiple simultaneous search operations
        if (IsSearching) return;
            
        try
        {
            IsSearching = true;

            // Capture the current expanded state before searching
            CaptureExpandedState(LeftDocument, _leftDocumentExpandedState);
            CaptureExpandedState(RightDocument, _rightDocumentExpandedState);

            var searchText = SearchText.Trim();
            var searchCriteria = new SearchCriteria
            {
                SearchText = searchText,
                SearchType = SearchType,
                CaseSensitive = false
            };

            // Search in left document
            if (LeftDocument?.Document != null && LeftDocument?.ObjectTreeNodes != null)
            {
                var leftResults = await _cadObjectTreeService.SearchObjectsAsync(LeftDocument.Document, searchCriteria);
                await FilterAndHighlightTree(LeftDocument, leftResults, searchText);
            }

            // Search in right document
            if (RightDocument?.Document != null && RightDocument?.ObjectTreeNodes != null)
            {
                var rightResults = await _cadObjectTreeService.SearchObjectsAsync(RightDocument.Document, searchCriteria);
                await FilterAndHighlightTree(RightDocument, rightResults, searchText);
            }
        }
        catch (Exception ex)
        {
            // Handle search errors silently for now
            System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
        }
        finally
        {
            IsSearching = false;
        }
    }

    /// <summary>
    /// Loads a file into the left panel (DWG) - no parameters version
    /// </summary>
    public async Task LoadLeftFileNoParamAsync()
    {
        try
        {
            var filePath = await _fileDialogService.ShowDwgFilePickerAsync();
            if (!string.IsNullOrEmpty(filePath))
            {
                await LoadLeftFileAsync(filePath);
            }
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LeftDocument.StatusMessage = $"Error selecting file: {ex.Message}";
            });
        }
    }

    /// <summary>
    /// Loads a file into the right panel (DXF) - no parameters version
    /// </summary>
    public async Task LoadRightFileNoParamAsync()
    {
        try
        {
            var filePath = await _fileDialogService.ShowDxfFilePickerAsync();
            if (!string.IsNullOrEmpty(filePath))
            {
                await LoadRightFileAsync(filePath);
            }
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                RightDocument.StatusMessage = $"Error selecting file: {ex.Message}";
            });
        }
    }

    /// <summary>
    /// Clears the search results
    /// </summary>
    private void ClearSearch()
    {
        // Ensure this runs on the main thread to avoid threading issues
        Dispatcher.UIThread.Post(() =>
        {
            SearchText = string.Empty;
            ClearSearchResults();
            RestoreTreeViewState();
        });
    }

    /// <summary>
    /// Handles file loading progress updates
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Progress event arguments</param>
    private void OnLoadProgressChanged(object? sender, FileLoadProgressEventArgs e)
    {
        // Update progress on the appropriate document
        if (IsBusy)
        {
            // Determine which document is being loaded and update its progress
            // This is a simplified implementation
        }
    }

    /// <summary>
    /// Filters and highlights the tree based on search results
    /// </summary>
    /// <param name="documentModel">Document model containing the tree nodes</param>
    /// <param name="searchResults">Search results</param>
    /// <param name="searchText">Search text for property highlighting</param>
    private async Task FilterAndHighlightTree(CadDocumentModel documentModel, System.Collections.Generic.IEnumerable<CadObject> searchResults, string searchText)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                var resultHandles = searchResults.Select(r => r.Handle).ToHashSet();
                
                // Filter and highlight the nodes while preserving expanded state
                var expandedStateDict = documentModel == LeftDocument ? _leftDocumentExpandedState : _rightDocumentExpandedState;
                FilterAndHighlightNodes(documentModel.ObjectTreeNodes, resultHandles, searchText, expandedStateDict);
                
                // Update the filtered tree
                documentModel.UpdateFilteredTreeNodes();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in FilterAndHighlightTree: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Recursively filters and highlights nodes
    /// </summary>
    /// <param name="nodes">Nodes to process</param>
    /// <param name="resultHandles">Set of handles that match the search</param>
    /// <param name="searchText">Search text for property highlighting</param>
    /// <param name="expandedStateDict">Dictionary containing the original expanded state</param>
    /// <param name="currentPath">Current path to build node paths</param>
    /// <returns>True if any node in this branch has a match</returns>
    private bool FilterAndHighlightNodes(ObservableCollection<CadObjectTreeNode> nodes, HashSet<ulong> resultHandles, string searchText, Dictionary<string, bool> expandedStateDict, string currentPath = "")
    {
        if (nodes == null) return false;
            
        bool anyMatch = false;
            
        // Create a copy of the collection to avoid concurrent modification issues
        var nodesCopy = nodes.ToList();
        foreach (var node in nodesCopy)
        {
            bool hasMatchingChild = false;
            bool isDirectMatch = false;

            // Build the path for this node
            var nodePath = string.IsNullOrEmpty(currentPath) ? node.Name : $"{currentPath}/{node.Name}";

            // Check if this node directly matches
            if (node.CadObject != null && resultHandles.Contains(node.CadObject.Handle))
            {
                isDirectMatch = true;
                hasMatchingChild = true;
            }

            // Check if node name or type matches search text
            if (!isDirectMatch && !string.IsNullOrEmpty(searchText))
            {
                var searchLower = searchText.ToLowerInvariant();
                if (node.Name.ToLowerInvariant().Contains(searchLower) || 
                    node.ObjectType.ToLowerInvariant().Contains(searchLower))
                {
                    isDirectMatch = true;
                    hasMatchingChild = true;
                }
            }

            // Recursively check children first
            if (node.Children.Any())
            {
                var childHasMatch = FilterAndHighlightNodes(node.Children, resultHandles, searchText, expandedStateDict, nodePath);
                hasMatchingChild = hasMatchingChild || childHasMatch;
            }

            // Set visibility and highlighting
            // A node should be visible if:
            // 1. It directly matches the search criteria, OR
            // 2. It has children that match the search criteria (to show the path to matching nodes)
            node.IsVisible = hasMatchingChild;
            node.IsHighlighted = isDirectMatch;
                
            // Preserve the original expanded state instead of auto-expanding
            // Only expand if:
            // 1. The node was originally expanded, OR
            // 2. The node needs to be expanded to show direct matches in children
            if (expandedStateDict.TryGetValue(nodePath, out bool wasExpanded))
            {
                node.IsExpanded = wasExpanded;
            }
            else if (hasMatchingChild && !isDirectMatch)
            {
                // If we don't have stored state but have matching children,
                // expand to show the path to matches (but only if this node itself is not a direct match)
                node.IsExpanded = true;
            }

            // Highlight properties if this node is selected and directly matches
            if (isDirectMatch && node == LeftDocument?.SelectedTreeNode && LeftDocument?.FilteredSelectedObjectProperties != null)
            {
                lock (_highlightLock)
                {
                    HighlightProperties(LeftDocument.FilteredSelectedObjectProperties, searchText);
                }
            }
            else if (isDirectMatch && node == RightDocument?.SelectedTreeNode && RightDocument?.FilteredSelectedObjectProperties != null)
            {
                lock (_highlightLock)
                {
                    HighlightProperties(RightDocument.FilteredSelectedObjectProperties, searchText);
                }
            }

            anyMatch = anyMatch || hasMatchingChild;
        }

        return anyMatch;
    }

    /// <summary>
    /// Ensures all parent nodes of matching nodes are expanded
    /// </summary>
    /// <param name="nodes">Root nodes to process</param>
    /// <param name="resultHandles">Set of handles that match the search</param>
    /// <param name="searchText">Search text for name/type matching</param>
    private void ExpandParentNodesOfMatches(ObservableCollection<CadObjectTreeNode> nodes, HashSet<ulong> resultHandles, string searchText)
    {
        if (nodes == null) return;

        foreach (var node in nodes)
        {
            ExpandParentNodesOfMatchesRecursive(node, resultHandles, searchText, new List<CadObjectTreeNode>());
        }
    }

    /// <summary>
    /// Recursively expands parent nodes if they contain matching descendants
    /// </summary>
    /// <param name="node">Current node to check</param>
    /// <param name="resultHandles">Set of handles that match the search</param>
    /// <param name="searchText">Search text for name/type matching</param>
    /// <param name="parentPath">List of parent nodes from root to current node</param>
    /// <returns>True if this node or any of its descendants have a match</returns>
    private bool ExpandParentNodesOfMatchesRecursive(CadObjectTreeNode node, HashSet<ulong> resultHandles, string searchText, List<CadObjectTreeNode> parentPath)
    {
        if (node == null) return false;

        bool hasMatchingDescendant = false;
        bool isDirectMatch = false;

        // Check if this node directly matches
        if (node.CadObject != null && resultHandles.Contains(node.CadObject.Handle))
        {
            isDirectMatch = true;
            hasMatchingDescendant = true;
        }

        // Check if node name or type matches search text
        if (!isDirectMatch && !string.IsNullOrEmpty(searchText))
        {
            var searchLower = searchText.ToLowerInvariant();
            if (node.Name.ToLowerInvariant().Contains(searchLower) || 
                node.ObjectType.ToLowerInvariant().Contains(searchLower))
            {
                isDirectMatch = true;
                hasMatchingDescendant = true;
            }
        }

        // Check children recursively
        var childParentPath = new List<CadObjectTreeNode>(parentPath) { node };
        foreach (var child in node.Children)
        {
            if (ExpandParentNodesOfMatchesRecursive(child, resultHandles, searchText, childParentPath))
            {
                hasMatchingDescendant = true;
            }
        }

        // If this node has matching descendants, expand all ancestors in the path
        if (hasMatchingDescendant)
        {
            node.IsExpanded = true;
            
            // Expand all ancestors in the path
            foreach (var ancestor in parentPath)
            {
                ancestor.IsExpanded = true;
            }
        }

        return hasMatchingDescendant;
    }

    /// <summary>
    /// Applies expansion state to the filtered tree nodes to ensure matches are visible
    /// </summary>
    /// <param name="filteredNodes">Filtered tree nodes</param>
    /// <param name="resultHandles">Set of handles that match the search</param>
    /// <param name="searchText">Search text for name/type matching</param>
    private void ApplyExpansionStateToFilteredTree(ObservableCollection<CadObjectTreeNode> filteredNodes, HashSet<ulong> resultHandles, string searchText)
    {
        if (filteredNodes == null) return;

        foreach (var node in filteredNodes)
        {
            ApplyExpansionStateToFilteredNodeRecursive(node, resultHandles, searchText);
        }
    }

    /// <summary>
    /// Recursively applies expansion state to filtered nodes that contain matches
    /// </summary>
    /// <param name="node">Current filtered node</param>
    /// <param name="resultHandles">Set of handles that match the search</param>
    /// <param name="searchText">Search text for name/type matching</param>
    /// <returns>True if this node or its descendants have matches</returns>
    private bool ApplyExpansionStateToFilteredNodeRecursive(CadObjectTreeNode node, HashSet<ulong> resultHandles, string searchText)
    {
        if (node == null) return false;

        bool hasMatchingDescendant = false;
        bool isDirectMatch = false;

        // Check if this node directly matches
        if (node.CadObject != null && resultHandles.Contains(node.CadObject.Handle))
        {
            isDirectMatch = true;
            hasMatchingDescendant = true;
        }

        // Check if node name or type matches search text
        if (!isDirectMatch && !string.IsNullOrEmpty(searchText))
        {
            var searchLower = searchText.ToLowerInvariant();
            if (node.Name.ToLowerInvariant().Contains(searchLower) || 
                node.ObjectType.ToLowerInvariant().Contains(searchLower))
            {
                isDirectMatch = true;
                hasMatchingDescendant = true;
            }
        }

        // Check children recursively
        foreach (var child in node.Children)
        {
            if (ApplyExpansionStateToFilteredNodeRecursive(child, resultHandles, searchText))
            {
                hasMatchingDescendant = true;
            }
        }

        // If this node has matching descendants, expand it
        if (hasMatchingDescendant)
        {
            node.IsExpanded = true;
        }

        return hasMatchingDescendant;
    }

    /// <summary>
    /// Highlights properties that match the search text
    /// </summary>
    /// <param name="properties">Properties to highlight</param>
    /// <param name="searchText">Search text</param>
    private void HighlightProperties(ObservableCollection<ObjectProperty> properties, string searchText)
    {
        if (properties == null) return;
            
        try
        {
            if (string.IsNullOrEmpty(searchText))
            {
                // Create a copy of the collection to avoid concurrent modification issues
                var propertiesToClear = properties.ToList();
                foreach (var prop in propertiesToClear)
                {
                    if (prop != null)
                    {
                        prop.IsHighlighted = false;
                    }
                }
                return;
            }

            var searchLower = searchText.ToLowerInvariant();
            // Create a copy of the collection to avoid concurrent modification issues
            var propertiesToHighlight = properties.ToList();
            foreach (var prop in propertiesToHighlight)
            {
                if (prop != null)
                {
                    bool shouldHighlight = false;
                    
                    // Determine what to highlight based on search type
                    switch (SearchType)
                    {
                        case SearchType.PropertyName:
                            shouldHighlight = prop.Name.ToLowerInvariant().Contains(searchLower);
                            break;
                        case SearchType.PropertyType:
                            shouldHighlight = prop.Type.ToLowerInvariant().Contains(searchLower);
                            break;
                        case SearchType.ObjectData:
                            shouldHighlight = prop.Value.ToLowerInvariant().Contains(searchLower);
                            break;
                        default:
                            // For other search types, highlight name, value, and type
                            shouldHighlight = prop.Name.ToLowerInvariant().Contains(searchLower) ||
                                             prop.Value.ToLowerInvariant().Contains(searchLower) ||
                                             prop.Type.ToLowerInvariant().Contains(searchLower);
                            break;
                    }
                    
                    prop.IsHighlighted = shouldHighlight;
                }
            }
        }
        catch (Exception ex)
        {
            // Log the exception but don't crash the application
            System.Diagnostics.Debug.WriteLine($"Error in HighlightProperties: {ex.Message}");
        }
    }

    /// <summary>
    /// Restores the tree view to its normal state after search operations
    /// </summary>
    private void RestoreTreeViewState()
    {
        // Debounce rapid successive calls to prevent duplicate root documents
        var now = DateTime.UtcNow;
        if ((now - _lastRestoreTime) < _restoreDebounceTime)
        {
            System.Diagnostics.Debug.WriteLine($"RestoreTreeViewState: Skipped due to debouncing (last call: {_lastRestoreTime:HH:mm:ss.fff})");
            return;
        }
        _lastRestoreTime = now;

        System.Diagnostics.Debug.WriteLine($"RestoreTreeViewState: Starting restoration at {now:HH:mm:ss.fff}");

        // Clear selected nodes first to prevent issues with filtered nodes
        if (LeftDocument?.SelectedTreeNode != null)
        {
            LeftDocument.SelectedTreeNode = null;
        }
            
        if (RightDocument?.SelectedTreeNode != null)
        {
            RightDocument.SelectedTreeNode = null;
        }

        // Ensure all nodes are visible and restore their original expanded state
        if (LeftDocument?.ObjectTreeNodes != null)
        {
            System.Diagnostics.Debug.WriteLine($"RestoreTreeViewState: Left document has {LeftDocument.ObjectTreeNodes.Count} root nodes");
            foreach (var node in LeftDocument.ObjectTreeNodes)
            {
                RestoreNodeState(node);
            }
            // Restore the original expanded state
            RestoreExpandedState(LeftDocument, _leftDocumentExpandedState);
            LeftDocument.UpdateFilteredTreeNodes();
            System.Diagnostics.Debug.WriteLine($"RestoreTreeViewState: Left document filtered collection has {LeftDocument.FilteredObjectTreeNodes.Count} root nodes");
        }
            
        if (RightDocument?.ObjectTreeNodes != null)
        {
            System.Diagnostics.Debug.WriteLine($"RestoreTreeViewState: Right document has {RightDocument.ObjectTreeNodes.Count} root nodes");
            foreach (var node in RightDocument.ObjectTreeNodes)
            {
                RestoreNodeState(node);
            }
            // Restore the original expanded state
            RestoreExpandedState(RightDocument, _rightDocumentExpandedState);
            RightDocument.UpdateFilteredTreeNodes();
            System.Diagnostics.Debug.WriteLine($"RestoreTreeViewState: Right document filtered collection has {RightDocument.FilteredObjectTreeNodes.Count} root nodes");
        }

        System.Diagnostics.Debug.WriteLine($"RestoreTreeViewState: Completed at {DateTime.UtcNow:HH:mm:ss.fff}");
    }

    /// <summary>
    /// Recursively restores a node and its children to normal state
    /// </summary>
    /// <param name="node">The node to restore</param>
    private void RestoreNodeState(CadObjectTreeNode node)
    {
        if (node == null) return;
            
        node.IsVisible = true;
        node.IsHighlighted = false;
            
        foreach (var child in node.Children)
        {
            RestoreNodeState(child);
        }
    }

    /// <summary>
    /// Clears all search results and highlighting
    /// </summary>
    private void ClearSearchResults()
    {
        lock (_highlightLock)
        {
            // Clear tree highlighting and visibility
            if (LeftDocument?.ObjectTreeNodes != null)
                ClearTreeHighlighting(LeftDocument.ObjectTreeNodes);
            if (RightDocument?.ObjectTreeNodes != null)
                ClearTreeHighlighting(RightDocument.ObjectTreeNodes);

            // Clear property highlighting
            if (LeftDocument?.SelectedObjectProperties != null)
                ClearPropertyHighlighting(LeftDocument.SelectedObjectProperties);
            if (RightDocument?.SelectedObjectProperties != null)
                ClearPropertyHighlighting(RightDocument.SelectedObjectProperties);
        }
    }

    /// <summary>
    /// Clears highlighting from tree nodes
    /// </summary>
    /// <param name="nodes">Nodes to clear</param>
    private void ClearTreeHighlighting(ObservableCollection<CadObjectTreeNode> nodes)
    {
        if (nodes == null) return;
            
        try
        {
            // Create a copy of the collection to avoid concurrent modification issues
            var nodesCopy = nodes.ToList();
            foreach (var node in nodesCopy)
            {
                if (node != null)
                {
                    node.IsHighlighted = false;
                    node.IsVisible = true; // Ensure all nodes are visible when clearing search
                        
                    // Don't reset expansion state here - let users maintain their preferred view
                    // node.IsExpanded = false;

                    if (node.Children?.Any() == true)
                    {
                        ClearTreeHighlighting(node.Children);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log the exception but don't crash the application
            System.Diagnostics.Debug.WriteLine($"Error in ClearTreeHighlighting: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears highlighting from properties
    /// </summary>
    /// <param name="properties">Properties to clear</param>
    private void ClearPropertyHighlighting(ObservableCollection<ObjectProperty> properties)
    {
        if (properties == null) return;
            
        try
        {
            // Create a copy of the collection to avoid concurrent modification issues
            var propertiesCopy = properties.ToList();
            foreach (var prop in propertiesCopy)
            {
                if (prop != null)
                {
                    prop.IsHighlighted = false;
                }
            }
        }
        catch (Exception ex)
        {
            // Log the exception but don't crash the application
            System.Diagnostics.Debug.WriteLine($"Error in ClearPropertyHighlighting: {ex.Message}");
        }
    }

    /// <summary>
    /// Highlights properties of the currently selected node based on search text
    /// </summary>
    private void HighlightSelectedNodeProperties()
    {
        if (_isHighlighting) return; // Prevent re-entrant calls
            
        lock (_highlightLock)
        {
            _isHighlighting = true;
            try
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    if (LeftDocument?.SelectedObjectProperties != null)
                        ClearPropertyHighlighting(LeftDocument.SelectedObjectProperties);
                    if (RightDocument?.SelectedObjectProperties != null)
                        ClearPropertyHighlighting(RightDocument.SelectedObjectProperties);
                    
                    // Update filtered properties to show all when no search
                    LeftDocument?.UpdateFilteredProperties(SearchType, null);
                    RightDocument?.UpdateFilteredProperties(SearchType, null);
                    return;
                }

                var searchText = SearchText.Trim();
                    
                // Highlight properties for left document
                if (LeftDocument?.SelectedTreeNode != null && LeftDocument?.FilteredSelectedObjectProperties != null)
                {
                    HighlightProperties(LeftDocument.FilteredSelectedObjectProperties, searchText);
                }

                // Highlight properties for right document
                if (RightDocument?.SelectedTreeNode != null && RightDocument?.FilteredSelectedObjectProperties != null)
                {
                    HighlightProperties(RightDocument.FilteredSelectedObjectProperties, searchText);
                }
                
                // Update filtered properties based on current search
                LeftDocument?.UpdateFilteredProperties(SearchType, searchText);
                RightDocument?.UpdateFilteredProperties(SearchType, searchText);
            }
            finally
            {
                _isHighlighting = false;
            }
        }
    }

    /// <summary>
    /// Navigates to a property object when clicked
    /// </summary>
    /// <param name="property">The property to navigate to</param>
    public void NavigateToProperty(ObjectProperty property)
    {
        if (property?.PropertyObject == null || !property.IsNavigable)
            return;

        // Determine the property path for breadcrumb navigation
        var propertyPath = DeterminePropertyPath(property);

        // If the property has a handle, try to navigate to the object in the tree first
        if (property.ObjectHandle.HasValue)
        {
            if (LeftDocument?.Document != null)
            {
                NavigateToPropertyInDocument(LeftDocument, property, propertyPath);
            }

            if (RightDocument?.Document != null)
            {
                NavigateToPropertyInDocument(RightDocument, property, propertyPath);
            }
        }
        else
        {
            // Otherwise, navigate directly to the object
            if (LeftDocument?.Document != null)
            {
                NavigateToPropertyInDocument(LeftDocument, property, propertyPath);
            }

            if (RightDocument?.Document != null)
            {
                NavigateToPropertyInDocument(RightDocument, property, propertyPath);
            }
        }
    }

    /// <summary>
    /// Navigates to a property in a specific document with enhanced tree synchronization
    /// </summary>
    /// <param name="document">The document to navigate in</param>
    /// <param name="property">The property to navigate to</param>
    /// <param name="propertyPath">The property path for breadcrumb tracking</param>
    private void NavigateToPropertyInDocument(CadDocumentModel document, ObjectProperty property, string propertyPath)
    {
        // Try to find and select the object in the tree first for better synchronization
        if (property.PropertyObject is ACadSharp.CadObject cadObject)
        {
            var targetNode = FindObjectInTreeAndSelect(document, cadObject);
            if (targetNode != null)
            {
                // Use the tree node navigation to maintain proper tree view state
                document.NavigateToTreeNodeWithoutHistory(targetNode);
                return;
            }
        }

        // Fallback to the original navigation methods
        if (property.ObjectHandle.HasValue)
        {
            document.NavigateToObjectByHandle(property.ObjectHandle.Value, propertyPath);
        }
        else
        {
            document.NavigateToObject(property.PropertyObject, propertyPath);
        }
    }

    /// <summary>
    /// Determines the property path for breadcrumb navigation
    /// </summary>
    /// <param name="property">The property to determine path for</param>
    /// <returns>The property path string</returns>
    private string DeterminePropertyPath(ObjectProperty property)
    {
        if (property.IsCollectionItem)
        {
            // For collection items, use the full indexed name (e.g., "Layers[0]")
            return property.Name;
        }
        else
        {
            // For regular properties, use the property name
            return property.Name;
        }
    }

    /// <summary>
    /// Navigates to a breadcrumb item when clicked
    /// </summary>
    /// <param name="breadcrumbItem">The breadcrumb item to navigate to</param>
    public void NavigateToBreadcrumb(BreadcrumbItem breadcrumbItem)
    {
        if (breadcrumbItem == null)
            return;

        // Check if this is a history-based navigation
        if (breadcrumbItem.HistoryIndex >= 0)
        {
            // Navigate using history index
            var leftBreadcrumbs = LeftNavigationHistory.GetBreadcrumbsForHistoryIndex(breadcrumbItem.HistoryIndex);
            var rightBreadcrumbs = RightNavigationHistory.GetBreadcrumbsForHistoryIndex(breadcrumbItem.HistoryIndex);

            if (leftBreadcrumbs.Count > 0 && LeftDocument?.Document != null)
            {
                var targetBreadcrumb = leftBreadcrumbs[leftBreadcrumbs.Count - 1];
                NavigateToBreadcrumbInternal(LeftDocument, targetBreadcrumb, leftBreadcrumbs);
            }

            if (rightBreadcrumbs.Count > 0 && RightDocument?.Document != null)
            {
                var targetBreadcrumb = rightBreadcrumbs[rightBreadcrumbs.Count - 1];
                NavigateToBreadcrumbInternal(RightDocument, targetBreadcrumb, rightBreadcrumbs);
            }
        }
        else
        {
            // Standard breadcrumb navigation
            NavigateToBreadcrumbInternal(LeftDocument, breadcrumbItem, null);
            NavigateToBreadcrumbInternal(RightDocument, breadcrumbItem, null);
        }
    }

    /// <summary>
    /// Internal method to navigate to a breadcrumb in a specific document
    /// </summary>
    /// <param name="document">The document to navigate in</param>
    /// <param name="breadcrumbItem">The breadcrumb item to navigate to</param>
    /// <param name="breadcrumbPath">Optional breadcrumb path to restore</param>
    private void NavigateToBreadcrumbInternal(CadDocumentModel? document, BreadcrumbItem breadcrumbItem, List<BreadcrumbItem>? breadcrumbPath)
    {
        if (document?.Document == null || breadcrumbItem == null)
            return;

        // Handle special navigation cases first
        if (HandleSpecialBreadcrumbNavigation(document, breadcrumbItem))
            return;

        // Handle property-based navigation (collections, etc.)
        if (!string.IsNullOrEmpty(breadcrumbItem.PropertyPath))
        {
            NavigateToPropertyPath(document, breadcrumbItem);
            return;
        }

        // Try different navigation strategies based on the object type
        if (breadcrumbItem.Object is ACadSharp.CadObject cadObject)
        {
            // Find the object in the tree and select it for synchronization
            var targetNode = FindObjectInTreeAndSelect(document, cadObject);
            if (targetNode != null)
            {
                // Navigate using the actual tree node to maintain tree view synchronization
                document.NavigateToTreeNodeWithoutHistory(targetNode);
                return;
            }

            // Fallback: Create a temporary node to represent the breadcrumb item
            var tempNode = new ACadSharp.Viewer.Interfaces.CadObjectTreeNode
            {
                Name = breadcrumbItem.Name,
                ObjectType = breadcrumbItem.Type,
                CadObject = cadObject,
                Handle = breadcrumbItem.Handle
            };

            document.NavigateToTreeNodeWithoutHistory(tempNode);
        }
        else if (breadcrumbItem.Handle != null)
        {
            // Try to navigate by handle
            try
            {
                if (document.Document.TryGetCadObject(breadcrumbItem.Handle.Value, out ACadSharp.CadObject objectByHandle))
                {
                    // Try to find and select in tree first
                    var targetNode = FindObjectInTreeAndSelect(document, objectByHandle);
                    if (targetNode != null)
                    {
                        document.NavigateToTreeNodeWithoutHistory(targetNode);
                        return;
                    }

                    // Fallback: Create temporary node
                    var tempNode = new ACadSharp.Viewer.Interfaces.CadObjectTreeNode
                    {
                        Name = breadcrumbItem.Name,
                        ObjectType = breadcrumbItem.Type,
                        CadObject = objectByHandle,
                        Handle = breadcrumbItem.Handle
                    };
                    document.NavigateToTreeNodeWithoutHistory(tempNode);
                }
            }
            catch
            {
                // Handle not found, continue with other navigation methods
            }
        }

        // Restore breadcrumb path if provided
        if (breadcrumbPath != null)
        {
            document.BreadcrumbItems.Clear();
            foreach (var item in breadcrumbPath)
            {
                document.BreadcrumbItems.Add(new BreadcrumbItem
                {
                    Name = item.Name,
                    Type = item.Type,
                    Object = item.Object,
                    Handle = item.Handle,
                    IsCurrent = item.IsCurrent,
                    HistoryIndex = item.HistoryIndex,
                    PropertyPath = item.PropertyPath,
                    IsCollectionItem = item.IsCollectionItem,
                    CollectionIndex = item.CollectionIndex
                });
            }
        }
    }

    /// <summary>
    /// Handles special breadcrumb navigation cases like Document, Tables, etc.
    /// </summary>
    /// <param name="document">The document to navigate in</param>
    /// <param name="breadcrumbItem">The breadcrumb item</param>
    /// <returns>True if navigation was handled</returns>
    private bool HandleSpecialBreadcrumbNavigation(CadDocumentModel document, BreadcrumbItem breadcrumbItem)
    {
        switch (breadcrumbItem.Type?.ToLowerInvariant())
        {
            case "document":
                // Navigate to document root - select the first node in tree if available
                if (document.ObjectTreeNodes.Count > 0)
                {
                    var documentNode = document.ObjectTreeNodes.FirstOrDefault(n => n.Name == "Document" || n.ObjectType == "Document");
                    if (documentNode != null)
                    {
                        SetSelectedTreeNode(document, documentNode);
                        return true;
                    }
                }
                break;

            case "tables":
            case "layers":
            case "blocks":
            case "linetypes":
            case "textstyles":
            case "dimensionstyles":
                // Navigate to specific table sections
                var tableNode = FindTableNode(document, breadcrumbItem.Name);
                if (tableNode != null)
                {
                    SetSelectedTreeNode(document, tableNode);
                    return true;
                }
                break;
        }

        return false;
    }

    /// <summary>
    /// Navigates to a specific property path (for collections and complex properties)
    /// </summary>
    /// <param name="document">The document to navigate in</param>
    /// <param name="breadcrumbItem">The breadcrumb item with property path</param>
    private void NavigateToPropertyPath(CadDocumentModel document, BreadcrumbItem breadcrumbItem)
    {
        // For now, just navigate to the object and let the property inspector show the details
        // This could be enhanced further to scroll to specific properties
        if (breadcrumbItem.Object != null)
        {
            document.NavigateToObject(breadcrumbItem.Object, breadcrumbItem.PropertyPath);
        }
    }

    /// <summary>
    /// Finds an object in the tree and selects the corresponding tree node
    /// </summary>
    /// <param name="document">The document to search in</param>
    /// <param name="cadObject">The CAD object to find</param>
    /// <returns>The tree node if found</returns>
    private CadObjectTreeNode? FindObjectInTreeAndSelect(CadDocumentModel document, ACadSharp.CadObject cadObject)
    {
        var targetNode = FindNodeInTree(document.ObjectTreeNodes, cadObject);
        if (targetNode != null)
        {
            SetSelectedTreeNode(document, targetNode);
        }
        return targetNode;
    }

    /// <summary>
    /// Recursively finds a CAD object in the tree nodes
    /// </summary>
    /// <param name="nodes">The tree nodes to search</param>
    /// <param name="cadObject">The CAD object to find</param>
    /// <returns>The tree node containing the object</returns>
    private CadObjectTreeNode? FindNodeInTree(IEnumerable<CadObjectTreeNode> nodes, ACadSharp.CadObject cadObject)
    {
        foreach (var node in nodes)
        {
            if (node.CadObject == cadObject)
                return node;

            var childResult = FindNodeInTree(node.Children, cadObject);
            if (childResult != null)
                return childResult;
        }
        return null;
    }

    /// <summary>
    /// Finds a table node by name in the document tree
    /// </summary>
    /// <param name="document">The document to search in</param>
    /// <param name="tableName">The name of the table</param>
    /// <returns>The table node if found</returns>
    private CadObjectTreeNode? FindTableNode(CadDocumentModel document, string tableName)
    {
        return FindNodeByName(document.ObjectTreeNodes, tableName);
    }

    /// <summary>
    /// Recursively finds a node by name
    /// </summary>
    /// <param name="nodes">The nodes to search</param>
    /// <param name="name">The name to search for</param>
    /// <returns>The node if found</returns>
    private CadObjectTreeNode? FindNodeByName(IEnumerable<CadObjectTreeNode> nodes, string name)
    {
        foreach (var node in nodes)
        {
            if (string.Equals(node.Name, name, StringComparison.OrdinalIgnoreCase))
                return node;

            var childResult = FindNodeByName(node.Children, name);
            if (childResult != null)
                return childResult;
        }
        return null;
    }

    /// <summary>
    /// Sets the selected tree node for proper tree view synchronization
    /// </summary>
    /// <param name="document">The document model</param>
    /// <param name="node">The node to select</param>
    private void SetSelectedTreeNode(CadDocumentModel document, CadObjectTreeNode node)
    {
        if (document == LeftDocument)
        {
            LeftDocument.SelectedTreeNode = node;
        }
        else if (document == RightDocument)
        {
            RightDocument.SelectedTreeNode = node;
        }
    }

    /// <summary>
    /// Goes back in navigation history
    /// </summary>
    public void GoBack()
    {
        // Try to go back in left document first, then right document
        var leftEntry = LeftNavigationHistory.GoBack();
        var rightEntry = RightNavigationHistory.GoBack();

        if (leftEntry != null && LeftDocument?.Document != null)
        {
            NavigateToHistoryEntry(LeftDocument, leftEntry);
        }

        if (rightEntry != null && RightDocument?.Document != null)
        {
            NavigateToHistoryEntry(RightDocument, rightEntry);
        }
    }

    /// <summary>
    /// Goes forward in navigation history
    /// </summary>
    public void GoForward()
    {
        // Try to go forward in left document first, then right document
        var leftEntry = LeftNavigationHistory.GoForward();
        var rightEntry = RightNavigationHistory.GoForward();

        if (leftEntry != null && LeftDocument?.Document != null)
        {
            NavigateToHistoryEntry(LeftDocument, leftEntry);
        }

        if (rightEntry != null && RightDocument?.Document != null)
        {
            NavigateToHistoryEntry(RightDocument, rightEntry);
        }
    }

    /// <summary>
    /// Navigates to a specific history entry
    /// </summary>
    /// <param name="document">The document to navigate in</param>
    /// <param name="entry">The history entry to navigate to</param>
    private void NavigateToHistoryEntry(CadDocumentModel document, NavigationHistoryEntry entry)
    {
        if (entry.Object != null && entry.Object is ACadSharp.CadObject cadObject)
        {
            // Create a temporary node to represent the history entry
            var tempNode = new ACadSharp.Viewer.Interfaces.CadObjectTreeNode
            {
                Name = entry.Name,
                ObjectType = entry.Type,
                CadObject = cadObject,
                Handle = entry.Handle
            };

            // Navigate without adding to history (to avoid infinite recursion)
            document.NavigateToTreeNodeWithoutHistory(tempNode);
            
            // Restore the breadcrumb path from history
            document.BreadcrumbItems.Clear();
            foreach (var breadcrumbItem in entry.BreadcrumbPath)
            {
                document.BreadcrumbItems.Add(new BreadcrumbItem
                {
                    Name = breadcrumbItem.Name,
                    Type = breadcrumbItem.Type,
                    Object = breadcrumbItem.Object,
                    Handle = breadcrumbItem.Handle,
                    IsCurrent = breadcrumbItem.IsCurrent,
                    HistoryIndex = breadcrumbItem.HistoryIndex
                });
            }
        }
        else if (entry.PropertyName != null && entry.Object != null)
        {
            // Navigate to property without adding to history
            document.NavigateToObjectWithoutHistory(entry.Object, entry.PropertyName);
        }
    }

    /// <summary>
    /// Opens the batch search window
    /// </summary>
    public void OpenBatchSearch()
    {
        var batchSearchWindow = new Views.BatchSearchWindow();
        var batchSearchFileDialogService = new FileDialogService(batchSearchWindow);
        batchSearchWindow.DataContext = new BatchSearchViewModel(batchSearchFileDialogService);
        batchSearchWindow.Show();
    }

    /// <summary>
    /// Captures the current expanded state of all tree nodes
    /// </summary>
    /// <param name="documentModel">The document model containing the tree nodes</param>
    /// <param name="expandedStateDict">Dictionary to store the expanded state</param>
    private void CaptureExpandedState(CadDocumentModel documentModel, Dictionary<string, bool> expandedStateDict)
    {
        if (documentModel?.ObjectTreeNodes == null) return;
        
        expandedStateDict.Clear();
        foreach (var node in documentModel.ObjectTreeNodes)
        {
            CaptureNodeExpandedState(node, expandedStateDict, "");
        }
    }
    
    /// <summary>
    /// Recursively captures the expanded state of a node and its children
    /// </summary>
    /// <param name="node">The node to capture state for</param>
    /// <param name="expandedStateDict">Dictionary to store the expanded state</param>
    /// <param name="path">Current path to the node</param>
    private void CaptureNodeExpandedState(CadObjectTreeNode node, Dictionary<string, bool> expandedStateDict, string path)
    {
        if (node == null) return;
        
        // Create a unique path for this node
        var nodePath = string.IsNullOrEmpty(path) ? node.Name : $"{path}/{node.Name}";
        
        // Store the expanded state
        expandedStateDict[nodePath] = node.IsExpanded;
        
        // Recursively capture children
        foreach (var child in node.Children)
        {
            CaptureNodeExpandedState(child, expandedStateDict, nodePath);
        }
    }
    
    /// <summary>
    /// Restores the expanded state of all tree nodes from stored state
    /// </summary>
    /// <param name="documentModel">The document model containing the tree nodes</param>
    /// <param name="expandedStateDict">Dictionary containing the stored expanded state</param>
    private void RestoreExpandedState(CadDocumentModel documentModel, Dictionary<string, bool> expandedStateDict)
    {
        if (documentModel?.ObjectTreeNodes == null || !expandedStateDict.Any()) return;
        
        foreach (var node in documentModel.ObjectTreeNodes)
        {
            RestoreNodeExpandedState(node, expandedStateDict, "");
        }
    }
    
    /// <summary>
    /// Recursively restores the expanded state of a node and its children
    /// </summary>
    /// <param name="node">The node to restore state for</param>
    /// <param name="expandedStateDict">Dictionary containing the stored expanded state</param>
    /// <param name="path">Current path to the node</param>
    private void RestoreNodeExpandedState(CadObjectTreeNode node, Dictionary<string, bool> expandedStateDict, string path)
    {
        if (node == null) return;
        
        // Create the same unique path used during capture
        var nodePath = string.IsNullOrEmpty(path) ? node.Name : $"{path}/{node.Name}";
        
        // Restore the expanded state if it was stored
        if (expandedStateDict.TryGetValue(nodePath, out bool wasExpanded))
        {
            node.IsExpanded = wasExpanded;
        }
        
        // Recursively restore children
        foreach (var child in node.Children)
        {
            RestoreNodeExpandedState(child, expandedStateDict, nodePath);
        }
    }

    /// <summary>
    /// Updates search suggestions based on the current search type
    /// </summary>
    private void UpdateSearchSuggestions()
    {
        try
        {
            // If search text is empty, show all suggestions for the current search type
            var filterText = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText;
            var suggestions = _searchSuggestionService.GetSuggestions(SearchType, filterText);
            SearchSuggestions.Clear();
            foreach (var suggestion in suggestions)
            {
                SearchSuggestions.Add(suggestion);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating search suggestions: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates search suggestions asynchronously when documents are loaded
    /// </summary>
    private async Task UpdateSearchSuggestionsAsync()
    {
        try
        {
            var documents = new List<CadDocument>();
            
            if (LeftDocument?.Document != null)
                documents.Add(LeftDocument.Document);
            
            if (RightDocument?.Document != null)
                documents.Add(RightDocument.Document);

            if (documents.Any())
            {
                await _searchSuggestionService.UpdateSuggestionsAsync(documents);
                UpdateSearchSuggestions();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating search suggestions async: {ex.Message}");
        }
    }


}
