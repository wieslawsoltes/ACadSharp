using ACadSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ACadSharp.Viewer.Interfaces;

/// <summary>
/// Search type for CAD object search
/// </summary>
public enum SearchType
{
    Handle,
    ObjectType,
    ObjectData,
    TagCode,
    PropertyName,
    PropertyType,
    All
}

/// <summary>
/// Interface for CAD object tree operations
/// </summary>
public interface ICadObjectTreeService
{
    /// <summary>
    /// Builds a hierarchical tree structure from a CAD document
    /// </summary>
    /// <param name="document">The CAD document</param>
    /// <returns>Tree nodes representing the document structure</returns>
    Task<IEnumerable<CadObjectTreeNode>> BuildObjectTreeAsync(CadDocument document);

    /// <summary>
    /// Searches for objects in the tree by various criteria
    /// </summary>
    /// <param name="document">The CAD document</param>
    /// <param name="searchCriteria">Search criteria</param>
    /// <returns>Matching objects</returns>
    Task<IEnumerable<CadObject>> SearchObjectsAsync(CadDocument document, SearchCriteria searchCriteria);

    /// <summary>
    /// Gets all properties of a CAD object
    /// </summary>
    /// <param name="cadObject">The CAD object</param>
    /// <returns>Object properties</returns>
    Task<IEnumerable<ObjectProperty>> GetObjectPropertiesAsync(CadObject cadObject);
}

