using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ACadSharp.Viewer.Converters;

/// <summary>
/// Converters for type checking and conversion
/// </summary>
public static class TypeConverters
{
    /// <summary>
    /// Converter that checks if a type is numeric
    /// </summary>
    public static readonly IValueConverter IsNumericType = new FuncValueConverter<Type?, bool>(type =>
    {
        if (type == null) return false;
        
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        
        return underlyingType == typeof(int) ||
               underlyingType == typeof(long) ||
               underlyingType == typeof(short) ||
               underlyingType == typeof(byte) ||
               underlyingType == typeof(double) ||
               underlyingType == typeof(float) ||
               underlyingType == typeof(decimal);
    });

    /// <summary>
    /// Converter that checks if a type is string
    /// </summary>
    public static readonly IValueConverter IsStringType = new FuncValueConverter<Type?, bool>(type =>
    {
        if (type == null) return false;
        
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        
        return underlyingType == typeof(string);
    });

    /// <summary>
    /// Converter that checks if a type is boolean
    /// </summary>
    public static readonly IValueConverter IsBooleanType = new FuncValueConverter<Type?, bool>(type =>
    {
        if (type == null) return false;
        
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        
        return underlyingType == typeof(bool);
    });

    /// <summary>
    /// Converter that checks if a type is enum
    /// </summary>
    public static readonly IValueConverter IsEnumType = new FuncValueConverter<Type?, bool>(type =>
    {
        if (type == null) return false;
        
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        
        return underlyingType.IsEnum;
    });
}