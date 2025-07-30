using ACadSharp;
using ACadSharp.Viewer.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Linq;

namespace ACadSharp.Viewer.Models;

/// <summary>
/// Model representing a CAD document with its metadata
/// </summary>
public class CadDocumentModel : INotifyPropertyChanged
{
    private CadDocument? _document;
    private string _filePath = string.Empty;
    private string _fileName = string.Empty;
    private string _fileType = string.Empty;
    private bool _isLoaded;
    private string _statusMessage = string.Empty;
    private int _loadProgress;
    private bool _suppressHistoryUpdate = false;
    private NavigationHistory? _navigationHistory;

    /// <summary>
    /// The underlying ACadSharp document
    /// </summary>
    public CadDocument? Document
    {
        get => _document;
        set => SetProperty(ref _document, value);
    }

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
    /// Indicates if the document is loaded
    /// </summary>
    public bool IsLoaded
    {
        get => _isLoaded;
        set => SetProperty(ref _isLoaded, value);
    }

    /// <summary>
    /// Status message for loading operations
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// Progress percentage for loading operations
    /// </summary>
    public int LoadProgress
    {
        get => _loadProgress;
        set => SetProperty(ref _loadProgress, value);
    }

    /// <summary>
    /// Collection of tree nodes for the object tree
    /// </summary>
    public ObservableCollection<CadObjectTreeNode> ObjectTreeNodes { get; } = new();

    /// <summary>
    /// Filtered collection of tree nodes for the object tree (used for search filtering)
    /// </summary>
    public ObservableCollection<CadObjectTreeNode> FilteredObjectTreeNodes { get; } = new();

    /// <summary>
    /// Collection of properties for the selected object
    /// </summary>
    public ObservableCollection<ObjectProperty> SelectedObjectProperties { get; } = new();

    /// <summary>
    /// Filtered collection of properties for the selected object (used for search filtering)
    /// </summary>
    public ObservableCollection<ObjectProperty> FilteredSelectedObjectProperties { get; } = new();

    /// <summary>
    /// Collection of breadcrumb items for navigation
    /// </summary>
    public ObservableCollection<BreadcrumbItem> BreadcrumbItems { get; } = new();

    /// <summary>
    /// Navigation history for this document
    /// </summary>
    public NavigationHistory? NavigationHistory
    {
        get => _navigationHistory;
        set => SetProperty(ref _navigationHistory, value);
    }

    /// <summary>
    /// Currently selected object
    /// </summary>
    public CadObject? SelectedObject
    {
        get => _selectedObject;
        set
        {
            if (SetProperty(ref _selectedObject, value))
            {
                OnSelectedObjectChanged();
            }
        }
    }

    private CadObject? _selectedObject;

    /// <summary>
    /// Currently selected tree node
    /// </summary>
    public CadObjectTreeNode? SelectedTreeNode
    {
        get => _selectedTreeNode;
        set
        {
            if (SetProperty(ref _selectedTreeNode, value))
            {
                OnSelectedTreeNodeChanged();
            }
        }
    }

    private CadObjectTreeNode? _selectedTreeNode;

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

    private void OnSelectedObjectChanged()
    {
        // This will be handled by the ViewModel to update properties
    }

    private void OnSelectedTreeNodeChanged()
    {
        if (SelectedTreeNode != null)
        {
            // Clear existing properties
            SelectedObjectProperties.Clear();
            FilteredSelectedObjectProperties.Clear();
                
            if (SelectedTreeNode.CadObject != null)
            {
                // Update the selected object
                SelectedObject = SelectedTreeNode.CadObject;
                    
                // Update breadcrumb navigation
                UpdateBreadcrumbNavigation(SelectedTreeNode.CadObject);
                    
                // Get properties for the selected object
                var properties = GetObjectProperties(SelectedTreeNode.CadObject);
                    
                // Add properties to the collection
                foreach (var property in properties)
                {
                    SelectedObjectProperties.Add(property);
                }
                
                // Update filtered properties
                UpdateFilteredProperties(SearchType.Handle, null);
            }
            else
            {
                // Handle special cases like Document and Header nodes
                SelectedObject = null;
                UpdateBreadcrumbNavigation(SelectedTreeNode);
                var properties = GetSpecialNodeProperties(SelectedTreeNode);
                    
                // Add properties to the collection
                foreach (var property in properties)
                {
                    SelectedObjectProperties.Add(property);
                }
                
                // Update filtered properties
                UpdateFilteredProperties(SearchType.Handle, null);
            }
        }
        else
        {
                    // Clear properties if no object is selected
        SelectedObjectProperties.Clear();
        FilteredSelectedObjectProperties.Clear();
        BreadcrumbItems.Clear();
        SelectedObject = null;
        }
    }

