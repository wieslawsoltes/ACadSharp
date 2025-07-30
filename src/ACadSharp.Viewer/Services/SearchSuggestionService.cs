using ACadSharp;
using ACadSharp.Viewer.Interfaces;
using ACadSharp.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ACadSharp.Viewer.Services;

/// <summary>
/// Service for providing search suggestions based on loaded CAD documents
/// </summary>
public class SearchSuggestionService
{
    private readonly HashSet<string> _handles = new();
    private readonly HashSet<string> _objectTypes = new();
    private readonly HashSet<string> _objectData = new();
    private readonly HashSet<string> _tagCodes = new();
    private readonly HashSet<string> _propertyNames = new();
    private readonly HashSet<string> _propertyTypes = new();

    /// <summary>
    /// Updates suggestions based on the provided documents
    /// </summary>
    /// <param name="documents">CAD documents to extract suggestions from</param>
    public async Task UpdateSuggestionsAsync(IEnumerable<CadDocument> documents)
    {
        await Task.Run(() =>
        {
            _handles.Clear();
            _objectTypes.Clear();
            _objectData.Clear();
            _tagCodes.Clear();
            _propertyNames.Clear();
            _propertyTypes.Clear();

            foreach (var document in documents)
            {
                if (document == null) continue;

                ExtractSuggestionsFromDocument(document);
            }
        });
    }

