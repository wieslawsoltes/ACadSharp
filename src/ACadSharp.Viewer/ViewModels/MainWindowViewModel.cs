using ACadSharp;
using ACadSharp.Viewer.Interfaces;
using ACadSharp.Viewer.Models;
using ACadSharp.Viewer.Services;
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
    private readonly IBatchSearchService _batchSearchService;
    private readonly IFileDialogService _fileDialogService;
    private CadDocumentModel _leftDocument;
    private CadDocumentModel _rightDocument;
    private string _searchText = string.Empty;
    private SearchType _searchType = SearchType.Handle;
    private bool _isSearching;
    private readonly object _highlightLock = new object();
    private bool _isHighlighting = false;
    private DateTime _lastRestoreTime = DateTime.MinValue;
    private readonly TimeSpan _restoreDebounceTime = TimeSpan.FromMilliseconds(100);
    private BatchSearchResultsViewModel? _batchSearchResults;
    private bool _isBatchSearching = false;
    private string _batchSearchStatus = string.Empty;
    private int _batchSearchProgress = 0;

    public MainWindowViewModel(IFileDialogService? fileDialogService = null)
    {
        _cadFileService = new CadFileService();
        _cadObjectTreeService = new CadObjectTreeService();
        _batchSearchService = new BatchSearchService();
        _fileDialogService = fileDialogService ?? new FileDialogService(null!); // Will be set properly
        _leftDocument = new CadDocumentModel();
        _rightDocument = new CadDocumentModel();

        Title = "ACadSharp Viewer - DWG/DXF File Comparison";

        // Commands - using simple commands to avoid threading issues
        LoadLeftFileCommand = ReactiveCommand.CreateFromTask<string>(LoadLeftFileAsync);
        LoadRightFileCommand = ReactiveCommand.CreateFromTask<string>(LoadRightFileAsync);
        LoadLeftFileNoParamCommand = ReactiveCommand.CreateFromTask(LoadLeftFileNoParamAsync);
        LoadRightFileNoParamCommand = ReactiveCommand.CreateFromTask(LoadRightFileNoParamAsync);
        ClearSearchCommand = ReactiveCommand.Create(ClearSearch);
        NavigateToPropertyCommand = ReactiveCommand.Create<ObjectProperty>(NavigateToProperty);
        NavigateToBreadcrumbCommand = ReactiveCommand.Create<BreadcrumbItem>(NavigateToBreadcrumb);
        StartBatchSearchCommand = ReactiveCommand.CreateFromTask(StartBatchSearchAsync);
        ShowBatchSearchResultsCommand = ReactiveCommand.Create(ShowBatchSearchResults);

        // Subscribe to progress events
        _cadFileService.LoadProgressChanged += OnLoadProgressChanged;
        _batchSearchService.ProgressChanged += OnBatchSearchProgressChanged;
        _batchSearchService.FileProcessed += OnBatchSearchFileProcessed;

        // Search text and type changes
        this.WhenAnyValue(x => x.SearchText, x => x.SearchType)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async tuple => 
            {
                try
                {
                    // Prevent multiple simultaneous operations
                    if (IsSearching) return;
                        
                    if (string.IsNullOrWhiteSpace(tuple.Item1))
                    {
                        // Clear search results immediately
                        ClearSearchResults();
                        // Then restore tree view state
                        RestoreTreeViewState();
                    }
                    else
                    {
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
    /// Command to start batch search
    /// </summary>
    public ICommand StartBatchSearchCommand { get; }

    /// <summary>
    /// Command to show batch search results
    /// </summary>
    public ICommand ShowBatchSearchResultsCommand { get; }

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
                FilterAndHighlightNodes(documentModel.ObjectTreeNodes, resultHandles, searchText);
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
    /// <returns>True if any node in this branch has a match</returns>
    private bool FilterAndHighlightNodes(ObservableCollection<CadObjectTreeNode> nodes, HashSet<ulong> resultHandles, string searchText)
    {
        if (nodes == null) return false;
            
        bool anyMatch = false;
            
        // Create a copy of the collection to avoid concurrent modification issues
        var nodesCopy = nodes.ToList();
        foreach (var node in nodesCopy)
        {
            bool hasMatchingChild = false;
            bool isDirectMatch = false;

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
                var childHasMatch = FilterAndHighlightNodes(node.Children, resultHandles, searchText);
                hasMatchingChild = hasMatchingChild || childHasMatch;
            }

            // Set visibility and highlighting
            // A node should be visible if:
            // 1. It directly matches the search criteria, OR
            // 2. It has children that match the search criteria (to show the path to matching nodes)
            node.IsVisible = hasMatchingChild;
            node.IsHighlighted = isDirectMatch;
                
            // Auto-expand nodes that have matches (either direct or in children)
            // This ensures the path to matching nodes is visible
            if (hasMatchingChild)
            {
                node.IsExpanded = true;
            }

            // Highlight properties if this node is selected and directly matches
            if (isDirectMatch && node == LeftDocument?.SelectedTreeNode && LeftDocument?.SelectedObjectProperties != null)
            {
                lock (_highlightLock)
                {
                    HighlightProperties(LeftDocument.SelectedObjectProperties, searchText);
                }
            }
            else if (isDirectMatch && node == RightDocument?.SelectedTreeNode && RightDocument?.SelectedObjectProperties != null)
            {
                lock (_highlightLock)
                {
                    HighlightProperties(RightDocument.SelectedObjectProperties, searchText);
                }
            }

            anyMatch = anyMatch || hasMatchingChild;
        }

        return anyMatch;
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
                    prop.IsHighlighted = prop.Name.ToLowerInvariant().Contains(searchLower) ||
                                         prop.Value.ToLowerInvariant().Contains(searchLower) ||
                                         prop.Type.ToLowerInvariant().Contains(searchLower);
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

        // Ensure all nodes are visible and update the filtered collection
        if (LeftDocument?.ObjectTreeNodes != null)
        {
            System.Diagnostics.Debug.WriteLine($"RestoreTreeViewState: Left document has {LeftDocument.ObjectTreeNodes.Count} root nodes");
            foreach (var node in LeftDocument.ObjectTreeNodes)
            {
                RestoreNodeState(node);
            }
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
                    return;
                }

                var searchText = SearchText.Trim();
                    
                // Highlight properties for left document
                if (LeftDocument?.SelectedTreeNode != null && LeftDocument?.SelectedObjectProperties != null)
                {
                    HighlightProperties(LeftDocument.SelectedObjectProperties, searchText);
                }

                // Highlight properties for right document
                if (RightDocument?.SelectedTreeNode != null && RightDocument?.SelectedObjectProperties != null)
                {
                    HighlightProperties(RightDocument.SelectedObjectProperties, searchText);
                }
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

        // If the property has a handle, try to navigate to the object in the tree first
        if (property.ObjectHandle.HasValue)
        {
            if (LeftDocument?.Document != null)
            {
                LeftDocument.NavigateToObjectByHandle(property.ObjectHandle.Value, property.Name);
            }

            if (RightDocument?.Document != null)
            {
                RightDocument.NavigateToObjectByHandle(property.ObjectHandle.Value, property.Name);
            }
        }
        else
        {
            // Otherwise, navigate directly to the object
            if (LeftDocument?.Document != null)
            {
                LeftDocument.NavigateToObject(property.PropertyObject, property.Name);
            }

            if (RightDocument?.Document != null)
            {
                RightDocument.NavigateToObject(property.PropertyObject, property.Name);
            }
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

        // Create a temporary node to represent the breadcrumb item
        var tempNode = new ACadSharp.Viewer.Interfaces.CadObjectTreeNode
        {
            Name = breadcrumbItem.Name,
            ObjectType = breadcrumbItem.Type,
            CadObject = breadcrumbItem.Object as ACadSharp.CadObject,
            Handle = breadcrumbItem.Handle
        };

        // Navigate to the object in the breadcrumb using tree navigation
        if (LeftDocument?.Document != null)
        {
            LeftDocument.NavigateToTreeNode(tempNode);
        }

        if (RightDocument?.Document != null)
        {
            RightDocument.NavigateToTreeNode(tempNode);
        }
    }

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