/// <summary>
/// Represents a node in the CAD object tree
/// </summary>
public class CadObjectTreeNode : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private CadObject? _cadObject;
    private string _objectType = string.Empty;
    private ulong? _handle;
    private bool _isExpanded;
    private bool _hasChildren;
    private ObservableCollection<CadObjectTreeNode> _children = new ObservableCollection<CadObjectTreeNode>();
    private bool _isHighlighted;
    private bool _isVisible = true;

    public string Name 
    { 
        get => _name; 
        set => SetProperty(ref _name, value); 
    }
        
    public CadObject? CadObject 
    { 
        get => _cadObject; 
        set => SetProperty(ref _cadObject, value); 
    }
        
    public string ObjectType 
    { 
        get => _objectType; 
        set => SetProperty(ref _objectType, value); 
    }
        
    public ulong? Handle 
    { 
        get => _handle; 
        set => SetProperty(ref _handle, value); 
    }
        
    public bool IsExpanded 
    { 
        get => _isExpanded; 
        set => SetProperty(ref _isExpanded, value); 
    }
        
    public bool HasChildren 
    { 
        get => _hasChildren; 
        set => SetProperty(ref _hasChildren, value); 
    }
        
    public ObservableCollection<CadObjectTreeNode> Children 
    { 
        get => _children; 
        set => SetProperty(ref _children, value); 
    }

    /// <summary>
    /// Indicates if this node should be highlighted due to search results
    /// </summary>
    public bool IsHighlighted 
    { 
        get => _isHighlighted; 
        set => SetProperty(ref _isHighlighted, value); 
    }

    /// <summary>
    /// Indicates if this node should be visible in the filtered view
    /// </summary>
    public bool IsVisible 
    { 
        get => _isVisible; 
        set => SetProperty(ref _isVisible, value); 
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
/// Search criteria for finding CAD objects
/// </summary>
public class SearchCriteria
{
    public string? SearchText { get; set; }
    public SearchType SearchType { get; set; } = SearchType.Handle;
    public bool CaseSensitive { get; set; }

    // Legacy properties for backward compatibility
    public string? TagCode => SearchType == SearchType.TagCode ? SearchText : null;
    public string? ObjectHandle => SearchType == SearchType.Handle ? SearchText : null;
    public string? ObjectData => SearchType == SearchType.ObjectData ? SearchText : null;
    public string? ObjectType => SearchType == SearchType.ObjectType ? SearchText : null;
    public string? PropertyName => SearchType == SearchType.PropertyName ? SearchText : null;
    public string? PropertyType => SearchType == SearchType.PropertyType ? SearchText : null;
}

/// <summary>
/// Represents a property of a CAD object
/// </summary>
public class ObjectProperty : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _value = string.Empty;
    private string _type = string.Empty;
    private bool _isReadOnly;
    private bool _isHighlighted;
    private bool _isNavigable;
    private object? _propertyObject;
    private ulong? _objectHandle;

    public string Name 
    { 
        get => _name; 
        set => SetProperty(ref _name, value); 
    }
        
    public string Value 
    { 
        get => _value; 
        set => SetProperty(ref _value, value); 
    }
        
    public string Type 
    { 
        get => _type; 
        set => SetProperty(ref _type, value); 
    }
        
    public bool IsReadOnly 
    { 
        get => _isReadOnly; 
        set => SetProperty(ref _isReadOnly, value); 
    }

    /// <summary>
    /// Indicates if this property should be highlighted due to search results
    /// </summary>
    public bool IsHighlighted 
    { 
        get => _isHighlighted; 
        set => SetProperty(ref _isHighlighted, value); 
    }

    /// <summary>
    /// Indicates if this property is navigable (clickable to view object properties)
    /// </summary>
    public bool IsNavigable 
    { 
        get => _isNavigable; 
        set => SetProperty(ref _isNavigable, value); 
    }

    /// <summary>
    /// The actual object value of this property (for navigation)
    /// </summary>
    public object? PropertyObject 
    { 
        get => _propertyObject; 
        set => SetProperty(ref _propertyObject, value); 
    }

    /// <summary>
    /// Handle of the object this property references (for tree navigation)
    /// </summary>
    public ulong? ObjectHandle 
    { 
        get => _objectHandle; 
        set => SetProperty(ref _objectHandle, value); 
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
/// Represents an item in the breadcrumb navigation
/// </summary>
public class BreadcrumbItem : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _type = string.Empty;
    private object? _object;
    private ulong? _handle;
    private bool _isCurrent;

    public string Name 
    { 
        get => _name; 
        set => SetProperty(ref _name, value); 
    }

    public string Type 
    { 
        get => _type; 
        set => SetProperty(ref _type, value); 
    }

    public object? Object 
    { 
        get => _object; 
        set => SetProperty(ref _object, value); 
    }

    public ulong? Handle 
    { 
        get => _handle; 
        set => SetProperty(ref _handle, value); 
    }

    public bool IsCurrent 
    { 
        get => _isCurrent; 
        set => SetProperty(ref _isCurrent, value); 
    }

    private int _historyIndex = -1;
    public int HistoryIndex 
    { 
        get => _historyIndex; 
        set => SetProperty(ref _historyIndex, value); 
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
/// Represents a navigation entry in history
/// </summary>
public class NavigationHistoryEntry
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public object? Object { get; set; }
    public ulong? Handle { get; set; }
    public string? PropertyName { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public List<BreadcrumbItem> BreadcrumbPath { get; set; } = new();
}

/// <summary>
/// Manages navigation history with back/forward functionality
/// </summary>
public class NavigationHistory : INotifyPropertyChanged
{
    private readonly List<NavigationHistoryEntry> _history = new();
    private int _currentIndex = -1;
    private bool _canGoBack;
    private bool _canGoForward;

    public bool CanGoBack 
    { 
        get => _canGoBack; 
        private set => SetProperty(ref _canGoBack, value); 
    }

    public bool CanGoForward 
    { 
        get => _canGoForward; 
        private set => SetProperty(ref _canGoForward, value); 
    }

    public int CurrentIndex => _currentIndex;
    public int Count => _history.Count;

    /// <summary>
    /// Adds a new navigation entry to the history
    /// </summary>
    /// <param name="entry">The navigation entry to add</param>
    public void AddEntry(NavigationHistoryEntry entry)
    {
        // Remove any forward history when adding a new entry
        if (_currentIndex < _history.Count - 1)
        {
            _history.RemoveRange(_currentIndex + 1, _history.Count - _currentIndex - 1);
        }

        _history.Add(entry);
        _currentIndex = _history.Count - 1;

        // Limit history size to prevent memory issues
        if (_history.Count > 100)
        {
            _history.RemoveAt(0);
            _currentIndex--;
        }

        UpdateCanNavigate();
    }

    /// <summary>
    /// Goes back in navigation history
    /// </summary>
    /// <returns>The previous navigation entry, or null if can't go back</returns>
    public NavigationHistoryEntry? GoBack()
    {
        if (!CanGoBack) return null;

        _currentIndex--;
        UpdateCanNavigate();
        return _history[_currentIndex];
    }

    /// <summary>
    /// Goes forward in navigation history
    /// </summary>
    /// <returns>The next navigation entry, or null if can't go forward</returns>
    public NavigationHistoryEntry? GoForward()
    {
        if (!CanGoForward) return null;

        _currentIndex++;
        UpdateCanNavigate();
        return _history[_currentIndex];
    }

    /// <summary>
    /// Gets the current navigation entry
    /// </summary>
    /// <returns>The current navigation entry, or null if no history</returns>
    public NavigationHistoryEntry? GetCurrent()
    {
        if (_currentIndex >= 0 && _currentIndex < _history.Count)
            return _history[_currentIndex];
        return null;
    }

    /// <summary>
    /// Clears all navigation history
    /// </summary>
    public void Clear()
    {
        _history.Clear();
        _currentIndex = -1;
        UpdateCanNavigate();
    }

    /// <summary>
    /// Gets all breadcrumb items for a specific history index
    /// </summary>
    /// <param name="historyIndex">The history index to get breadcrumbs for</param>
    /// <returns>List of breadcrumb items</returns>
    public List<BreadcrumbItem> GetBreadcrumbsForHistoryIndex(int historyIndex)
    {
        if (historyIndex >= 0 && historyIndex < _history.Count)
        {
            var entry = _history[historyIndex];
            var breadcrumbs = new List<BreadcrumbItem>();

            // Create breadcrumb items from the stored path
            for (int i = 0; i < entry.BreadcrumbPath.Count; i++)
            {
                var originalItem = entry.BreadcrumbPath[i];
                breadcrumbs.Add(new BreadcrumbItem
                {
                    Name = originalItem.Name,
                    Type = originalItem.Type,
                    Object = originalItem.Object,
                    Handle = originalItem.Handle,
                    IsCurrent = false,
                    HistoryIndex = i < historyIndex ? i : -1 // Allow navigation to previous items in breadcrumb
                });
            }

            // Mark the current item
            if (breadcrumbs.Count > 0)
            {
                breadcrumbs[breadcrumbs.Count - 1].IsCurrent = true;
            }

            return breadcrumbs;
        }

        return new List<BreadcrumbItem>();
    }

    private void UpdateCanNavigate()
    {
        CanGoBack = _currentIndex > 0;
        CanGoForward = _currentIndex < _history.Count - 1;
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