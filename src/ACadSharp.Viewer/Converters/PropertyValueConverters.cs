using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ACadSharp.Viewer.Converters;

/// <summary>
/// Converter for boolean property values
/// </summary>
public class BooleanValueConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return false;
        
        if (value is bool boolValue)
            return boolValue;
            
        if (value is string stringValue)
        {
            return bool.TryParse(stringValue, out var result) ? result : false;
        }
        
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return boolValue.ToString();
            
        return "false";
    }
}

/// <summary>
/// Converter for enum property values
/// </summary>
public class EnumValueConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return null;
        
        if (value is string stringValue && targetType.IsEnum)
        {
            try
            {
                return Enum.Parse(targetType, stringValue, true);
            }
            catch
            {
                return null;
            }
        }
        
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString();
    }
}

/// <summary>
/// Converter for numeric property values
/// </summary>
public class NumericValueConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return null;
        
        if (value is string stringValue)
        {
            if (decimal.TryParse(stringValue, NumberStyles.Float, culture, out var decimalResult))
                return decimalResult;
                
            if (double.TryParse(stringValue, NumberStyles.Float, culture, out var doubleResult))
                return (decimal)doubleResult;
                
            if (int.TryParse(stringValue, NumberStyles.Integer, culture, out var intResult))
                return intResult;
        }
        
        if (value is IConvertible convertible)
        {
            try
            {
                return System.Convert.ToDecimal(convertible);
            }
            catch
            {
                return null;
            }
        }
        
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return null;
        
        if (targetType == typeof(string))
            return value.ToString();
            
        try
        {
            if (targetType == typeof(int) || targetType == typeof(int?))
                return System.Convert.ToInt32(value);
            if (targetType == typeof(long) || targetType == typeof(long?))
                return System.Convert.ToInt64(value);
            if (targetType == typeof(short) || targetType == typeof(short?))
                return System.Convert.ToInt16(value);
            if (targetType == typeof(byte) || targetType == typeof(byte?))
                return System.Convert.ToByte(value);
            if (targetType == typeof(double) || targetType == typeof(double?))
                return System.Convert.ToDouble(value);
            if (targetType == typeof(float) || targetType == typeof(float?))
                return System.Convert.ToSingle(value);
            if (targetType == typeof(decimal) || targetType == typeof(decimal?))
                return System.Convert.ToDecimal(value);
        }
        catch
        {
            return null;
        }
        
        return value;
    }
}

/// <summary>
/// Converter that handles property value updates
/// </summary>
public class PropertyValueUpdateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not ACadSharp.Viewer.Interfaces.ObjectProperty property)
            return value;
            
        // Try to set the value on the property and return the result
        if (property.TrySetValue(value))
        {
            return property.Value;
        }
        
        // If setting failed, return the original value
        return property.Value;
    }
}