    /// <summary>
    /// Updates the breadcrumb navigation for the current object
    /// </summary>
    /// <param name="cadObject">The CAD object</param>
    private void UpdateBreadcrumbNavigation(CadObject cadObject)
    {
        BreadcrumbItems.Clear();
            
        // Find the path from root to this object
        var path = FindPathToObject(cadObject);
            
        // Build breadcrumb items from the path
        for (int i = 0; i < path.Count; i++)
        {
            var node = path[i];
            BreadcrumbItems.Add(new BreadcrumbItem
            {
                Name = node.Name,
                Type = node.ObjectType,
                Object = node.CadObject,
                Handle = node.Handle,
                IsCurrent = (i == path.Count - 1)
            });
        }

        // Add to navigation history if not suppressed
        if (!_suppressHistoryUpdate && NavigationHistory != null && path.Count > 0)
        {
            var lastNode = path[path.Count - 1];
            var historyEntry = new NavigationHistoryEntry
            {
                Name = lastNode.Name,
                Type = lastNode.ObjectType,
                Object = lastNode.CadObject,
                Handle = lastNode.Handle,
                BreadcrumbPath = new List<BreadcrumbItem>(BreadcrumbItems.Select(b => new BreadcrumbItem
                {
                    Name = b.Name,
                    Type = b.Type,
                    Object = b.Object,
                    Handle = b.Handle,
                    IsCurrent = b.IsCurrent,
                    HistoryIndex = b.HistoryIndex
                }))
            };
            NavigationHistory.AddEntry(historyEntry);
        }
    }

    /// <summary>
    /// Updates the breadcrumb navigation for a tree node
    /// </summary>
    /// <param name="node">The tree node</param>
    private void UpdateBreadcrumbNavigation(CadObjectTreeNode node)
    {
        BreadcrumbItems.Clear();
            
        // Find the path from root to this node
        var path = FindPathToNode(node);
            
        // Build breadcrumb items from the path
        for (int i = 0; i < path.Count; i++)
        {
            var pathNode = path[i];
            BreadcrumbItems.Add(new BreadcrumbItem
            {
                Name = pathNode.Name,
                Type = pathNode.ObjectType,
                Object = pathNode.CadObject,
                Handle = pathNode.Handle,
                IsCurrent = (i == path.Count - 1)
            });
        }

        // Add to navigation history if not suppressed
        if (!_suppressHistoryUpdate && NavigationHistory != null && path.Count > 0)
        {
            var lastNode = path[path.Count - 1];
            var historyEntry = new NavigationHistoryEntry
            {
                Name = lastNode.Name,
                Type = lastNode.ObjectType,
                Object = lastNode.CadObject,
                Handle = lastNode.Handle,
                BreadcrumbPath = new List<BreadcrumbItem>(BreadcrumbItems.Select(b => new BreadcrumbItem
                {
                    Name = b.Name,
                    Type = b.Type,
                    Object = b.Object,
                    Handle = b.Handle,
                    IsCurrent = b.IsCurrent,
                    HistoryIndex = b.HistoryIndex
                }))
            };
            NavigationHistory.AddEntry(historyEntry);
        }
    }

    /// <summary>
    /// Finds the path from root to a specific object
    /// </summary>
    /// <param name="targetObject">The target object</param>
    /// <returns>List of nodes representing the path from root to the object</returns>
    private List<CadObjectTreeNode> FindPathToObject(object targetObject)
    {
        var path = new List<CadObjectTreeNode>();
            
        foreach (var rootNode in ObjectTreeNodes)
        {
            if (FindPathInNode(rootNode, targetObject, path))
            {
                return path;
            }
        }
            
        // If not found in tree, return just document level
        if (ObjectTreeNodes.Any())
        {
            path.Add(ObjectTreeNodes.First());
        }
            
        return path;
    }

    /// <summary>
    /// Finds the path from root to a specific node
    /// </summary>
    /// <param name="targetNode">The target node</param>
    /// <returns>List of nodes representing the path from root to the target node</returns>
    private List<CadObjectTreeNode> FindPathToNode(CadObjectTreeNode targetNode)
    {
        var path = new List<CadObjectTreeNode>();
            
        foreach (var rootNode in ObjectTreeNodes)
        {
            if (FindPathToNodeRecursive(rootNode, targetNode, path))
            {
                return path;
            }
        }
            
        // If not found, return just document level
        if (ObjectTreeNodes.Any())
        {
            path.Add(ObjectTreeNodes.First());
        }
            
        return path;
    }

