using ACadSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ACadSharp.Viewer.Models;

/// <summary>
/// Supported input file filters for batch conversion
/// </summary>
public enum InputFileFilter
{
    [Description("*.DWG ; *.DXF")]
    DwgAndDxf,
    [Description("*.DWG")]
    DwgOnly,
    [Description("*.DXF")]
    DxfOnly
}

/// <summary>
/// Supported output file formats and versions
/// </summary>
public enum OutputFormat
{
    // DWG Formats
    [Description("2018 DWG")]
    Dwg2018,
    [Description("2013 DWG")]
    Dwg2013,
    [Description("2010 DWG")]
    Dwg2010,
    [Description("2007 DWG")]
    Dwg2007,
    [Description("2004 DWG")]
    Dwg2004,
    [Description("2000 DWG")]
    Dwg2000,
    [Description("R14 DWG")]
    DwgR14,
    [Description("R13 DWG")]
    DwgR13,
    [Description("R12 DWG")]
    DwgR12,
    
    // ASCII DXF Formats
    [Description("2018 ASCII DXF")]
    DxfAscii2018,
    [Description("2013 ASCII DXF")]
    DxfAscii2013,
    [Description("2010 ASCII DXF")]
    DxfAscii2010,
    [Description("2007 ASCII DXF")]
    DxfAscii2007,
    [Description("2004 ASCII DXF")]
    DxfAscii2004,
    [Description("2000 ASCII DXF")]
    DxfAscii2000,
    [Description("R14 ASCII DXF")]
    DxfAsciiR14,
    [Description("R13 ASCII DXF")]
    DxfAsciiR13,
    [Description("R12 ASCII DXF")]
    DxfAsciiR12,
    [Description("R10 ASCII DXF")]
    DxfAsciiR10,
    [Description("R9 ASCII DXF")]
    DxfAsciiR9,
    
    // Binary DXF Formats
    [Description("2018 Binary DXF")]
    DxfBinary2018,
    [Description("2013 Binary DXF")]
    DxfBinary2013,
    [Description("2010 Binary DXF")]
    DxfBinary2010,
    [Description("2007 Binary DXF")]
    DxfBinary2007,
    [Description("2004 Binary DXF")]
    DxfBinary2004,
    [Description("2000 Binary DXF")]
    DxfBinary2000,
    [Description("R14 Binary DXF")]
    DxfBinaryR14,
    [Description("R13 Binary DXF")]
    DxfBinaryR13,
    [Description("R12 Binary DXF")]
    DxfBinaryR12,
    [Description("R10 Binary DXF")]
    DxfBinaryR10
}

/// <summary>
/// Configuration for batch file conversion
/// </summary>
public class BatchConverterConfiguration
{
    /// <summary>
    /// Input folder path
    /// </summary>
    public string InputFolder { get; set; } = string.Empty;
    
    /// <summary>
    /// Output folder path
    /// </summary>
    public string OutputFolder { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to recurse through subfolders
    /// </summary>
    public bool RecurseFolders { get; set; } = false;
    
    /// <summary>
    /// Whether to audit files during conversion
    /// </summary>
    public bool Audit { get; set; } = true;
    
    /// <summary>
    /// Input file filter
    /// </summary>
    public InputFileFilter InputFileFilter { get; set; } = InputFileFilter.DwgAndDxf;
    
    /// <summary>
    /// Output format and version
    /// </summary>
    public OutputFormat OutputFormat { get; set; } = OutputFormat.DxfAscii2000;
}

/// <summary>
/// Persistent settings for batch converter
/// </summary>
public class BatchConverterSettings
{
    /// <summary>
    /// Last used input folder path
    /// </summary>
    public string LastInputFolder { get; set; } = string.Empty;
    
    /// <summary>
    /// Last used output folder path  
    /// </summary>
    public string LastOutputFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    
    /// <summary>
    /// Default setting for recursive folder processing
    /// </summary>
    public bool DefaultRecurseFolders { get; set; } = true;
    
    /// <summary>
    /// Default setting for audit during conversion
    /// </summary>
    public bool DefaultAudit { get; set; } = true;
    
    /// <summary>
    /// Default input file filter
    /// </summary>
    public InputFileFilter DefaultInputFileFilter { get; set; } = InputFileFilter.DwgAndDxf;
    
    /// <summary>
    /// Default output format
    /// </summary>
    public OutputFormat DefaultOutputFormat { get; set; } = OutputFormat.DxfAscii2000;
    
    /// <summary>
    /// Window width
    /// </summary>
    public double WindowWidth { get; set; } = 600;
    
    /// <summary>
    /// Window height
    /// </summary>
    public double WindowHeight { get; set; } = 500;
}

/// <summary>
/// Progress information for batch conversion
/// </summary>
public class BatchConversionProgress
{
    /// <summary>
    /// Overall progress percentage (0-100)
    /// </summary>
    public int OverallProgress { get; set; }
    
