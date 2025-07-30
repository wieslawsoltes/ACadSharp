using ACadSharp;
using System.Collections.Generic;
using System.Linq;

namespace ACadSharp.Viewer.Models;

/// <summary>
/// Utility class for managing supported CAD versions
/// </summary>
public static class SupportedVersions
{
    /// <summary>
    /// Gets all supported DWG versions for writing
    /// </summary>
    public static readonly List<VersionInfo> SupportedDwgVersions = new()
    {
        new VersionInfo(ACadVersion.AC1012, "R13 - AutoCAD Release 13"),
        new VersionInfo(ACadVersion.AC1014, "R14 - AutoCAD Release 14"),
        new VersionInfo(ACadVersion.AC1015, "2000 - AutoCAD 2000/2000i/2002"),
        new VersionInfo(ACadVersion.AC1018, "2004 - AutoCAD 2004/2005/2006"),
        new VersionInfo(ACadVersion.AC1021, "2007 - AutoCAD 2007/2008/2009"),
        new VersionInfo(ACadVersion.AC1024, "2010 - AutoCAD 2010/2011/2012"),
        new VersionInfo(ACadVersion.AC1027, "2013 - AutoCAD 2013/2014/2015/2016/2017"),
        new VersionInfo(ACadVersion.AC1032, "2018 - AutoCAD 2018/2019/2020+")
    };

    /// <summary>
    /// Gets all supported DXF versions for writing
    /// </summary>
    public static readonly List<VersionInfo> SupportedDxfVersions = new()
    {
        new VersionInfo(ACadVersion.AC1012, "R13 - AutoCAD Release 13"),
        new VersionInfo(ACadVersion.AC1014, "R14 - AutoCAD Release 14"),
        new VersionInfo(ACadVersion.AC1015, "2000 - AutoCAD 2000/2000i/2002"),
        new VersionInfo(ACadVersion.AC1018, "2004 - AutoCAD 2004/2005/2006"),
        new VersionInfo(ACadVersion.AC1021, "2007 - AutoCAD 2007/2008/2009"),
        new VersionInfo(ACadVersion.AC1024, "2010 - AutoCAD 2010/2011/2012"),
        new VersionInfo(ACadVersion.AC1027, "2013 - AutoCAD 2013/2014/2015/2016/2017"),
        new VersionInfo(ACadVersion.AC1032, "2018 - AutoCAD 2018/2019/2020+")
    };

    /// <summary>
    /// Gets the default version info for the given document version
    /// </summary>
    /// <param name="documentVersion">The document's current version</param>
    /// <param name="isDwg">True for DWG, false for DXF</param>
    /// <returns>Default version info, or latest supported version if not found</returns>
    public static VersionInfo GetDefaultVersion(ACadVersion documentVersion, bool isDwg)
    {
        var supportedVersions = isDwg ? SupportedDwgVersions : SupportedDxfVersions;
        
        // First try to find exact match
        var exactMatch = supportedVersions.FirstOrDefault(v => v.Version == documentVersion);
        if (exactMatch != null)
            return exactMatch;

        // If not supported, find the closest supported version (prefer newer)
        var supportedVersion = supportedVersions.LastOrDefault(v => v.Version <= documentVersion);
        
        // If no suitable version found, use the latest
        return supportedVersion ?? supportedVersions.Last();
    }
}

/// <summary>
/// Information about a CAD version
/// </summary>
public class VersionInfo
{
    public ACadVersion Version { get; }
    public string DisplayName { get; }

    public VersionInfo(ACadVersion version, string displayName)
    {
        Version = version;
        DisplayName = displayName;
    }

    public override string ToString() => DisplayName;
}