    /// <summary>
    /// Recursively searches for a path to an object within a node and its children
    /// </summary>
    /// <param name="node">The current node</param>
    /// <param name="targetObject">The target object</param>
    /// <param name="path">The path being built</param>
    /// <returns>True if the path was found</returns>
    private bool FindPathInNode(CadObjectTreeNode node, object targetObject, List<CadObjectTreeNode> path)
    {
        // Add current node to path
        path.Add(node);
            
        // Check if this node contains the target object
        if (node.CadObject == targetObject)
        {
            return true;
        }
            
        // Search in children
        foreach (var child in node.Children)
        {
            if (FindPathInNode(child, targetObject, path))
            {
                return true;
            }
        }
            
        // If not found in this branch, remove this node from path
        path.RemoveAt(path.Count - 1);
        return false;
    }

    /// <summary>
    /// Recursively searches for a path to a specific node
    /// </summary>
    /// <param name="currentNode">The current node</param>
    /// <param name="targetNode">The target node</param>
    /// <param name="path">The path being built</param>
    /// <returns>True if the path was found</returns>
    private bool FindPathToNodeRecursive(CadObjectTreeNode currentNode, CadObjectTreeNode targetNode, List<CadObjectTreeNode> path)
    {
        // Add current node to path
        path.Add(currentNode);
            
        // Check if this is the target node
        if (currentNode == targetNode)
        {
            return true;
        }
            
        // Search in children
        foreach (var child in currentNode.Children)
        {
            if (FindPathToNodeRecursive(child, targetNode, path))
            {
                return true;
            }
        }
            
        // If not found in this branch, remove this node from path
        path.RemoveAt(path.Count - 1);
        return false;
    }

    /// <summary>
    /// Navigates to a specific object and updates the breadcrumb
    /// </summary>
    /// <param name="targetObject">The object to navigate to</param>
    /// <param name="propertyName">The property name that led to this navigation</param>
    public void NavigateToObject(object targetObject, string propertyName)
    {
        if (targetObject == null) return;

        // First, try to find the object in the tree
        var targetNode = FindObjectInTree(targetObject);
            
        if (targetNode != null)
        {
            // Select the node in the tree - this will update properties and breadcrumb
            SelectedTreeNode = targetNode;
        }
        else
        {
            // If not found in tree, just update properties and breadcrumb
            UpdateBreadcrumbForObject(targetObject, propertyName);
            
            // Check if the target object is a collection - if so, show collection items
            var properties = new List<ObjectProperty>();
            if (IsCollectionType(targetObject.GetType()) && targetObject is System.Collections.IEnumerable enumerable)
            {
                // Show individual collection items
                AddCollectionItemProperties(properties, "Items", enumerable);
            }
            else
            {
                // Show regular object properties
                properties = GetObjectPropertiesForAnyObject(targetObject).ToList();
            }
                
            SelectedObjectProperties.Clear();
            FilteredSelectedObjectProperties.Clear();
            foreach (var property in properties)
            {
                SelectedObjectProperties.Add(property);
            }
            
            // Update filtered properties
            UpdateFilteredProperties(SearchType.Handle, null);
        }
    }

    /// <summary>
    /// Navigates to a specific object by handle, prioritizing tree navigation
    /// </summary>
    /// <param name="handle">The handle of the object to navigate to</param>
    /// <param name="propertyName">The property name that led to this navigation</param>
    public void NavigateToObjectByHandle(ulong handle, string propertyName = "")
    {
        var targetNode = FindNodeByHandle(ObjectTreeNodes, handle);
        if (targetNode != null)
        {
            // Select the node in the tree - this will update properties and breadcrumb
            SelectedTreeNode = targetNode;
        }
        else
        {
            // If not found in tree, try to find the object in the document
            var targetObject = FindObjectByHandle(handle);
            if (targetObject != null)
            {
                NavigateToObject(targetObject, propertyName);
            }
        }
    }

    /// <summary>
    /// Navigates to a specific tree node (for breadcrumb navigation)
    /// </summary>
    /// <param name="targetNode">The target node to navigate to</param>
    public void NavigateToTreeNode(CadObjectTreeNode targetNode)
    {
        if (targetNode == null) return;

        // Find the node in the current tree structure
        var actualNode = FindNodeInTree(targetNode);
        if (actualNode != null)
        {
            // Select the node in the tree - this will update properties and breadcrumb
            SelectedTreeNode = actualNode;
        }
    }

    /// <summary>
    /// Navigates to a specific tree node without adding to history (for history navigation)
    /// </summary>
    /// <param name="targetNode">The target node to navigate to</param>
    public void NavigateToTreeNodeWithoutHistory(CadObjectTreeNode targetNode)
    {
        if (targetNode == null) return;

        // Find the node in the current tree structure
        var actualNode = FindNodeInTree(targetNode);
        if (actualNode != null)
        {
            // Select the node in the tree without triggering history update
            _suppressHistoryUpdate = true;
            try
            {
                SelectedTreeNode = actualNode;
            }
            finally
            {
                _suppressHistoryUpdate = false;
            }
        }
    }

