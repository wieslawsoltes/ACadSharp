using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using ACadSharp.Viewer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ACadSharp.Viewer.Controls;

/// <summary>
/// Custom property editor for editable text properties
/// </summary>
public class EditablePropertyTextBox : TextBox
{
    public static readonly StyledProperty<ObjectProperty?> PropertyProperty =
        AvaloniaProperty.Register<EditablePropertyTextBox, ObjectProperty?>(nameof(Property));

    public ObjectProperty? Property
    {
        get => GetValue(PropertyProperty);
        set => SetValue(PropertyProperty, value);
    }

    public EditablePropertyTextBox()
    {
        LostFocus += OnLostFocus;
        KeyDown += OnKeyDown;
        BorderThickness = new Thickness(1);
        Padding = new Thickness(4, 2);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == PropertyProperty)
        {
            if (change.NewValue is ObjectProperty property)
            {
                Text = property.Value;
                IsReadOnly = !property.IsEditable;
            }
        }
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        CommitValue();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitValue();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            // Reset to original value
            if (Property != null)
            {
                Text = Property.Value;
            }
            e.Handled = true;
        }
    }

    private void CommitValue()
    {
        if (Property != null && Property.IsEditable)
        {
            if (Property.TrySetValue(Text))
            {
                // Value was successfully set, update display
                Text = Property.Value;
            }
            else
            {
                // Failed to set value, revert to original
                Text = Property.Value;
            }
        }
    }
}

/// <summary>
/// Custom property editor for boolean properties
/// </summary>
public class EditablePropertyCheckBox : CheckBox
{
    public static readonly StyledProperty<ObjectProperty?> PropertyProperty =
        AvaloniaProperty.Register<EditablePropertyCheckBox, ObjectProperty?>(nameof(Property));

    public ObjectProperty? Property
    {
        get => GetValue(PropertyProperty);
        set => SetValue(PropertyProperty, value);
    }

    public EditablePropertyCheckBox()
    {
        Click += OnClick;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == PropertyProperty)
        {
            if (change.NewValue is ObjectProperty property)
            {
                IsChecked = bool.TryParse(property.Value, out var result) ? result : false;
                IsEnabled = property.IsEditable;
            }
        }
    }

    private void OnClick(object? sender, RoutedEventArgs e)
    {
        if (Property != null && Property.IsEditable)
        {
            Property.TrySetValue(IsChecked ?? false);
        }
    }
}

/// <summary>
/// Custom property editor for enum properties
/// </summary>
public class EditablePropertyComboBox : ComboBox
{
    public static readonly StyledProperty<ObjectProperty?> PropertyProperty =
        AvaloniaProperty.Register<EditablePropertyComboBox, ObjectProperty?>(nameof(Property));

    public ObjectProperty? Property
    {
        get => GetValue(PropertyProperty);
        set => SetValue(PropertyProperty, value);
    }

    public EditablePropertyComboBox()
    {
        SelectionChanged += OnSelectionChanged;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == PropertyProperty)
        {
            if (change.NewValue is ObjectProperty property && property.UnderlyingType?.IsEnum == true)
            {
                // Set enum values as items
                var enumValues = Enum.GetValues(property.UnderlyingType).Cast<object>().ToList();
                ItemsSource = enumValues;
                
                // Set current value
                if (Enum.TryParse(property.UnderlyingType, property.Value, true, out var currentValue))
                {
                    SelectedItem = currentValue;
                }
                
                IsEnabled = property.IsEditable;
            }
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Property != null && Property.IsEditable && SelectedItem != null)
        {
            Property.TrySetValue(SelectedItem);
        }
    }
}

/// <summary>
/// Custom property editor for numeric properties
/// </summary>
public class EditablePropertyNumericUpDown : NumericUpDown
{
    public static readonly StyledProperty<ObjectProperty?> PropertyProperty =
        AvaloniaProperty.Register<EditablePropertyNumericUpDown, ObjectProperty?>(nameof(Property));

    public ObjectProperty? Property
    {
        get => GetValue(PropertyProperty);
        set => SetValue(PropertyProperty, value);
    }

    public EditablePropertyNumericUpDown()
    {
        ValueChanged += OnValueChanged;
        BorderThickness = new Thickness(1);
        Padding = new Thickness(4, 2);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == PropertyProperty)
        {
            if (change.NewValue is ObjectProperty property)
            {
                ConfigureForPropertyType(property);
                
                if (decimal.TryParse(property.Value, out var decimalValue))
                {
                    Value = decimalValue;
                }
                
                IsEnabled = property.IsEditable;
            }
        }
    }

    private void ConfigureForPropertyType(ObjectProperty property)
    {
        if (property.UnderlyingType == null) return;

        var type = property.UnderlyingType;
        
        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
        {
            // Integer types
            FormatString = "0";
            Increment = 1;
            
            if (type == typeof(byte))
            {
                Minimum = byte.MinValue;
                Maximum = byte.MaxValue;
            }
            else if (type == typeof(short))
            {
                Minimum = short.MinValue;
                Maximum = short.MaxValue;
            }
            else if (type == typeof(int))
            {
                Minimum = int.MinValue;
                Maximum = int.MaxValue;
            }
        }
        else if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
        {
            // Floating point types
            FormatString = "0.###";
            Increment = 0.1m;
            
            if (type == typeof(float))
            {
                Minimum = decimal.MinValue;
                Maximum = decimal.MaxValue;
            }
            else if (type == typeof(double))
            {
                Minimum = decimal.MinValue;
                Maximum = decimal.MaxValue;
            }
        }
    }

    private void OnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (Property != null && Property.IsEditable && Value.HasValue)
        {
            Property.TrySetValue(Value.Value);
        }
    }
}