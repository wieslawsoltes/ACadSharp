using ACadSharp;
using ACadSharp.Viewer.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
        /// Collection of properties for the selected object
        /// </summary>
        public ObservableCollection<ObjectProperty> SelectedObjectProperties { get; } = new();

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
                SelectedObject = null;
            }
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
                        
                        properties.Add(new ObjectProperty
                        {
                            Name = prop.Name,
                            Value = stringValue,
                            Type = prop.PropertyType.Name
                        });
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
                    Value = node.Handle.ToString(),
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
                                
                                properties.Add(new ObjectProperty
                                {
                                    Name = prop.Name,
                                    Value = stringValue,
                                    Type = prop.PropertyType.Name
                                });
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
            SelectedObjectProperties.Clear();
            SelectedObject = null;
        }
    }
} 