using Avalonia.Data.Converters;
using Avalonia.Media;
using ACadSharp.Viewer.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ACadSharp.Viewer.Converters;

/// <summary>
/// Converter to highlight search results
/// </summary>
public class SearchHighlightConverter : IValueConverter, IMultiValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isHighlighted && targetType == typeof(IBrush))
        {
            return isHighlighted ? Brushes.Yellow : Brushes.Transparent;
        }
            
        if (value is bool isVisible && targetType == typeof(bool))
        {
            return isVisible;
        }
            
        return value;
    }

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && values[0] is string text && values[1] is bool isHighlighted)
        {
            if (targetType == typeof(IBrush))
            {
                return isHighlighted ? Brushes.Yellow : Brushes.Transparent;
            }
            return text;
        }
            
        return values.FirstOrDefault();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for DataGrid row background based on IsHighlighted property
/// </summary>
public class DataGridRowHighlightConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ObjectProperty property && targetType == typeof(IBrush))
        {
            return property.IsHighlighted ? Brushes.Yellow : Brushes.Transparent;
        }
            
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter that converts a count (integer) to a boolean for visibility
/// Returns true if count > 0, false if count = 0
/// </summary>
public class CountToBooleanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 0;
        }
        
        if (value is long longCount)
        {
            return longCount > 0;
        }
        
        // For collections
        if (value is System.Collections.ICollection collection)
        {
            return collection.Count > 0;
        }
        
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}