    /// <summary>
    /// Gets suggestions for a specific search type
    /// </summary>
    /// <param name="searchType">The search type</param>
    /// <param name="filter">Optional filter text</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <returns>List of suggestions</returns>
    public IEnumerable<string> GetSuggestions(SearchType searchType, string? filter = null, int maxResults = 50)
    {
        var suggestions = searchType switch
        {
            SearchType.Handle => _handles,
            SearchType.ObjectType => _objectTypes,
            SearchType.ObjectData => _objectData,
            SearchType.TagCode => _tagCodes,
            SearchType.PropertyName => _propertyNames,
            SearchType.PropertyType => _propertyTypes,
            SearchType.All => _handles.Concat(_objectTypes).Concat(_objectData).Concat(_tagCodes).Concat(_propertyNames).Concat(_propertyTypes).Distinct(),
            _ => Enumerable.Empty<string>()
        };

        if (!string.IsNullOrWhiteSpace(filter))
        {
            suggestions = suggestions.Where(s => s.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        return suggestions.Take(maxResults).OrderBy(s => s);
    }

    /// <summary>
    /// Extracts suggestions from a single document
    /// </summary>
    /// <param name="document">The CAD document</param>
    private void ExtractSuggestionsFromDocument(CadDocument document)
    {
        // Extract handles
        ExtractHandlesFromDocument(document);

        // Extract object types
        ExtractObjectTypesFromDocument(document);

        // Extract object data
        ExtractObjectDataFromDocument(document);

        // Extract tag codes (using handles as a proxy for now)
        ExtractTagCodesFromDocument(document);

        // Extract property names and types
        ExtractPropertyNamesAndTypesFromDocument(document);
    }

    /// <summary>
    /// Extracts handle suggestions from document objects
    /// </summary>
    /// <param name="document">The CAD document</param>
    private void ExtractHandlesFromDocument(CadDocument document)
    {
        // Add handles from all collections
        AddHandlesFromCollection(document.Layers);
        AddHandlesFromCollection(document.BlockRecords);
        AddHandlesFromCollection(document.TextStyles);
        AddHandlesFromCollection(document.LineTypes);
        AddHandlesFromCollection(document.DimensionStyles);
        AddHandlesFromCollection(document.Views);
        AddHandlesFromCollection(document.UCSs);
        AddHandlesFromCollection(document.VPorts);
        AddHandlesFromCollection(document.AppIds);

        // Add handles from entities
        if (document.ModelSpace?.Entities != null)
        {
            foreach (var entity in document.ModelSpace.Entities)
            {
                _handles.Add(entity.Handle.ToString("X"));
            }
        }

        if (document.PaperSpace?.Entities != null)
        {
            foreach (var entity in document.PaperSpace.Entities)
            {
                _handles.Add(entity.Handle.ToString("X"));
            }
        }

        // Add handles from dictionaries
        ExtractHandlesFromDictionary(document.RootDictionary);
    }

    /// <summary>
    /// Extracts object type suggestions from document objects
    /// </summary>
    /// <param name="document">The CAD document</param>
    private void ExtractObjectTypesFromDocument(CadDocument document)
    {
        // Add types from all collections
        AddTypesFromCollection(document.Layers);
        AddTypesFromCollection(document.BlockRecords);
        AddTypesFromCollection(document.TextStyles);
        AddTypesFromCollection(document.LineTypes);
        AddTypesFromCollection(document.DimensionStyles);
        AddTypesFromCollection(document.Views);
        AddTypesFromCollection(document.UCSs);
        AddTypesFromCollection(document.VPorts);
        AddTypesFromCollection(document.AppIds);

        // Add types from entities
        if (document.ModelSpace?.Entities != null)
        {
            foreach (var entity in document.ModelSpace.Entities)
            {
                _objectTypes.Add(entity.GetType().Name);
            }
        }

        if (document.PaperSpace?.Entities != null)
        {
            foreach (var entity in document.PaperSpace.Entities)
            {
                _objectTypes.Add(entity.GetType().Name);
            }
        }

        // Add types from dictionaries
        ExtractTypesFromDictionary(document.RootDictionary);
    }

    /// <summary>
    /// Extracts object data suggestions from document objects
    /// </summary>
    /// <param name="document">The CAD document</param>
    private void ExtractObjectDataFromDocument(CadDocument document)
    {
        // Add data from all collections
        AddDataFromCollection(document.Layers);
        AddDataFromCollection(document.BlockRecords);
        AddDataFromCollection(document.TextStyles);
        AddDataFromCollection(document.LineTypes);
        AddDataFromCollection(document.DimensionStyles);
        AddDataFromCollection(document.Views);
        AddDataFromCollection(document.UCSs);
        AddDataFromCollection(document.VPorts);
        AddDataFromCollection(document.AppIds);

        // Add data from entities
        if (document.ModelSpace?.Entities != null)
        {
            foreach (var entity in document.ModelSpace.Entities)
            {
                AddDataFromObject(entity);
            }
        }

        if (document.PaperSpace?.Entities != null)
        {
            foreach (var entity in document.PaperSpace.Entities)
            {
                AddDataFromObject(entity);
            }
        }

        // Add data from dictionaries
        ExtractDataFromDictionary(document.RootDictionary);
    }

    /// <summary>
    /// Extracts tag code suggestions from document objects
    /// </summary>
    /// <param name="document">The CAD document</param>
    private void ExtractTagCodesFromDocument(CadDocument document)
    {
        // For now, use handles as tag codes since they are unique identifiers
        // In a real implementation, you might want to extract actual DXF tag codes
        foreach (var handle in _handles)
        {
            _tagCodes.Add(handle);
        }
    }

    /// <summary>
    /// Adds handles from a collection of CAD objects
    /// </summary>
    /// <typeparam name="T">Type of CAD object</typeparam>
    /// <param name="collection">Collection of objects</param>
    private void AddHandlesFromCollection<T>(IEnumerable<T>? collection) where T : CadObject
    {
        if (collection == null) return;

        foreach (var item in collection)
        {
            _handles.Add(item.Handle.ToString("X"));
        }
    }

    /// <summary>
    /// Adds types from a collection of CAD objects
    /// </summary>
    /// <typeparam name="T">Type of CAD object</typeparam>
    /// <param name="collection">Collection of objects</param>
    private void AddTypesFromCollection<T>(IEnumerable<T>? collection) where T : CadObject
    {
        if (collection == null) return;

        foreach (var item in collection)
        {
            _objectTypes.Add(item.GetType().Name);
        }
    }

    /// <summary>
    /// Adds data from a collection of CAD objects
    /// </summary>
    /// <typeparam name="T">Type of CAD object</typeparam>
    /// <param name="collection">Collection of objects</param>
    private void AddDataFromCollection<T>(IEnumerable<T>? collection) where T : CadObject
    {
        if (collection == null) return;

        foreach (var item in collection)
        {
            AddDataFromObject(item);
        }
    }

    /// <summary>
    /// Adds data from a single CAD object
    /// </summary>
    /// <param name="obj">The CAD object</param>
    private void AddDataFromObject(CadObject obj)
    {
        try
        {
            var type = obj.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(obj)?.ToString();
                    if (!string.IsNullOrEmpty(value) && value.Length > 0 && value.Length < 100)
                    {
                        _objectData.Add(value);
                    }
                }
                catch
                {
                    // Skip properties that can't be read
                }
            }
        }
        catch
        {
            // Skip objects that can't be processed
        }
    }

    /// <summary>
    /// Extracts handles from a dictionary recursively
    /// </summary>
    /// <param name="dictionary">The dictionary to process</param>
    private void ExtractHandlesFromDictionary(CadDictionary? dictionary)
    {
        if (dictionary == null) return;

        foreach (var entryName in dictionary.EntryNames)
        {
            if (dictionary.TryGetEntry<NonGraphicalObject>(entryName, out var entry))
            {
                _handles.Add(entry.Handle.ToString("X"));

                // Recursively process nested dictionaries
                if (entry is CadDictionary nestedDict)
                {
                    ExtractHandlesFromDictionary(nestedDict);
                }
            }
        }
    }

