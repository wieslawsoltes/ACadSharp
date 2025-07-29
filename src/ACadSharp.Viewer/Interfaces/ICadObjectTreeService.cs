using ACadSharp;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ACadSharp.Viewer.Interfaces
{
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
    public class CadObjectTreeNode
    {
        public string Name { get; set; } = string.Empty;
        public CadObject? CadObject { get; set; }
        public string ObjectType { get; set; } = string.Empty;
        public ulong? Handle { get; set; }
        public bool IsExpanded { get; set; }
        public bool HasChildren { get; set; }
        public IEnumerable<CadObjectTreeNode> Children { get; set; } = new List<CadObjectTreeNode>();
    }

    /// <summary>
    /// Search criteria for finding CAD objects
    /// </summary>
    public class SearchCriteria
    {
        public string? TagCode { get; set; }
        public string? ObjectHandle { get; set; }
        public string? ObjectData { get; set; }
        public string? ObjectType { get; set; }
        public bool CaseSensitive { get; set; }
    }

    /// <summary>
    /// Represents a property of a CAD object
    /// </summary>
    public class ObjectProperty
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsReadOnly { get; set; }
    }
} 