    /// <summary>
    /// Navigates to an object without adding to history (for history navigation)
    /// </summary>
    /// <param name="targetObject">The target object</param>
    /// <param name="propertyName">The property name that led to this navigation</param>
    public void NavigateToObjectWithoutHistory(object targetObject, string propertyName)
    {
        _suppressHistoryUpdate = true;
        try
        {
            NavigateToObject(targetObject, propertyName);
        }
        finally
        {
            _suppressHistoryUpdate = false;
        }
    }

    /// <summary>
    /// Updates breadcrumb navigation for an object that's not in the tree
    /// </summary>
    /// <param name="targetObject">The target object</param>
    /// <param name="propertyName">The property name that led to this navigation</param>
    private void UpdateBreadcrumbForObject(object targetObject, string propertyName)
    {
        BreadcrumbItems.Clear();
            
        // Add document level
        BreadcrumbItems.Add(new BreadcrumbItem
        {
            Name = "Document",
            Type = "Document",
            Object = Document,
            IsCurrent = false,
            PropertyPath = ""
        });

        // Parse property name to handle collection items (e.g., "Layers[0]")
        var isCollectionItem = propertyName.Contains("[") && propertyName.Contains("]");
        if (isCollectionItem)
        {
            var baseName = propertyName.Substring(0, propertyName.IndexOf('['));
            var indexStr = propertyName.Substring(propertyName.IndexOf('[') + 1, propertyName.IndexOf(']') - propertyName.IndexOf('[') - 1);
            
            if (int.TryParse(indexStr, out int index))
            {
                // Add the parent collection property
                BreadcrumbItems.Add(new BreadcrumbItem
                {
                    Name = baseName,
                    Type = "Collection",
                    Object = null,
                    IsCurrent = false,
                    PropertyPath = baseName
                });

                // Add the collection item
                BreadcrumbItems.Add(new BreadcrumbItem
                {
                    Name = $"[{index}]",
                    Type = "CollectionItem",
                    Object = null,
                    IsCurrent = false,
                    PropertyPath = propertyName,
                    IsCollectionItem = true,
                    CollectionIndex = index
                });
            }
        }
        else
        {
            // Add the property that was clicked
            BreadcrumbItems.Add(new BreadcrumbItem
            {
                Name = propertyName,
                Type = "Property",
                Object = null,
                IsCurrent = false,
                PropertyPath = propertyName
            });
        }

        // Add current object
        BreadcrumbItems.Add(new BreadcrumbItem
        {
            Name = targetObject.GetType().Name,
            Type = targetObject.GetType().Name,
            Object = targetObject,
            IsCurrent = true
        });

        // Add to navigation history if not suppressed
        if (!_suppressHistoryUpdate && NavigationHistory != null)
        {
            var historyEntry = new NavigationHistoryEntry
            {
                Name = targetObject.GetType().Name,
                Type = targetObject.GetType().Name,
                Object = targetObject,
                PropertyName = propertyName,
                BreadcrumbPath = new List<BreadcrumbItem>(BreadcrumbItems.Select(b => new BreadcrumbItem
                {
                    Name = b.Name,
                    Type = b.Type,
                    Object = b.Object,
                    Handle = b.Handle,
                    IsCurrent = b.IsCurrent,
                    HistoryIndex = b.HistoryIndex
                }))
            };
            NavigationHistory.AddEntry(historyEntry);
        }
    }

    /// <summary>
    /// Finds an object in the tree by searching recursively
    /// </summary>
    /// <param name="targetObject">The object to find</param>
    /// <returns>The tree node containing the object, or null if not found</returns>
    private CadObjectTreeNode? FindObjectInTree(object targetObject)
    {
        foreach (var node in ObjectTreeNodes)
        {
            var result = FindObjectInNode(node, targetObject);
            if (result != null)
                return result;
        }
        return null;
    }

    /// <summary>
    /// Recursively searches for an object in a tree node and its children
    /// </summary>
    /// <param name="node">The node to search in</param>
    /// <param name="targetObject">The object to find</param>
    /// <returns>The tree node containing the object, or null if not found</returns>
    private CadObjectTreeNode? FindObjectInNode(CadObjectTreeNode node, object targetObject)
    {
        if (node.CadObject == targetObject)
            return node;

        foreach (var child in node.Children)
        {
            var result = FindObjectInNode(child, targetObject);
            if (result != null)
                return result;
        }
        return null;
    }

