using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ACadSharp.Viewer.Interfaces;
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

        // For editable primitive types, create appropriate editor
        if (property.IsPrimitiveEditable && property.UnderlyingType != null)
        {
            return property.UnderlyingType switch
            {
                Type t when t == typeof(bool) => CreateBooleanEditor(),
                Type t when t.IsEnum => CreateEnumEditor(t),
                Type t when t == typeof(string) => CreateTextEditor(),
                Type t when t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte) => CreateIntegerEditor(),
                Type t when t == typeof(double) || t == typeof(float) || t == typeof(decimal) => CreateNumericEditor(),
                _ => CreateTextEditor()
            };
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

    private Control CreateBooleanEditor()
    {
        var checkBox = new CheckBox();
        checkBox.Bind(CheckBox.IsCheckedProperty, new Avalonia.Data.Binding("Value")
        {
            Converter = new BooleanValueConverter()
        });
        return checkBox;
    }

    private Control CreateEnumEditor(Type enumType)
    {
        var comboBox = new ComboBox
        {
            ItemsSource = Enum.GetValues(enumType)
        };
        comboBox.Bind(ComboBox.SelectedValueProperty, new Avalonia.Data.Binding("Value")
        {
            Converter = new EnumValueConverter()
        });
        return comboBox;
    }

    private Control CreateTextEditor()
    {
        var textBox = new TextBox
        {
            BorderThickness = new Avalonia.Thickness(1),
            Padding = new Avalonia.Thickness(4, 2)
        };
        textBox.Bind(TextBox.TextProperty, new Avalonia.Data.Binding("Value")
        {
            Mode = Avalonia.Data.BindingMode.TwoWay
        });
        return textBox;
    }

    private Control CreateIntegerEditor()
    {
        var numericUpDown = new NumericUpDown
        {
            FormatString = "0",
            Increment = 1,
            BorderThickness = new Avalonia.Thickness(1),
            Padding = new Avalonia.Thickness(4, 2)
        };
        numericUpDown.Bind(NumericUpDown.ValueProperty, new Avalonia.Data.Binding("Value")
        {
            Converter = new NumericValueConverter(),
            Mode = Avalonia.Data.BindingMode.TwoWay
        });
        return numericUpDown;
    }

    private Control CreateNumericEditor()
    {
        var numericUpDown = new NumericUpDown
        {
            FormatString = "0.###",
            Increment = 0.1m,
            BorderThickness = new Avalonia.Thickness(1),
            Padding = new Avalonia.Thickness(4, 2)
        };
        numericUpDown.Bind(NumericUpDown.ValueProperty, new Avalonia.Data.Binding("Value")
        {
            Converter = new NumericValueConverter(),
            Mode = Avalonia.Data.BindingMode.TwoWay
        });
        return numericUpDown;
    }
}