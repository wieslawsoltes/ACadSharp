using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Linq;
using ACadSharp.Viewer.Interfaces;
using Microsoft.Extensions.Logging;

namespace ACadSharp.Viewer.ViewModels;

/// <summary>
/// Commands and logic for property editing functionality
/// </summary>
public class PropertyEditingCommands : ReactiveObject
{
    private readonly ILogger<PropertyEditingCommands>? _logger;

    public PropertyEditingCommands(ILogger<PropertyEditingCommands>? logger = null)
    {
        _logger = logger;

        // Create the property editing command
        EditPropertyCommand = ReactiveCommand.Create<ObjectProperty>(EditProperty);
        
        // Create validation command
        ValidatePropertyCommand = ReactiveCommand.Create<(ObjectProperty property, object? newValue), bool>(ValidateProperty);
    }

    /// <summary>
    /// Command to edit a property value
    /// </summary>
    public ReactiveCommand<ObjectProperty, Unit> EditPropertyCommand { get; }

    /// <summary>
    /// Command to validate a property value before editing
    /// </summary>
    public ReactiveCommand<(ObjectProperty property, object? newValue), bool> ValidatePropertyCommand { get; }

    /// <summary>
    /// Edits a property value
    /// </summary>
    /// <param name="property">The property to edit</param>
    private void EditProperty(ObjectProperty property)
    {
        try
        {
            if (!property.IsEditable)
            {
                _logger?.LogWarning("Attempted to edit non-editable property: {PropertyName}", property.Name);
                return;
            }

            // The actual editing is handled by the UI controls
            // This method can be used for logging or additional validation
            _logger?.LogInformation("Property {PropertyName} edited successfully", property.Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error editing property {PropertyName}", property.Name);
        }
    }

    /// <summary>
    /// Validates a property value before setting it
    /// </summary>
    /// <param name="input">Tuple containing the property and new value</param>
    /// <returns>True if the value is valid, false otherwise</returns>
    private bool ValidateProperty((ObjectProperty property, object? newValue) input)
    {
        var (property, newValue) = input;

        try
        {
            if (!property.IsEditable || property.PropertyInfo == null)
                return false;

            var propertyType = property.PropertyInfo.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            // Basic validation based on type
            if (newValue == null)
            {
                // Null is only valid for nullable types
                return Nullable.GetUnderlyingType(propertyType) != null || !propertyType.IsValueType;
            }

            // Type compatibility check
            if (underlyingType.IsAssignableFrom(newValue.GetType()))
                return true;

            // String conversion validation
            if (newValue is string stringValue)
            {
                return ValidateStringConversion(stringValue, underlyingType);
            }

            // Numeric conversion validation
            if (IsNumericType(underlyingType) && IsNumericType(newValue.GetType()))
            {
                return ValidateNumericConversion(newValue, underlyingType);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error validating property {PropertyName}", property.Name);
            return false;
        }
    }

    /// <summary>
    /// Validates string to type conversion
    /// </summary>
    private bool ValidateStringConversion(string stringValue, Type targetType)
    {
        if (targetType == typeof(string))
            return true;

        if (string.IsNullOrEmpty(stringValue))
            return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;

        try
        {
            if (targetType.IsEnum)
            {
                Enum.Parse(targetType, stringValue, true);
                return true;
            }

            if (targetType == typeof(bool))
                return bool.TryParse(stringValue, out _);

            if (IsNumericType(targetType))
            {
                return targetType switch
                {
                    Type t when t == typeof(int) => int.TryParse(stringValue, out _),
                    Type t when t == typeof(long) => long.TryParse(stringValue, out _),
                    Type t when t == typeof(short) => short.TryParse(stringValue, out _),
                    Type t when t == typeof(byte) => byte.TryParse(stringValue, out _),
                    Type t when t == typeof(double) => double.TryParse(stringValue, out _),
                    Type t when t == typeof(float) => float.TryParse(stringValue, out _),
                    Type t when t == typeof(decimal) => decimal.TryParse(stringValue, out _),
                    _ => false
                };
            }

            // Try general conversion
            Convert.ChangeType(stringValue, targetType);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates numeric type conversion
    /// </summary>
    private bool ValidateNumericConversion(object value, Type targetType)
    {
        try
        {
            Convert.ChangeType(value, targetType);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a type is numeric
    /// </summary>
    private bool IsNumericType(Type type)
    {
        return type == typeof(int) ||
               type == typeof(long) ||
               type == typeof(short) ||
               type == typeof(byte) ||
               type == typeof(double) ||
               type == typeof(float) ||
               type == typeof(decimal);
    }
}