    /// <summary>
    /// Recursively finds a tree node by handle
    /// </summary>
    /// <param name="nodes">The nodes to search in</param>
    /// <param name="handle">The handle to find</param>
    /// <returns>The tree node with the specified handle, or null if not found</returns>
    private CadObjectTreeNode? FindNodeByHandle(ObservableCollection<CadObjectTreeNode> nodes, ulong handle)
    {
        foreach (var node in nodes)
        {
            if (node.Handle == handle)
                return node;

            var result = FindNodeByHandle(node.Children, handle);
            if (result != null)
                return result;
        }
        return null;
    }

    /// <summary>
    /// Finds an object in the document by handle
    /// </summary>
    /// <param name="handle">The handle to find</param>
    /// <returns>The object with the specified handle, or null if not found</returns>
    private CadObject? FindObjectByHandle(ulong handle)
    {
        if (Document == null) return null;

        // Try to get the object directly from the document
        return Document.GetCadObject(handle);
    }

    private IEnumerable<ObjectProperty> GetObjectProperties(CadObject cadObject)
    {
        var properties = new List<ObjectProperty>();
            
        try
        {
            // Get all public properties of the object
            var type = cadObject.GetType();
            var publicProperties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
            foreach (var prop in publicProperties)
            {
                try
                {
                    var value = prop.GetValue(cadObject);
                    var stringValue = value?.ToString() ?? "null";
                        
                    var objectProperty = new ObjectProperty
                    {
                        Name = prop.Name,
                        Value = stringValue,
                        Type = prop.PropertyType.Name,
                        PropertyInfo = prop,
                        SourceObject = cadObject,
                        IsReadOnly = !prop.CanWrite || prop.SetMethod?.IsPublic != true
                    };

                    // Determine if property is editable (writable primitive types or enums)
                    var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    objectProperty.IsEditable = prop.CanWrite && 
                                              prop.SetMethod?.IsPublic == true && 
                                              !objectProperty.IsReadOnly &&
                                              (underlyingType.IsPrimitive || 
                                               underlyingType == typeof(string) || 
                                               underlyingType == typeof(decimal) || 
                                               underlyingType.IsEnum);

                    // Check if this property is navigable (an object type)
                    if (value != null && !prop.PropertyType.IsPrimitive && prop.PropertyType != typeof(string))
                    {
                        objectProperty.IsNavigable = true;
                        objectProperty.PropertyObject = value;
                            
                        // If it's a CadObject, get its handle
                        if (value is CadObject cadObj)
                        {
                            objectProperty.ObjectHandle = cadObj.Handle;
                        }

                        // Check if this property is a collection or enumerable (but not string)
                        if (IsCollectionType(prop.PropertyType) && value is System.Collections.IEnumerable enumerable)
                        {
                            // Add the collection summary property as navigable
                            var collectionCount = GetCollectionCount(enumerable);
                            objectProperty.Value = $"{stringValue} (Count: {collectionCount})";
                            // Don't add individual items here - they'll be shown when navigating to this collection
                            properties.Add(objectProperty);
                            continue; // Skip adding the main property again
                        }
                    }

                    properties.Add(objectProperty);
                }
                catch
                {
                    // Skip properties that can't be read
                    properties.Add(new ObjectProperty
                    {
                        Name = prop.Name,
                        Value = "Error reading property",
                        Type = prop.PropertyType.Name
                    });
                }
            }
        }
        catch (Exception ex)
        {
            properties.Add(new ObjectProperty
            {
                Name = "Error",
                Value = ex.Message,
                Type = "Exception"
            });
        }
            
        return properties;
    }

    /// <summary>
    /// Gets properties for any object type (not just CadObject)
    /// </summary>
    /// <param name="obj">The object to get properties for</param>
    /// <returns>Collection of object properties</returns>
    private IEnumerable<ObjectProperty> GetObjectPropertiesForAnyObject(object obj)
    {
        var properties = new List<ObjectProperty>();
            
        try
        {
            // Get all public properties of the object
            var type = obj.GetType();
            var publicProperties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
            foreach (var prop in publicProperties)
            {
                try
                {
                    var value = prop.GetValue(obj);
                    var stringValue = value?.ToString() ?? "null";
                        
                    var objectProperty = new ObjectProperty
                    {
                        Name = prop.Name,
                        Value = stringValue,
                        Type = prop.PropertyType.Name
                    };

                    // Check if this property is navigable (an object type)
                    if (value != null && !prop.PropertyType.IsPrimitive && prop.PropertyType != typeof(string))
                    {
                        objectProperty.IsNavigable = true;
                        objectProperty.PropertyObject = value;
                            
                        // If it's a CadObject, get its handle
                        if (value is CadObject cadObj)
                        {
                            objectProperty.ObjectHandle = cadObj.Handle;
                        }

                        // Check if this property is a collection or enumerable (but not string)
                        if (IsCollectionType(prop.PropertyType) && value is System.Collections.IEnumerable enumerable)
                        {
                            // Add the collection summary property as navigable
                            var collectionCount = GetCollectionCount(enumerable);
                            objectProperty.Value = $"{stringValue} (Count: {collectionCount})";
                            // Don't add individual items here - they'll be shown when navigating to this collection
                            properties.Add(objectProperty);
                            continue; // Skip adding the main property again
                        }
                    }

                    properties.Add(objectProperty);
                }
                catch
                {
                    // Skip properties that can't be read
                    properties.Add(new ObjectProperty
                    {
                        Name = prop.Name,
                        Value = "Error reading property",
                        Type = prop.PropertyType.Name
                    });
                }
            }
        }
        catch (Exception ex)
        {
            properties.Add(new ObjectProperty
            {
                Name = "Error",
                Value = ex.Message,
                Type = "Exception"
            });
        }
            
        return properties;
    }

