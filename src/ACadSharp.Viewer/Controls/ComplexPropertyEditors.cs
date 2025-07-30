using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using ACadSharp.Viewer.Interfaces;
using System;
using System.Globalization;

namespace ACadSharp.Viewer.Controls;

/// <summary>
/// Property editor for XYZ coordinate properties (CSMath.XY, CSMath.XYZ)
/// </summary>
public class CoordinatePropertyEditor : UserControl
{
    public static readonly StyledProperty<ObjectProperty?> PropertyProperty =
        AvaloniaProperty.Register<CoordinatePropertyEditor, ObjectProperty?>(nameof(Property));

    public ObjectProperty? Property
    {
        get => GetValue(PropertyProperty);
        set => SetValue(PropertyProperty, value);
    }

    private readonly TextBox _xTextBox;
    private readonly TextBox _yTextBox;
    private readonly TextBox _zTextBox;
    private readonly StackPanel _container;

    public CoordinatePropertyEditor()
    {
        _container = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
        
        _xTextBox = CreateCoordinateTextBox("X");
        _yTextBox = CreateCoordinateTextBox("Y");
        _zTextBox = CreateCoordinateTextBox("Z");

        _container.Children.Add(new TextBlock { Text = "X:", VerticalAlignment = VerticalAlignment.Center });
        _container.Children.Add(_xTextBox);
        _container.Children.Add(new TextBlock { Text = "Y:", VerticalAlignment = VerticalAlignment.Center });
        _container.Children.Add(_yTextBox);
        _container.Children.Add(new TextBlock { Text = "Z:", VerticalAlignment = VerticalAlignment.Center });
        _container.Children.Add(_zTextBox);

        Content = _container;
    }

    private TextBox CreateCoordinateTextBox(string coordinate)
    {
        var textBox = new TextBox
        {
            Width = 80,
            Watermark = coordinate,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2)
        };

        textBox.LostFocus += (s, e) => CommitCoordinateValue();
        textBox.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                CommitCoordinateValue();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                LoadCoordinateValues();
                e.Handled = true;
            }
        };

        return textBox;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == PropertyProperty)
        {
            LoadCoordinateValues();
        }
    }

    private void LoadCoordinateValues()
    {
        if (Property?.PropertyObject == null) return;

        try
        {
            var obj = Property.PropertyObject;
            var type = obj.GetType();

            // Handle CSMath.XY
            if (type.Name == "XY" || type.FullName?.Contains("CSMath.XY") == true)
            {
                var xProp = type.GetProperty("X");
                var yProp = type.GetProperty("Y");
                
                if (xProp != null && yProp != null)
                {
                    _xTextBox.Text = xProp.GetValue(obj)?.ToString() ?? "0";
                    _yTextBox.Text = yProp.GetValue(obj)?.ToString() ?? "0";
                    _zTextBox.Text = "0";
                    _zTextBox.IsEnabled = false;
                }
            }
            // Handle CSMath.XYZ
            else if (type.Name == "XYZ" || type.FullName?.Contains("CSMath.XYZ") == true)
            {
                var xProp = type.GetProperty("X");
                var yProp = type.GetProperty("Y");
                var zProp = type.GetProperty("Z");
                
                if (xProp != null && yProp != null && zProp != null)
                {
                    _xTextBox.Text = xProp.GetValue(obj)?.ToString() ?? "0";
                    _yTextBox.Text = yProp.GetValue(obj)?.ToString() ?? "0";
                    _zTextBox.Text = zProp.GetValue(obj)?.ToString() ?? "0";
                    _zTextBox.IsEnabled = true;
                }
            }
        }
        catch
        {
            // Reset to defaults if error
            _xTextBox.Text = "0";
            _yTextBox.Text = "0";
            _zTextBox.Text = "0";
        }
    }

    private void CommitCoordinateValue()
    {
        if (Property?.PropertyObject == null || !Property.IsEditable) return;

        try
        {
            if (!double.TryParse(_xTextBox.Text, out var x) ||
                !double.TryParse(_yTextBox.Text, out var y) ||
                !double.TryParse(_zTextBox.Text, out var z))
            {
                LoadCoordinateValues(); // Reset on invalid input
                return;
            }

            var obj = Property.PropertyObject;
            var type = obj.GetType();

            // Create new coordinate object
            object? newCoordinate = null;

            if (type.Name == "XY" || type.FullName?.Contains("CSMath.XY") == true)
            {
                // Try to create new XY instance
                newCoordinate = Activator.CreateInstance(type, x, y);
            }
            else if (type.Name == "XYZ" || type.FullName?.Contains("CSMath.XYZ") == true)
            {
                // Try to create new XYZ instance
                newCoordinate = Activator.CreateInstance(type, x, y, z);
            }

            if (newCoordinate != null)
            {
                Property.TrySetValue(newCoordinate);
            }
        }
        catch
        {
            LoadCoordinateValues(); // Reset on error
        }
    }
}

