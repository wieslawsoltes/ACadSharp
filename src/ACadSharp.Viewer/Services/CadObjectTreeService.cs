using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Objects;
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

                // Objects
                if (document.RootDictionary != null)
                {
                    var objectsNode = new CadObjectTreeNode
                    {
                        Name = "Objects",
                        CadObject = document.RootDictionary,
                        ObjectType = "Root Dictionary",
                        Handle = document.RootDictionary.Handle,
                        HasChildren = true,
                        IsExpanded = true
                    };

                    documentNode.Children.Add(objectsNode);
                }

                // Entities
                if (document.Entities != null)
                {
                    var entitiesNode = new CadObjectTreeNode
                    {
                        Name = "Entities",
                        ObjectType = "Entities",
                        HasChildren = true,
                        IsExpanded = true
                    };

                    var entityNodes = document.Entities.Select(entity => new CadObjectTreeNode
                    {
                        Name = $"{entity.GetType().Name} ({entity.Handle:X})",
                        CadObject = entity,
                        ObjectType = entity.GetType().Name,
                        Handle = entity.Handle,
                        HasChildren = false
                    });

                    foreach (var node in entityNodes)
                    {
                        entitiesNode.Children.Add(node);
                    }
                    documentNode.Children.Add(entitiesNode);
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
                SearchInCollection<Entity>(document.Entities, searchCriteria, results);
                SearchInCollection<Layer>(document.Layers, searchCriteria, results);
                SearchInCollection<BlockRecord>(document.BlockRecords, searchCriteria, results);
                SearchInCollection<TextStyle>(document.TextStyles, searchCriteria, results);
                SearchInCollection<LineType>(document.LineTypes, searchCriteria, results);
                SearchInCollection<DimensionStyle>(document.DimensionStyles, searchCriteria, results);
                SearchInCollection<View>(document.Views, searchCriteria, results);
                SearchInCollection<VPort>(document.VPorts, searchCriteria, results);
                SearchInCollection<UCS>(document.UCSs, searchCriteria, results);
                SearchInCollection<AppId>(document.AppIds, searchCriteria, results);

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

        private bool MatchesSearchCriteria(CadObject obj, SearchCriteria criteria)
        {
            var comparison = criteria.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            // Search by handle
            if (!string.IsNullOrEmpty(criteria.ObjectHandle))
            {
                if (obj.Handle.ToString("X").Contains(criteria.ObjectHandle, comparison))
                    return true;
            }

            // Search by object type
            if (!string.IsNullOrEmpty(criteria.ObjectType))
            {
                if (obj.GetType().Name.Contains(criteria.ObjectType, comparison))
                    return true;
            }

            // Search by object data (basic properties)
            if (!string.IsNullOrEmpty(criteria.ObjectData))
            {
                var type = obj.GetType();
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var prop in properties)
                {
                    try
                    {
                        var value = prop.GetValue(obj)?.ToString();
                        if (!string.IsNullOrEmpty(value) && value.Contains(criteria.ObjectData, comparison))
                            return true;
                    }
                    catch
                    {
                        // Skip properties that can't be read
                    }
                }
            }

            return false;
        }
    }
} 