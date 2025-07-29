using ACadSharp;
using ACadSharp.Viewer.Interfaces;
using ACadSharp.Viewer.Models;
using ACadSharp.Viewer.Services;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using System.Reactive.Concurrency;

namespace ACadSharp.Viewer.ViewModels
{
    /// <summary>
    /// Main window ViewModel for the CAD file viewer
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly ICadFileService _cadFileService;
        private readonly ICadObjectTreeService _cadObjectTreeService;
        private readonly IFileDialogService _fileDialogService;
        private CadDocumentModel _leftDocument;
        private CadDocumentModel _rightDocument;
        private string _searchText = string.Empty;
        private bool _isSearching;

        public MainWindowViewModel(IFileDialogService? fileDialogService = null)
        {
            _cadFileService = new CadFileService();
            _cadObjectTreeService = new CadObjectTreeService();
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

            // Subscribe to progress events
            _cadFileService.LoadProgressChanged += OnLoadProgressChanged;

            // Search text changes
            this.WhenAnyValue(x => x.SearchText)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async _ => await SearchAsync());

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
        }

        /// <summary>
        /// Left document (DWG)
        /// </summary>
        public CadDocumentModel LeftDocument
        {
            get => _leftDocument;
            set => this.RaiseAndSetIfChanged(ref _leftDocument, value);
        }

        /// <summary>
        /// Right document (DXF)
        /// </summary>
        public CadDocumentModel RightDocument
        {
            get => _rightDocument;
            set => this.RaiseAndSetIfChanged(ref _rightDocument, value);
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
                    LeftDocument.ObjectTreeNodes.Clear();
                    foreach (var node in treeNodes)
                    {
                        LeftDocument.ObjectTreeNodes.Add(node);
                    }
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
                    RightDocument.ObjectTreeNodes.Clear();
                    foreach (var node in treeNodes)
                    {
                        RightDocument.ObjectTreeNodes.Add(node);
                    }
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
            if (string.IsNullOrWhiteSpace(SearchText))
                return;

            try
            {
                IsSearching = true;

                var searchCriteria = new SearchCriteria
                {
                    ObjectData = SearchText,
                    ObjectHandle = SearchText,
                    ObjectType = SearchText,
                    CaseSensitive = false
                };

                // Search in left document
                if (LeftDocument.Document != null)
                {
                    var leftResults = await _cadObjectTreeService.SearchObjectsAsync(LeftDocument.Document, searchCriteria);
                    // Highlight results in tree
                    HighlightSearchResults(LeftDocument.ObjectTreeNodes, leftResults);
                }

                // Search in right document
                if (RightDocument.Document != null)
                {
                    var rightResults = await _cadObjectTreeService.SearchObjectsAsync(RightDocument.Document, searchCriteria);
                    // Highlight results in tree
                    HighlightSearchResults(RightDocument.ObjectTreeNodes, rightResults);
                }
            }
            catch (Exception)
            {
                // Handle search errors
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
            SearchText = string.Empty;
            // Clear highlighting from trees
            ClearSearchHighlighting(LeftDocument.ObjectTreeNodes);
            ClearSearchHighlighting(RightDocument.ObjectTreeNodes);
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
        /// Highlights search results in the object tree
        /// </summary>
        /// <param name="nodes">Tree nodes to search</param>
        /// <param name="searchResults">Search results to highlight</param>
        private void HighlightSearchResults(ObservableCollection<CadObjectTreeNode> nodes, System.Collections.Generic.IEnumerable<CadObject> searchResults)
        {
            var resultHandles = searchResults.Select(r => r.Handle).ToHashSet();
            
            foreach (var node in nodes)
            {
                if (node.CadObject != null && resultHandles.Contains(node.CadObject.Handle))
                {
                    // Highlight this node (you would add a property for this)
                }
                
                // Recursively search children
                if (node.Children.Any())
                {
                    HighlightSearchResults(new ObservableCollection<CadObjectTreeNode>(node.Children), searchResults);
                }
            }
        }

        /// <summary>
        /// Clears search highlighting from the object tree
        /// </summary>
        /// <param name="nodes">Tree nodes to clear</param>
        private void ClearSearchHighlighting(ObservableCollection<CadObjectTreeNode> nodes)
        {
            foreach (var node in nodes)
            {
                // Clear highlighting from this node
                
                // Recursively clear children
                if (node.Children.Any())
                {
                    ClearSearchHighlighting(new ObservableCollection<CadObjectTreeNode>(node.Children));
                }
            }
        }
    }
} 