using ACadSharp;
using ACadSharp.Classes;
using ACadSharp.Entities;
using ACadSharp.Objects;
using ACadSharp.Objects.Collections;
using ACadSharp.Tables;
using ACadSharp.Viewer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ACadSharp.Viewer.Services
{
    /// <summary>
    /// Service for building and managing CAD object trees
    /// </summary>
    public class CadObjectTreeService : ICadObjectTreeService
    {
        /// <summary>
        /// Builds a hierarchical tree structure from a CAD document
        /// </summary>
        /// <param name="document">The CAD document</param>
        /// <returns>Tree nodes representing the document structure</returns>
        public async Task<IEnumerable<CadObjectTreeNode>> BuildObjectTreeAsync(CadDocument document)
        {
            return await Task.Run(() =>
            {
                var rootNodes = new List<CadObjectTreeNode>();

                // Document root
                var documentNode = new CadObjectTreeNode
                {
                    Name = "Document",
                    CadObject = null, // Document is not a CadObject
                    ObjectType = "Document",
                    Handle = document.Handle,
                    HasChildren = true,
                    IsExpanded = true
                };

                // Header
                if (document.Header != null)
                {
                    var headerNode = new CadObjectTreeNode
                    {
                        Name = "Header",
                        CadObject = null, // Header is not a CadObject
                        ObjectType = "Header",
                        Handle = 0, // Header doesn't have a Handle property
                        HasChildren = false
                    };
                    documentNode.Children.Add(headerNode);
                }

                // Summary Info
                if (document.SummaryInfo != null)
                {
                    var summaryNode = new CadObjectTreeNode
                    {
                        Name = "Summary Info",
                        CadObject = null, // SummaryInfo is not a CadObject
                        ObjectType = "SummaryInfo",
                        Handle = 0,
                        HasChildren = false
                    };
                    documentNode.Children.Add(summaryNode);
                }

                // Classes
                if (document.Classes != null && document.Classes.Any())
                {
                    var classesNode = new CadObjectTreeNode
                    {
                        Name = "Classes",
                        ObjectType = "Classes",
                        HasChildren = true,
                        IsExpanded = false
                    };

                    var classNodes = document.Classes.Select(cls => new CadObjectTreeNode
                    {
                        Name = $"{cls.DxfName} ({cls.CppClassName})",
                        CadObject = null, // Classes are not CadObjects
                        ObjectType = "DxfClass",
                        Handle = 0,
                        HasChildren = false
                    });

                    foreach (var node in classNodes)
                    {
                        classesNode.Children.Add(node);
                    }
                    documentNode.Children.Add(classesNode);
                }

                // Tables
                var tablesNode = new CadObjectTreeNode
                {
                    Name = "Tables",
                    ObjectType = "Tables",
                    HasChildren = true,
                    IsExpanded = true
                };

                var tableNodes = new List<CadObjectTreeNode>();

                // Layers
                if (document.Layers != null)
                {
                    tableNodes.Add(CreateTableNode<Layer>("Layers", document.Layers));
                }

                // Block Records
                if (document.BlockRecords != null)
                {
                    tableNodes.Add(CreateTableNode<BlockRecord>("Block Records", document.BlockRecords));
                }

                // Text Styles
                if (document.TextStyles != null)
                {
                    tableNodes.Add(CreateTableNode<TextStyle>("Text Styles", document.TextStyles));
                }

                // Line Types
                if (document.LineTypes != null)
                {
                    tableNodes.Add(CreateTableNode<LineType>("Line Types", document.LineTypes));
                }

                // Dimension Styles
                if (document.DimensionStyles != null)
                {
                    tableNodes.Add(CreateTableNode<DimensionStyle>("Dimension Styles", document.DimensionStyles));
                }

                // Views
                if (document.Views != null)
                {
                    tableNodes.Add(CreateTableNode<View>("Views", document.Views));
                }

                // VPorts
                if (document.VPorts != null)
                {
                    tableNodes.Add(CreateTableNode<VPort>("VPorts", document.VPorts));
                }

                // UCSs
                if (document.UCSs != null)
                {
                    tableNodes.Add(CreateTableNode<UCS>("UCSs", document.UCSs));
                }

                // AppIds
                if (document.AppIds != null)
                {
                    tableNodes.Add(CreateTableNode<AppId>("AppIds", document.AppIds));
                }

                foreach (var node in tableNodes)
                {
                    tablesNode.Children.Add(node);
                }
                documentNode.Children.Add(tablesNode);

                // Collections (from Root Dictionary)
                var collectionsNode = new CadObjectTreeNode
                {
                    Name = "Collections",
                    ObjectType = "Collections",
                    HasChildren = true,
                    IsExpanded = false
                };

                var collectionNodes = new List<CadObjectTreeNode>();

                // Groups
                if (document.Groups != null)
                {
                    collectionNodes.Add(CreateCollectionNode<Group>("Groups", document.Groups));
                }

                // Colors
                if (document.Colors != null)
                {
                    collectionNodes.Add(CreateCollectionNode<BookColor>("Colors", document.Colors));
                }

                // Image Definitions
                if (document.ImageDefinitions != null)
                {
                    collectionNodes.Add(CreateCollectionNode<ImageDefinition>("Image Definitions", document.ImageDefinitions));
                }

                // PDF Definitions
                if (document.PdfDefinitions != null)
                {
                    collectionNodes.Add(CreateCollectionNode<PdfUnderlayDefinition>("PDF Definitions", document.PdfDefinitions));
                }

                // Layouts
                if (document.Layouts != null)
                {
                    collectionNodes.Add(CreateCollectionNode<Layout>("Layouts", document.Layouts));
                }

                // MLeader Styles
                if (document.MLeaderStyles != null)
                {
                    collectionNodes.Add(CreateCollectionNode<MultiLeaderStyle>("MLeader Styles", document.MLeaderStyles));
                }

                // MLine Styles
                if (document.MLineStyles != null)
                {
                    collectionNodes.Add(CreateCollectionNode<MLineStyle>("MLine Styles", document.MLineStyles));
                }

                // Scales
                if (document.Scales != null)
                {
                    collectionNodes.Add(CreateCollectionNode<Scale>("Scales", document.Scales));
                }

                foreach (var node in collectionNodes)
                {
                    collectionsNode.Children.Add(node);
                }
                documentNode.Children.Add(collectionsNode);

                // Objects (Root Dictionary)
                if (document.RootDictionary != null)
                {
                    var objectsNode = CreateDictionaryNode(document.RootDictionary, "Objects (Root Dictionary)");
                    documentNode.Children.Add(objectsNode);
                }

                // Model Space Entities
                if (document.ModelSpace != null && document.ModelSpace.Entities != null)
                {
                    var modelSpaceNode = new CadObjectTreeNode
                    {
                        Name = "Model Space Entities",
                        ObjectType = "Model Space",
                        HasChildren = document.ModelSpace.Entities.Any(),
                        IsExpanded = true
                    };

                    var modelSpaceEntityNodes = document.ModelSpace.Entities.Select(entity => new CadObjectTreeNode
                    {
                        Name = $"{entity.GetType().Name} ({entity.Handle:X})",
                        CadObject = entity,
                        ObjectType = entity.GetType().Name,
                        Handle = entity.Handle,
                        HasChildren = false
                    });

                    foreach (var node in modelSpaceEntityNodes)
                    {
                        modelSpaceNode.Children.Add(node);
                    }
                    documentNode.Children.Add(modelSpaceNode);
                }

                // Paper Space Entities
                if (document.PaperSpace != null && document.PaperSpace.Entities != null)
                {
                    var paperSpaceNode = new CadObjectTreeNode
                    {
                        Name = "Paper Space Entities",
                        ObjectType = "Paper Space",
                        HasChildren = document.PaperSpace.Entities.Any(),
                        IsExpanded = false
                    };

                    var paperSpaceEntityNodes = document.PaperSpace.Entities.Select(entity => new CadObjectTreeNode
                    {
                        Name = $"{entity.GetType().Name} ({entity.Handle:X})",
                        CadObject = entity,
                        ObjectType = entity.GetType().Name,
                        Handle = entity.Handle,
                        HasChildren = false
                    });

                    foreach (var node in paperSpaceEntityNodes)
                    {
                        paperSpaceNode.Children.Add(node);
                    }
                    documentNode.Children.Add(paperSpaceNode);
                }

                rootNodes.Add(documentNode);
                return rootNodes;
            });
        }

        /// <summary>
        /// Searches for objects in the tree by various criteria
        /// </summary>
        /// <param name="document">The CAD document</param>
        /// <param name="searchCriteria">Search criteria</param>
        /// <returns>Matching objects</returns>
        public async Task<IEnumerable<CadObject>> SearchObjectsAsync(CadDocument document, SearchCriteria searchCriteria)
        {
            return await Task.Run(() =>
            {
                var results = new List<CadObject>();

                // Search in all collections
                SearchInCollection<Layer>(document.Layers, searchCriteria, results);
                SearchInCollection<BlockRecord>(document.BlockRecords, searchCriteria, results);
                SearchInCollection<TextStyle>(document.TextStyles, searchCriteria, results);
                SearchInCollection<LineType>(document.LineTypes, searchCriteria, results);
                SearchInCollection<DimensionStyle>(document.DimensionStyles, searchCriteria, results);
                SearchInCollection<View>(document.Views, searchCriteria, results);
                SearchInCollection<VPort>(document.VPorts, searchCriteria, results);
                SearchInCollection<UCS>(document.UCSs, searchCriteria, results);
                SearchInCollection<AppId>(document.AppIds, searchCriteria, results);

                // Search in model space and paper space entities
                if (document.ModelSpace?.Entities != null)
                {
                    SearchInCollection<Entity>(document.ModelSpace.Entities, searchCriteria, results);
                }
                if (document.PaperSpace?.Entities != null)
                {
                    SearchInCollection<Entity>(document.PaperSpace.Entities, searchCriteria, results);
                }

                // Search in block entities
                if (document.BlockRecords != null)
                {
                    foreach (var blockRecord in document.BlockRecords)
                    {
                        if (blockRecord.Entities != null)
                        {
                            SearchInCollection<Entity>(blockRecord.Entities, searchCriteria, results);
                        }
                    }
                }

                // Search in additional collections
                SearchInCollection<Group>(document.Groups, searchCriteria, results);
                SearchInCollection<BookColor>(document.Colors, searchCriteria, results);
                SearchInCollection<ImageDefinition>(document.ImageDefinitions, searchCriteria, results);
                SearchInCollection<PdfUnderlayDefinition>(document.PdfDefinitions, searchCriteria, results);
                SearchInCollection<Layout>(document.Layouts, searchCriteria, results);
                SearchInCollection<MultiLeaderStyle>(document.MLeaderStyles, searchCriteria, results);
                SearchInCollection<MLineStyle>(document.MLineStyles, searchCriteria, results);
                SearchInCollection<Scale>(document.Scales, searchCriteria, results);

                // Search in classes (DxfClass objects)
                if (document.Classes != null)
                {
                    foreach (var cls in document.Classes)
                    {
                        if (MatchesSearchCriteriaForClass(cls, searchCriteria))
                        {
                            // Note: Classes are not CadObjects, so we can't add them to results
                            // But we can still search through them for display purposes
                        }
                    }
                }

                // Search in root dictionary and its nested dictionaries
                if (document.RootDictionary != null)
                {
                    SearchInDictionary(document.RootDictionary, searchCriteria, results);
                }

                return results;
            });
        }

        /// <summary>
        /// Gets all properties of a CAD object
        /// </summary>
        /// <param name="cadObject">The CAD object</param>
        /// <returns>Object properties</returns>
        public async Task<IEnumerable<ObjectProperty>> GetObjectPropertiesAsync(CadObject cadObject)
        {
            return await Task.Run(() =>
            {
                var properties = new List<ObjectProperty>();

                if (cadObject == null) return properties;

                var type = cadObject.GetType();
                var publicProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

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
                            Type = prop.PropertyType.Name,
                            IsReadOnly = !prop.CanWrite
                        });
                    }
                    catch (Exception)
                    {
                        // Skip properties that can't be read
                        properties.Add(new ObjectProperty
                        {
                            Name = prop.Name,
                            Value = "<Error reading property>",
                            Type = prop.PropertyType.Name,
                            IsReadOnly = true
                        });
                    }
                }

                return properties;
            });
        }

        private CadObjectTreeNode CreateTableNode<T>(string name, IEnumerable<T> collection) where T : CadObject
        {
            var node = new CadObjectTreeNode
            {
                Name = name,
                ObjectType = name,
                HasChildren = collection.Any(),
                IsExpanded = false
            };

            var children = collection.Select(item => 
            {
                var childNode = new CadObjectTreeNode
                {
                    Name = $"{item.GetType().Name} ({item.Handle:X})",
                    CadObject = item,
                    ObjectType = item.GetType().Name,
                    Handle = item.Handle,
                    HasChildren = false
                };

                // Special handling for BlockRecord to show its entities
                if (item is BlockRecord blockRecord && blockRecord.Entities != null && blockRecord.Entities.Any())
                {
                    childNode.HasChildren = true;
                    childNode.Name = $"{blockRecord.Name} ({item.Handle:X})";

                    var entityNodes = blockRecord.Entities.Select(entity => new CadObjectTreeNode
                    {
                        Name = $"{entity.GetType().Name} ({entity.Handle:X})",
                        CadObject = entity,
                        ObjectType = entity.GetType().Name,
                        Handle = entity.Handle,
                        HasChildren = false
                    });

                    foreach (var entityNode in entityNodes)
                    {
                        childNode.Children.Add(entityNode);
                    }
                }

                return childNode;
            });

            foreach (var child in children)
            {
                node.Children.Add(child);
            }
            return node;
        }

        private CadObjectTreeNode CreateCollectionNode<T>(string name, IEnumerable<T> collection) where T : CadObject
        {
            var node = new CadObjectTreeNode
            {
                Name = name,
                ObjectType = name,
                HasChildren = collection.Any(),
                IsExpanded = false
            };

            var children = collection.Select(item => new CadObjectTreeNode
            {
                Name = $"{item.GetType().Name} ({item.Handle:X})",
                CadObject = item,
                ObjectType = item.GetType().Name,
                Handle = item.Handle,
                HasChildren = false
            });

            foreach (var child in children)
            {
                node.Children.Add(child);
            }
            return node;
        }

        private CadObjectTreeNode CreateDictionaryNode(CadDictionary dictionary, string name)
        {
            var node = new CadObjectTreeNode
            {
                Name = name,
                CadObject = dictionary,
                ObjectType = "CadDictionary",
                Handle = dictionary.Handle,
                HasChildren = dictionary.Any(),
                IsExpanded = false
            };

            foreach (var entryName in dictionary.EntryNames)
            {
                if (dictionary.TryGetEntry<NonGraphicalObject>(entryName, out var entry))
                {
                    var entryNode = new CadObjectTreeNode
                    {
                        Name = $"{entryName} ({entry.GetType().Name})",
                        CadObject = entry,
                        ObjectType = entry.GetType().Name,
                        Handle = entry.Handle,
                        HasChildren = entry is CadDictionary nestedDict && nestedDict.Any(),
                        IsExpanded = false
                    };

                    // Recursively add nested dictionaries
                    if (entry is CadDictionary nestedDictionary)
                    {
                        foreach (var nestedEntryName in nestedDictionary.EntryNames)
                        {
                            if (nestedDictionary.TryGetEntry<NonGraphicalObject>(nestedEntryName, out var nestedEntry))
                            {
                                var nestedNode = new CadObjectTreeNode
                                {
                                    Name = $"{nestedEntryName} ({nestedEntry.GetType().Name})",
                                    CadObject = nestedEntry,
                                    ObjectType = nestedEntry.GetType().Name,
                                    Handle = nestedEntry.Handle,
                                    HasChildren = nestedEntry is CadDictionary && ((CadDictionary)nestedEntry).Any(),
                                    IsExpanded = false
                                };
                                entryNode.Children.Add(nestedNode);
                            }
                        }
                    }

                    node.Children.Add(entryNode);
                }
            }

            return node;
        }

        private void SearchInCollection<T>(IEnumerable<T>? collection, SearchCriteria criteria, List<CadObject> results) where T : CadObject
        {
            if (collection == null) return;

            foreach (var item in collection)
            {
                if (MatchesSearchCriteria(item, criteria))
                {
                    results.Add(item);
                }
            }
        }

        private void SearchInDictionary(CadDictionary dictionary, SearchCriteria criteria, List<CadObject> results)
        {
            foreach (var entryName in dictionary.EntryNames)
            {
                if (dictionary.TryGetEntry<NonGraphicalObject>(entryName, out var entry))
                {
                    if (MatchesSearchCriteria(entry, criteria))
                    {
                        results.Add(entry);
                    }

                    // Recursively search nested dictionaries
                    if (entry is CadDictionary nestedDict)
                    {
                        SearchInDictionary(nestedDict, criteria, results);
                    }
                }
            }
        }

        private bool MatchesSearchCriteria(CadObject obj, SearchCriteria criteria)
        {
            if (string.IsNullOrEmpty(criteria.SearchText))
                return false;

            var comparison = criteria.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var searchText = criteria.SearchText;

            switch (criteria.SearchType)
            {
                case SearchType.Handle:
                    return obj.Handle.ToString("X").Contains(searchText, comparison);

                case SearchType.ObjectType:
                    return obj.GetType().Name.Contains(searchText, comparison);

                case SearchType.ObjectData:
                    var type = obj.GetType();
                    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    foreach (var prop in properties)
                    {
                        try
                        {
                            var value = prop.GetValue(obj)?.ToString();
                            if (!string.IsNullOrEmpty(value) && value.Contains(searchText, comparison))
                                return true;
                        }
                        catch
                        {
                            // Skip properties that can't be read
                        }
                    }
                    return false;

                case SearchType.TagCode:
                    // For tag code search, we'll search in the object's basic properties
                    // This is a simplified implementation - you might want to enhance this
                    return obj.Handle.ToString("X").Contains(searchText, comparison) ||
                           obj.GetType().Name.Contains(searchText, comparison);

                case SearchType.All:
                    // Search in handle, type, and properties
                    if (obj.Handle.ToString("X").Contains(searchText, comparison))
                        return true;
                    if (obj.GetType().Name.Contains(searchText, comparison))
                        return true;
                    
                    var allType = obj.GetType();
                    var allProperties = allType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in allProperties)
                    {
                        try
                        {
                            var value = prop.GetValue(obj)?.ToString();
                            if (!string.IsNullOrEmpty(value) && value.Contains(searchText, comparison))
                                return true;
                        }
                        catch
                        {
                            // Skip properties that can't be read
                        }
                    }
                    return false;

                default:
                    return false;
            }
        }

        private bool MatchesSearchCriteriaForClass(DxfClass cls, SearchCriteria criteria)
        {
            if (string.IsNullOrEmpty(criteria.SearchText))
                return false;

            var comparison = criteria.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var searchText = criteria.SearchText;

            switch (criteria.SearchType)
            {
                case SearchType.ObjectType:
                    return cls.GetType().Name.Contains(searchText, comparison) ||
                           cls.DxfName.Contains(searchText, comparison) ||
                           cls.CppClassName.Contains(searchText, comparison);

                case SearchType.ObjectData:
                case SearchType.All:
                    return cls.DxfName.Contains(searchText, comparison) ||
                           cls.CppClassName.Contains(searchText, comparison) ||
                           cls.ApplicationName?.Contains(searchText, comparison) == true;

                case SearchType.Handle:
                case SearchType.TagCode:
                    // Classes don't have handles, so these search types don't apply
                    return false;

                default:
                    return false;
            }
        }
    }
} 