    /// <summary>
    /// Current file being processed
    /// </summary>
    public string CurrentFile { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status message
    /// </summary>
    public string StatusMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of files processed
    /// </summary>
    public int FilesProcessed { get; set; }
    
    /// <summary>
    /// Total number of files to process
    /// </summary>
    public int TotalFiles { get; set; }
    
    /// <summary>
    /// Number of successful conversions
    /// </summary>
    public int SuccessfulConversions { get; set; }
    
    /// <summary>
    /// Number of failed conversions
    /// </summary>
    public int FailedConversions { get; set; }
    
    /// <summary>
    /// List of conversion errors
    /// </summary>
    public List<string> Errors { get; set; } = new List<string>();
}

/// <summary>
/// Result of a batch conversion operation
/// </summary>
public class BatchConversionResult
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Total number of files processed
    /// </summary>
    public int TotalFiles { get; set; }
    
    /// <summary>
    /// Number of successful conversions
    /// </summary>
    public int SuccessfulConversions { get; set; }
    
    /// <summary>
    /// Number of failed conversions
    /// </summary>
    public int FailedConversions { get; set; }
    
    /// <summary>
    /// List of errors encountered
    /// </summary>
    public List<string> Errors { get; set; } = new List<string>();
    
    /// <summary>
    /// Total time taken for conversion
    /// </summary>
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Extension methods for enums
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Gets the description attribute value for an enum
    /// </summary>
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field == null) return value.ToString();
        
        var attribute = (DescriptionAttribute?)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
        return attribute?.Description ?? value.ToString();
    }
    
    /// <summary>
    /// Gets all enum values with their descriptions
    /// </summary>
    public static Dictionary<T, string> GetDescriptions<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T))
            .Cast<T>()
            .ToDictionary(value => value, value => value.GetDescription());
    }
}

/// <summary>
/// Utility class for mapping output formats to ACad versions and file types
/// </summary>
public static class OutputFormatHelper
{
    /// <summary>
    /// Maps output format to ACad version
    /// </summary>
    public static ACadVersion GetACadVersion(OutputFormat format)
    {
        return format switch
        {
            OutputFormat.Dwg2018 or OutputFormat.DxfAscii2018 or OutputFormat.DxfBinary2018 => ACadVersion.AC1032,
            OutputFormat.Dwg2013 or OutputFormat.DxfAscii2013 or OutputFormat.DxfBinary2013 => ACadVersion.AC1027,
            OutputFormat.Dwg2010 or OutputFormat.DxfAscii2010 or OutputFormat.DxfBinary2010 => ACadVersion.AC1024,
            OutputFormat.Dwg2007 or OutputFormat.DxfAscii2007 or OutputFormat.DxfBinary2007 => ACadVersion.AC1021,
            OutputFormat.Dwg2004 or OutputFormat.DxfAscii2004 or OutputFormat.DxfBinary2004 => ACadVersion.AC1018,
            OutputFormat.Dwg2000 or OutputFormat.DxfAscii2000 or OutputFormat.DxfBinary2000 => ACadVersion.AC1015,
            OutputFormat.DwgR14 or OutputFormat.DxfAsciiR14 or OutputFormat.DxfBinaryR14 => ACadVersion.AC1014,
            OutputFormat.DwgR13 or OutputFormat.DxfAsciiR13 or OutputFormat.DxfBinaryR13 => ACadVersion.AC1012,
            OutputFormat.DwgR12 or OutputFormat.DxfAsciiR12 or OutputFormat.DxfBinaryR12 => ACadVersion.AC1009,
            OutputFormat.DxfAsciiR10 or OutputFormat.DxfBinaryR10 => ACadVersion.AC1006,
            OutputFormat.DxfAsciiR9 => ACadVersion.AC1004,
            _ => ACadVersion.AC1015
        };
    }
    
    /// <summary>
    /// Checks if output format is DWG
    /// </summary>
    public static bool IsDwgFormat(OutputFormat format)
    {
        return format switch
        {
            OutputFormat.Dwg2018 or
            OutputFormat.Dwg2013 or
            OutputFormat.Dwg2010 or
            OutputFormat.Dwg2007 or
            OutputFormat.Dwg2004 or
            OutputFormat.Dwg2000 or
            OutputFormat.DwgR14 or
            OutputFormat.DwgR13 or
            OutputFormat.DwgR12 => true,
            _ => false
        };
    }
    
    /// <summary>
    /// Checks if output format is binary DXF
    /// </summary>
    public static bool IsBinaryDxf(OutputFormat format)
    {
        return format switch
        {
            OutputFormat.DxfBinary2018 or
            OutputFormat.DxfBinary2013 or
            OutputFormat.DxfBinary2010 or
            OutputFormat.DxfBinary2007 or
            OutputFormat.DxfBinary2004 or
            OutputFormat.DxfBinary2000 or
            OutputFormat.DxfBinaryR14 or
            OutputFormat.DxfBinaryR13 or
            OutputFormat.DxfBinaryR12 or
            OutputFormat.DxfBinaryR10 => true,
            _ => false
        };
    }
    
    /// <summary>
    /// Gets the file extension for the output format
    /// </summary>
    public static string GetFileExtension(OutputFormat format)
    {
        return IsDwgFormat(format) ? ".dwg" : ".dxf";
    }
}