    /// <summary>
    /// Checks if a type is a collection type (but not string)
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <returns>True if the type is a collection</returns>
    private bool IsCollectionType(Type type)
    {
        if (type == typeof(string))
            return false;

        return typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && 
               !typeof(System.Collections.IDictionary).IsAssignableFrom(type);
    }

    /// <summary>
    /// Gets the count of items in a collection
    /// </summary>
    /// <param name="enumerable">The collection</param>
    /// <returns>The number of items</returns>
    private int GetCollectionCount(System.Collections.IEnumerable enumerable)
    {
        if (enumerable is System.Collections.ICollection collection)
            return collection.Count;

        int count = 0;
        foreach (var _ in enumerable)
            count++;
        return count;
    }

    /// <summary>
    /// Adds individual collection items as separate properties
    /// </summary>
    /// <param name="properties">The properties list to add to</param>
    /// <param name="propertyName">The name of the collection property</param>
    /// <param name="enumerable">The collection</param>
    private void AddCollectionItemProperties(List<ObjectProperty> properties, string propertyName, System.Collections.IEnumerable enumerable)
    {
        int index = 0;
        const int maxItems = 50; // Limit to prevent UI overload

        foreach (var item in enumerable)
        {
            if (index >= maxItems)
            {
                properties.Add(new ObjectProperty
                {
                    Name = $"{propertyName}[...more]",
                    Value = "Additional items not shown (limit reached)",
                    Type = "Info",
                    IsNavigable = false,
                    IsCollectionItem = true,
                    ParentPropertyName = propertyName
                });
                break;
            }

            var itemProperty = new ObjectProperty
            {
                Name = $"{propertyName}[{index}]",
                Value = item?.ToString() ?? "null",
                Type = item?.GetType().Name ?? "null",
                IsCollectionItem = true,
                CollectionIndex = index,
                ParentPropertyName = propertyName
            };

            // Check if the item is navigable
            if (item != null && !item.GetType().IsPrimitive && item.GetType() != typeof(string))
            {
                itemProperty.IsNavigable = true;
                itemProperty.PropertyObject = item;

                // If it's a CadObject, get its handle
                if (item is CadObject cadObj)
                {
                    itemProperty.ObjectHandle = cadObj.Handle;
                }
            }

            properties.Add(itemProperty);
            index++;
        }
    }

