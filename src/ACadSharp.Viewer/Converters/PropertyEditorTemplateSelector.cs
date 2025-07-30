using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using ACadSharp.Viewer.Interfaces;
using ACadSharp.Viewer.Controls;
using System;

namespace ACadSharp.Viewer.Converters;

/// <summary>
/// Template selector for property editors based on property type
/// </summary>
public class PropertyEditorTemplateSelector : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is not ObjectProperty property)
            return null;

        // If not editable, return a TextBlock for display
        if (!property.IsEditable)
        {
            return new TextBlock 
            { 
                [!TextBlock.TextProperty] = new Avalonia.Data.Binding("Value"),
                [!TextBlock.BackgroundProperty] = new Avalonia.Data.Binding("IsHighlighted") 
                { 
                    Converter = new SearchHighlightConverter() 
                },
                Padding = new Avalonia.Thickness(4, 2)
            };
        }

        // For editable properties, create appropriate specialized editor
        if (property.IsEditable)
        {
            // Check for coordinate types first (XY, XYZ)
            if (IsCoordinateType(property))
            {
                return CreateCoordinateEditor(property);
            }
            
            // Check for color types
            if (IsColorType(property))
            {
                return CreateColorEditor(property);
            }

            // Handle primitive types with specialized editors
            if (property.UnderlyingType != null)
            {
                return property.UnderlyingType switch
                {
                    Type t when t == typeof(bool) => CreateBooleanEditor(property),
                    Type t when t.IsEnum => CreateEnumEditor(property),
                    Type t when t == typeof(string) => CreateTextEditor(property),
                    Type t when IsNumericType(t) => CreateNumericEditor(property),
                    _ => CreateTextEditor(property)
                };
            }
        }

        // For navigable properties, return a button
        if (property.IsNavigable)
        {
            return new Button
            {
                [!Button.ContentProperty] = new Avalonia.Data.Binding("Value"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                Background = Avalonia.Media.Brushes.Transparent,
                BorderThickness = new Avalonia.Thickness(0),
                Padding = new Avalonia.Thickness(0),
                Margin = new Avalonia.Thickness(0)
            };
        }

        // Default to text display
        return new TextBlock 
        { 
            [!TextBlock.TextProperty] = new Avalonia.Data.Binding("Value"),
            [!TextBlock.BackgroundProperty] = new Avalonia.Data.Binding("IsHighlighted") 
            { 
                Converter = new SearchHighlightConverter() 
            },
            Padding = new Avalonia.Thickness(4, 2)
        };
    }

    public bool Match(object? data) => data is ObjectProperty;

    private Control CreateBooleanEditor(ObjectProperty property)
    {
        var checkBox = new EditablePropertyCheckBox();
        checkBox.Bind(EditablePropertyCheckBox.PropertyProperty, new Binding { Source = property });
        return checkBox;
    }

    private Control CreateEnumEditor(ObjectProperty property)
    {
        var comboBox = new EditablePropertyComboBox();
        comboBox.Bind(EditablePropertyComboBox.PropertyProperty, new Binding { Source = property });
        return comboBox;
    }

    private Control CreateTextEditor(ObjectProperty property)
    {
        var textBox = new EditablePropertyTextBox();
        textBox.Bind(EditablePropertyTextBox.PropertyProperty, new Binding { Source = property });
        return textBox;
    }

    private Control CreateNumericEditor(ObjectProperty property)
    {
        var numericUpDown = new EditablePropertyNumericUpDown();
        numericUpDown.Bind(EditablePropertyNumericUpDown.PropertyProperty, new Binding { Source = property });
        return numericUpDown;
    }

    private Control CreateCoordinateEditor(ObjectProperty property)
    {
        var coordinateEditor = new CoordinatePropertyEditor();
        coordinateEditor.Bind(CoordinatePropertyEditor.PropertyProperty, new Binding { Source = property });
        return coordinateEditor;
    }

    private Control CreateColorEditor(ObjectProperty property)
    {
        var colorEditor = new ColorPropertyEditor();
        colorEditor.Bind(ColorPropertyEditor.PropertyProperty, new Binding { Source = property });
        return colorEditor;
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(long) || type == typeof(short) || 
               type == typeof(byte) || type == typeof(double) || type == typeof(float) || 
               type == typeof(decimal) || type == typeof(sbyte) || type == typeof(uint) ||
               type == typeof(ulong) || type == typeof(ushort);
    }

    private static bool IsCoordinateType(ObjectProperty property)
    {
        if (property.PropertyObject == null) return false;
        
        var typeName = property.PropertyObject.GetType().Name;
        var fullTypeName = property.PropertyObject.GetType().FullName ?? "";
        
        return typeName == "XY" || typeName == "XYZ" || 
               fullTypeName.Contains("CSMath.XY") || fullTypeName.Contains("CSMath.XYZ");
    }

    private static bool IsColorType(ObjectProperty property)
    {
        if (property.PropertyObject == null) return false;
        
        var typeName = property.PropertyObject.GetType().Name;
        var fullTypeName = property.PropertyObject.GetType().FullName ?? "";
        
        return typeName.Contains("Color") || fullTypeName.Contains("Color") ||
               property.Name.Contains("Color", StringComparison.OrdinalIgnoreCase);
    }
}