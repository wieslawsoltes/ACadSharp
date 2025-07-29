using ACadSharp;
using ACadSharp.Viewer.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Linq;

namespace ACadSharp.Viewer.Models
{
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
        /// Collection of breadcrumb items for navigation
        /// </summary>
        public ObservableCollection<BreadcrumbItem> BreadcrumbItems { get; } = new();

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
                }
            }
            else
            {
                // Clear properties if no object is selected
                SelectedObjectProperties.Clear();
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
                var properties = GetObjectPropertiesForAnyObject(targetObject);
                
                SelectedObjectProperties.Clear();
                foreach (var property in properties)
                {
                    SelectedObjectProperties.Add(property);
                }
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
                IsCurrent = false
            });

            // Add the property that was clicked
            BreadcrumbItems.Add(new BreadcrumbItem
            {
                Name = propertyName,
                Type = "Property",
                Object = null,
                IsCurrent = false
            });

            // Add current object
            BreadcrumbItems.Add(new BreadcrumbItem
            {
                Name = targetObject.GetType().Name,
                Type = targetObject.GetType().Name,
                Object = targetObject,
                IsCurrent = true
            });
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
            FilteredObjectTreeNodes.Clear();
            
            // Create filtered copies of nodes that are visible or have visible children
            foreach (var node in ObjectTreeNodes)
            {
                var filteredNode = CreateFilteredNodeCopy(node);
                if (filteredNode != null)
                {
                    FilteredObjectTreeNodes.Add(filteredNode);
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
                foreach (var child in originalNode.Children)
                {
                    var filteredChild = CreateFilteredNodeCopy(child);
                    if (filteredChild != null)
                    {
                        filteredNode.Children.Add(filteredChild);
                    }
                }

                return filteredNode;
            }

            // If the node is not visible but has children, check if any children are visible
            if (originalNode.Children.Any())
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
            BreadcrumbItems.Clear();
            SelectedObject = null;
        }
    }
} 