/// <summary>
/// Property editor for Color properties
/// </summary>
public class ColorPropertyEditor : UserControl
{
    public static readonly StyledProperty<ObjectProperty?> PropertyProperty =
        AvaloniaProperty.Register<ColorPropertyEditor, ObjectProperty?>(nameof(Property));

    public ObjectProperty? Property
    {
        get => GetValue(PropertyProperty);
        set => SetValue(PropertyProperty, value);
    }

    private readonly ComboBox _colorComboBox;
    private readonly TextBox _customColorTextBox;

    public ColorPropertyEditor()
    {
        var container = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
        
        _colorComboBox = new ComboBox
        {
            Width = 120,
            ItemsSource = new[]
            {
                "ByLayer", "ByBlock", "Red", "Yellow", "Green", "Cyan", 
                "Blue", "Magenta", "White", "Gray", "Black", "Custom"
            }
        };

        _customColorTextBox = new TextBox
        {
            Width = 80,
            Watermark = "RGB/Index",
            IsVisible = false,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2)
        };

        _colorComboBox.SelectionChanged += OnColorSelectionChanged;
        _customColorTextBox.LostFocus += OnCustomColorLostFocus;
        _customColorTextBox.KeyDown += OnCustomColorKeyDown;

        container.Children.Add(new TextBlock { Text = "Color:", VerticalAlignment = VerticalAlignment.Center });
        container.Children.Add(_colorComboBox);
        container.Children.Add(_customColorTextBox);

        Content = container;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == PropertyProperty)
        {
            LoadColorValue();
        }
    }

    private void LoadColorValue()
    {
        if (Property?.PropertyObject == null) return;

        try
        {
            var colorObj = Property.PropertyObject;
            var colorString = colorObj.ToString() ?? "";

            // Try to match known color names or special values
            _colorComboBox.SelectedItem = colorString switch
            {
                var s when s.Contains("ByLayer") => "ByLayer",
                var s when s.Contains("ByBlock") => "ByBlock", 
                var s when s.Contains("Red") => "Red",
                var s when s.Contains("Yellow") => "Yellow",
                var s when s.Contains("Green") => "Green",
                var s when s.Contains("Cyan") => "Cyan",
                var s when s.Contains("Blue") => "Blue",
                var s when s.Contains("Magenta") => "Magenta",
                var s when s.Contains("White") => "White",
                var s when s.Contains("Gray") => "Gray",
                var s when s.Contains("Black") => "Black",
                _ => "Custom"
            };

            if (_colorComboBox.SelectedItem?.ToString() == "Custom")
            {
                _customColorTextBox.Text = colorString;
                _customColorTextBox.IsVisible = true;
            }
            else
            {
                _customColorTextBox.IsVisible = false;
            }
        }
        catch
        {
            _colorComboBox.SelectedItem = "ByLayer";
            _customColorTextBox.IsVisible = false;
        }
    }

    private void OnColorSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_colorComboBox.SelectedItem?.ToString() == "Custom")
        {
            _customColorTextBox.IsVisible = true;
            _customColorTextBox.Focus();
        }
        else
        {
            _customColorTextBox.IsVisible = false;
            CommitColorValue();
        }
    }

    private void OnCustomColorLostFocus(object? sender, RoutedEventArgs e)
    {
        CommitColorValue();
    }

    private void OnCustomColorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitColorValue();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            LoadColorValue();
            e.Handled = true;
        }
    }

    private void CommitColorValue()
    {
        if (Property == null || !Property.IsEditable) return;

        try
        {
            var selectedColor = _colorComboBox.SelectedItem?.ToString();
            var valueToSet = selectedColor == "Custom" ? _customColorTextBox.Text : selectedColor;

            if (!string.IsNullOrEmpty(valueToSet))
            {
                Property.TrySetValue(valueToSet);
            }
        }
        catch
        {
            LoadColorValue(); // Reset on error
        }
    }
}