    private IEnumerable<ObjectProperty> GetSpecialNodeProperties(CadObjectTreeNode node)
    {
        var properties = new List<ObjectProperty>();
            
        try
        {
            // Add basic node information
            properties.Add(new ObjectProperty
            {
                Name = "Node Name",
                Value = node.Name,
                Type = "String"
            });
                
            properties.Add(new ObjectProperty
            {
                Name = "Object Type",
                Value = node.ObjectType,
                Type = "String"
            });
                
            properties.Add(new ObjectProperty
            {
                Name = "Handle",
                Value = node.Handle?.ToString() ?? "null",
                Type = "UInt64"
            });
                
            properties.Add(new ObjectProperty
            {
                Name = "Has Children",
                Value = node.HasChildren.ToString(),
                Type = "Boolean"
            });
                
            properties.Add(new ObjectProperty
            {
                Name = "Is Expanded",
                Value = node.IsExpanded.ToString(),
                Type = "Boolean"
            });
                
            // Handle specific node types
            if (node.Name == "Document" && Document != null)
            {
                // Add document-specific properties
                properties.Add(new ObjectProperty
                {
                    Name = "File Path",
                    Value = FilePath,
                    Type = "String"
                });
                    
                properties.Add(new ObjectProperty
                {
                    Name = "File Name",
                    Value = FileName,
                    Type = "String"
                });
                    
                properties.Add(new ObjectProperty
                {
                    Name = "File Type",
                    Value = FileType,
                    Type = "String"
                });
                    
                properties.Add(new ObjectProperty
                {
                    Name = "Is Loaded",
                    Value = IsLoaded.ToString(),
                    Type = "Boolean"
                });
                    
                properties.Add(new ObjectProperty
                {
                    Name = "Status Message",
                    Value = StatusMessage,
                    Type = "String"
                });
            }
            else if (node.Name == "Header" && Document != null && Document.Header != null)
            {
                // Add header-specific properties using reflection
                var header = Document.Header;
                {
                    var headerType = header.GetType();
                    var headerProperties = headerType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        
                    foreach (var prop in headerProperties)
                    {
                        try
                        {
                            var value = prop.GetValue(header);
                            var stringValue = value?.ToString() ?? "null";
                                
                            var objectProperty = new ObjectProperty
                            {
                                Name = prop.Name,
                                Value = stringValue,
                                Type = prop.PropertyType.Name
                            };

                            // Check if this property is navigable
                            if (value != null && !prop.PropertyType.IsPrimitive && prop.PropertyType != typeof(string))
                            {
                                objectProperty.IsNavigable = true;
                                objectProperty.PropertyObject = value;
                                    
                                if (value is CadObject cadObj)
                                {
                                    objectProperty.ObjectHandle = cadObj.Handle;
                                }
                            }

                            properties.Add(objectProperty);
                        }
                        catch
                        {
                            properties.Add(new ObjectProperty
                            {
                                Name = prop.Name,
                                Value = "Error reading property",
                                Type = prop.PropertyType.Name
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            properties.Add(new ObjectProperty
            {
                Name = "Error",
                Value = ex.Message,
                Type = "Exception"
            });
        }
            
        return properties;
    }

    /// <summary>
    /// Finds a specific node in the current tree structure
    /// </summary>
    /// <param name="targetNode">The node to find</param>
    /// <returns>The actual node in the tree, or null if not found</returns>
    private CadObjectTreeNode? FindNodeInTree(CadObjectTreeNode targetNode)
    {
        foreach (var rootNode in ObjectTreeNodes)
        {
            var result = FindNodeInTreeRecursive(rootNode, targetNode);
            if (result != null)
                return result;
        }
        return null;
    }

    /// <summary>
    /// Recursively searches for a node in the tree
    /// </summary>
    /// <param name="currentNode">The current node</param>
    /// <param name="targetNode">The target node</param>
    /// <returns>The matching node, or null if not found</returns>
    private CadObjectTreeNode? FindNodeInTreeRecursive(CadObjectTreeNode currentNode, CadObjectTreeNode targetNode)
    {
        // Check if this is the target node (by comparing key properties)
        if (IsSameNode(currentNode, targetNode))
            return currentNode;

        // Search in children
        foreach (var child in currentNode.Children)
        {
            var result = FindNodeInTreeRecursive(child, targetNode);
            if (result != null)
                return result;
        }
        return null;
    }

    /// <summary>
    /// Determines if two nodes represent the same object
    /// </summary>
    /// <param name="node1">First node</param>
    /// <param name="node2">Second node</param>
    /// <returns>True if the nodes represent the same object</returns>
    private bool IsSameNode(CadObjectTreeNode node1, CadObjectTreeNode node2)
    {
        // Compare by handle if available
        if (node1.Handle.HasValue && node2.Handle.HasValue)
        {
            return node1.Handle.Value == node2.Handle.Value;
        }

        // Compare by object reference if available
        if (node1.CadObject != null && node2.CadObject != null)
        {
            return node1.CadObject == node2.CadObject;
        }

        // Compare by name and type for non-object nodes
        return node1.Name == node2.Name && node1.ObjectType == node2.ObjectType;
    }

    /// <summary>
    /// Updates the filtered tree nodes collection based on visibility
    /// </summary>
    public void UpdateFilteredTreeNodes()
    {
        System.Diagnostics.Debug.WriteLine($"UpdateFilteredTreeNodes: Starting update, original nodes count: {ObjectTreeNodes?.Count ?? 0}");
            
        // Clear the filtered collection completely to prevent duplicates
        FilteredObjectTreeNodes.Clear();
            
        // Only proceed if we have original nodes
        if (ObjectTreeNodes == null || !ObjectTreeNodes.Any())
        {
            System.Diagnostics.Debug.WriteLine("UpdateFilteredTreeNodes: No original nodes to process");
            return;
        }
            
        // Create filtered copies of nodes that are visible or have visible children
        foreach (var node in ObjectTreeNodes)
        {
            var filteredNode = CreateFilteredNodeCopy(node);
            if (filteredNode != null)
            {
                FilteredObjectTreeNodes.Add(filteredNode);
            }
        }
            
        System.Diagnostics.Debug.WriteLine($"UpdateFilteredTreeNodes: Completed, filtered nodes count: {FilteredObjectTreeNodes.Count}");
    }

    /// <summary>
    /// Updates the filtered properties collection based on search criteria
    /// </summary>
    /// <param name="searchType">The search type</param>
    /// <param name="searchText">The search text</param>
    public void UpdateFilteredProperties(SearchType searchType, string? searchText)
    {
        // Clear the filtered collection
        FilteredSelectedObjectProperties.Clear();
        
        // If no search text or not a property-specific search, show all properties
        if (string.IsNullOrEmpty(searchText) || 
            (searchType != SearchType.PropertyName && searchType != SearchType.PropertyType))
        {
            foreach (var prop in SelectedObjectProperties)
            {
                FilteredSelectedObjectProperties.Add(prop);
            }
            return;
        }
        
        var searchLower = searchText.ToLowerInvariant();
        
        // Filter properties based on search type
        foreach (var prop in SelectedObjectProperties)
        {
            bool shouldInclude = false;
            
            switch (searchType)
            {
                case SearchType.PropertyName:
                    shouldInclude = prop.Name.ToLowerInvariant().Contains(searchLower);
                    break;
                case SearchType.PropertyType:
                    shouldInclude = prop.Type.ToLowerInvariant().Contains(searchLower);
                    break;
            }
            
            if (shouldInclude)
            {
                FilteredSelectedObjectProperties.Add(prop);
            }
        }
    }

    /// <summary>
    /// Creates a filtered copy of a node and its children
    /// </summary>
    /// <param name="originalNode">The original node to filter</param>
    /// <returns>A filtered node copy or null if the node and all its children are not visible</returns>
    private CadObjectTreeNode? CreateFilteredNodeCopy(CadObjectTreeNode originalNode)
    {
        if (originalNode == null)
            return null;

        // If the node itself is visible, include it
        if (originalNode.IsVisible)
        {
            var filteredNode = new CadObjectTreeNode
            {
                Name = originalNode.Name,
                CadObject = originalNode.CadObject,
                ObjectType = originalNode.ObjectType,
                Handle = originalNode.Handle,
                IsExpanded = originalNode.IsExpanded,
                HasChildren = originalNode.HasChildren,
                IsHighlighted = originalNode.IsHighlighted,
                IsVisible = true // Always true in filtered collection
            };

            // Add visible children
            if (originalNode.Children != null)
            {
                foreach (var child in originalNode.Children)
                {
                    var filteredChild = CreateFilteredNodeCopy(child);
                    if (filteredChild != null)
                    {
                        filteredNode.Children.Add(filteredChild);
                    }
                }
            }

            return filteredNode;
        }

        // If the node is not visible but has children, check if any children are visible
        if (originalNode.Children != null && originalNode.Children.Any())
        {
            var visibleChildren = new List<CadObjectTreeNode>();
                
            foreach (var child in originalNode.Children)
            {
                var filteredChild = CreateFilteredNodeCopy(child);
                if (filteredChild != null)
                {
                    visibleChildren.Add(filteredChild);
                }
            }

            // If we have visible children, create a node to contain them
            if (visibleChildren.Any())
            {
                var filteredNode = new CadObjectTreeNode
                {
                    Name = originalNode.Name,
                    CadObject = originalNode.CadObject,
                    ObjectType = originalNode.ObjectType,
                    Handle = originalNode.Handle,
                    IsExpanded = originalNode.IsExpanded,
                    HasChildren = true,
                    IsHighlighted = false,
                    IsVisible = true
                };

                foreach (var child in visibleChildren)
                {
                    filteredNode.Children.Add(child);
                }

                return filteredNode;
            }
        }

        return null;
    }

    /// <summary>
    /// Clears the document and resets all properties
    /// </summary>
    public void Clear()
    {
        Document = null;
        FilePath = string.Empty;
        FileName = string.Empty;
        FileType = string.Empty;
        IsLoaded = false;
        StatusMessage = string.Empty;
        LoadProgress = 0;
        ObjectTreeNodes.Clear();
        FilteredObjectTreeNodes.Clear();
        SelectedObjectProperties.Clear();
        FilteredSelectedObjectProperties.Clear();
        BreadcrumbItems.Clear();
        SelectedObject = null;
    }

    /// <summary>
    /// Refreshes the selected object properties to reflect any changes
    /// </summary>
    public void RefreshSelectedObjectProperties()
    {
        if (SelectedTreeNode?.CadObject != null)
        {
            // Re-generate properties for the currently selected object
            var refreshedProperties = GetObjectProperties(SelectedTreeNode.CadObject).ToList();
            
            // Update the property collections
            SelectedObjectProperties.Clear();
            foreach (var prop in refreshedProperties)
            {
                SelectedObjectProperties.Add(prop);
            }
            
            // Update filtered properties as well
            UpdateFilteredProperties(SearchType.Handle, null);
        }
    }
}
