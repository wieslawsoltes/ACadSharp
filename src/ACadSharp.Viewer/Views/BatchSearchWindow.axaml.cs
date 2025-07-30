using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ACadSharp.Viewer.Views;

/// <summary>
/// Code-behind for BatchSearchWindow
/// </summary>
public partial class BatchSearchWindow : Window
{
    public BatchSearchWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles the close button click
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments</param>
    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
} 