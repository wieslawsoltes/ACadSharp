using ACadSharp;
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