    /// <summary>
    /// Extracts types from a dictionary recursively
    /// </summary>
    /// <param name="dictionary">The dictionary to process</param>
    private void ExtractTypesFromDictionary(CadDictionary? dictionary)
    {
        if (dictionary == null) return;

        foreach (var entryName in dictionary.EntryNames)
        {
            if (dictionary.TryGetEntry<NonGraphicalObject>(entryName, out var entry))
            {
                _objectTypes.Add(entry.GetType().Name);

                // Recursively process nested dictionaries
                if (entry is CadDictionary nestedDict)
                {
                    ExtractTypesFromDictionary(nestedDict);
                }
            }
        }
    }

    /// <summary>
    /// Extracts data from a dictionary recursively
    /// </summary>
    /// <param name="dictionary">The dictionary to process</param>
    private void ExtractDataFromDictionary(CadDictionary? dictionary)
    {
        if (dictionary == null) return;

        foreach (var entryName in dictionary.EntryNames)
        {
            if (dictionary.TryGetEntry<NonGraphicalObject>(entryName, out var entry))
            {
                AddDataFromObject(entry);

                // Recursively process nested dictionaries
                if (entry is CadDictionary nestedDict)
                {
                    ExtractDataFromDictionary(nestedDict);
                }
            }
        }
    }

    /// <summary>
    /// Extracts property names and types from document objects
    /// </summary>
    /// <param name="document">The CAD document</param>
    private void ExtractPropertyNamesAndTypesFromDocument(CadDocument document)
    {
        // Extract from all collections
        AddPropertyNamesAndTypesFromCollection(document.Layers);
        AddPropertyNamesAndTypesFromCollection(document.BlockRecords);
        AddPropertyNamesAndTypesFromCollection(document.TextStyles);
        AddPropertyNamesAndTypesFromCollection(document.LineTypes);
        AddPropertyNamesAndTypesFromCollection(document.DimensionStyles);
        AddPropertyNamesAndTypesFromCollection(document.Views);
        AddPropertyNamesAndTypesFromCollection(document.UCSs);
        AddPropertyNamesAndTypesFromCollection(document.VPorts);
        AddPropertyNamesAndTypesFromCollection(document.AppIds);

        // Extract from entities
        if (document.ModelSpace?.Entities != null)
        {
            foreach (var entity in document.ModelSpace.Entities)
            {
                AddPropertyNamesAndTypesFromObject(entity);
            }
        }

        if (document.PaperSpace?.Entities != null)
        {
            foreach (var entity in document.PaperSpace.Entities)
            {
                AddPropertyNamesAndTypesFromObject(entity);
            }
        }

        // Extract from dictionaries
        ExtractPropertyNamesAndTypesFromDictionary(document.RootDictionary);
    }

    /// <summary>
    /// Adds property names and types from a collection of CAD objects
    /// </summary>
    /// <typeparam name="T">Type of CAD object</typeparam>
    /// <param name="collection">Collection of objects</param>
    private void AddPropertyNamesAndTypesFromCollection<T>(IEnumerable<T>? collection) where T : CadObject
    {
        if (collection == null) return;

        foreach (var item in collection)
        {
            AddPropertyNamesAndTypesFromObject(item);
        }
    }

    /// <summary>
    /// Adds property names and types from a single CAD object
    /// </summary>
    /// <param name="obj">The CAD object</param>
    private void AddPropertyNamesAndTypesFromObject(CadObject obj)
    {
        try
        {
            var type = obj.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                try
                {
                    // Add property name
                    _propertyNames.Add(prop.Name);

                    // Add property type (simple name for readability)
                    var propertyType = prop.PropertyType;
                    var typeName = propertyType.IsGenericType 
                        ? $"{propertyType.Name.Split('`')[0]}<{string.Join(", ", propertyType.GetGenericArguments().Select(t => t.Name))}>"
                        : propertyType.Name;
                    _propertyTypes.Add(typeName);
                }
                catch
                {
                    // Skip properties that can't be processed
                }
            }
        }
        catch
        {
            // Skip objects that can't be processed
        }
    }

    /// <summary>
    /// Extracts property names and types from a dictionary recursively
    /// </summary>
    /// <param name="dictionary">The dictionary to process</param>
    private void ExtractPropertyNamesAndTypesFromDictionary(CadDictionary? dictionary)
    {
        if (dictionary == null) return;

        foreach (var entryName in dictionary.EntryNames)
        {
            if (dictionary.TryGetEntry<NonGraphicalObject>(entryName, out var entry))
            {
                AddPropertyNamesAndTypesFromObject(entry);

                // Recursively process nested dictionaries
                if (entry is CadDictionary nestedDict)
                {
                    ExtractPropertyNamesAndTypesFromDictionary(nestedDict);
                }
            }
        }
    }
} 