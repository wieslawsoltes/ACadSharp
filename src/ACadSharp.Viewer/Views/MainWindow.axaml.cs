using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Input;
using ACadSharp.Viewer.ViewModels;
using ACadSharp.Viewer.Interfaces;
using ACadSharp.Viewer.Controls;
using ACadSharp.Viewer.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using System;
using Avalonia.LogicalTree;
using Avalonia.Controls.Primitives;

namespace ACadSharp.Viewer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SetupDragAndDrop();
        SetupTreeViewExpansion();
    }

    private async void LoadDwgButton_Click(object sender, RoutedEventArgs e)
    {
        var viewModel = DataContext as MainWindowViewModel;
        if (viewModel != null)
        {
            await viewModel.LoadLeftFileNoParamAsync();
        }
    }

    private async void LoadDxfButton_Click(object sender, RoutedEventArgs e)
    {
        var viewModel = DataContext as MainWindowViewModel;
        if (viewModel != null)
        {
            await viewModel.LoadRightFileNoParamAsync();
        }
    }

    private void SetupDragAndDrop()
    {
        // Setup left panel drag and drop
        var leftPanel = this.FindControl<Border>("LeftPanel");
        if (leftPanel != null)
        {
            leftPanel.AddHandler(DragDrop.DropEvent, OnLeftPanelDrop);
            leftPanel.AddHandler(DragDrop.DragOverEvent, OnLeftPanelDragOver);
            leftPanel.AddHandler(DragDrop.DragEnterEvent, OnLeftPanelDragEnter);
            leftPanel.AddHandler(DragDrop.DragLeaveEvent, OnLeftPanelDragLeave);
        }

        // Setup right panel drag and drop
        var rightPanel = this.FindControl<Border>("RightPanel");
        if (rightPanel != null)
        {
            rightPanel.AddHandler(DragDrop.DropEvent, OnRightPanelDrop);
            rightPanel.AddHandler(DragDrop.DragOverEvent, OnRightPanelDragOver);
            rightPanel.AddHandler(DragDrop.DragEnterEvent, OnRightPanelDragEnter);
            rightPanel.AddHandler(DragDrop.DragLeaveEvent, OnRightPanelDragLeave);
        }
    }



    private void OnLeftPanelDragEnter(object? sender, DragEventArgs e)
    {
        if (IsValidFileDrop(e))
        {
            e.DragEffects = DragDropEffects.Copy;
            if (sender is Border border)
            {
                border.Classes.Add("drag-over");
            }
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnLeftPanelDragLeave(object? sender, DragEventArgs e)
    {
        if (sender is Border border)
        {
            border.Classes.Remove("drag-over");
        }
        e.Handled = true;
    }

    private void OnLeftPanelDragOver(object? sender, DragEventArgs e)
    {
        if (IsValidFileDrop(e))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnRightPanelDragEnter(object? sender, DragEventArgs e)
    {
        if (IsValidFileDrop(e))
        {
            e.DragEffects = DragDropEffects.Copy;
            if (sender is Border border)
            {
                border.Classes.Add("drag-over");
            }
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnRightPanelDragLeave(object? sender, DragEventArgs e)
    {
        if (sender is Border border)
        {
            border.Classes.Remove("drag-over");
        }
        e.Handled = true;
    }

    private void OnRightPanelDragOver(object? sender, DragEventArgs e)
    {
        if (IsValidFileDrop(e))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private async void OnLeftPanelDrop(object? sender, DragEventArgs e)
    {
        // Remove visual feedback
        if (sender is Border border)
        {
            border.Classes.Remove("drag-over");
        }

        if (IsValidFileDrop(e))
        {
            var filePath = GetFilePathFromDrop(e);
            if (!string.IsNullOrEmpty(filePath))
            {
                var viewModel = DataContext as MainWindowViewModel;
                if (viewModel != null)
                {
                    await viewModel.LoadLeftFileAsync(filePath);
                }
            }
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private async void OnRightPanelDrop(object? sender, DragEventArgs e)
    {
        // Remove visual feedback
        if (sender is Border border)
        {
            border.Classes.Remove("drag-over");
        }

        if (IsValidFileDrop(e))
        {
            var filePath = GetFilePathFromDrop(e);
            if (!string.IsNullOrEmpty(filePath))
            {
                var viewModel = DataContext as MainWindowViewModel;
                if (viewModel != null)
                {
                    await viewModel.LoadRightFileAsync(filePath);
                }
            }
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private bool IsValidFileDrop(DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            if (files != null)
            {
                foreach (var file in files)
                {
                    if (IsValidCadFile(file?.Path?.LocalPath))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private string? GetFilePathFromDrop(DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            if (files != null)
            {
                foreach (var file in files)
                {
                    var filePath = file?.Path?.LocalPath;
                    if (IsValidCadFile(filePath))
                    {
                        return filePath;
                    }
                }
            }
        }
        return null;
    }

    private bool IsValidCadFile(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension == ".dwg" || extension == ".dxf";
    }

    private void BreadcrumbButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ACadSharp.Viewer.Interfaces.BreadcrumbItem breadcrumbItem)
        {
            var viewModel = DataContext as MainWindowViewModel;
            viewModel?.NavigateToBreadcrumb(breadcrumbItem);
        }
    }

    private void PropertyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ACadSharp.Viewer.Interfaces.ObjectProperty property)
        {
            var viewModel = DataContext as MainWindowViewModel;
            viewModel?.NavigateToProperty(property);
        }
    }

    private void DataGrid_BeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
    {
        // Check if the property is editable
        if (e.Row.DataContext is ACadSharp.Viewer.Interfaces.ObjectProperty property)
        {
            System.Diagnostics.Debug.WriteLine($"BeginningEdit: {property.Name}, IsEditable: {property.IsEditable}, IsNavigable: {property.IsNavigable}");
            
            // Cancel edit if property is not editable
            if (!property.IsEditable)
            {
                e.Cancel = true;
                System.Diagnostics.Debug.WriteLine($"Edit canceled for {property.Name}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Edit allowed for {property.Name}");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("BeginningEdit: No ObjectProperty found");
        }
    }

    private void DataGrid_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Commit && 
            e.Row.DataContext is ACadSharp.Viewer.Interfaces.ObjectProperty property &&
            e.EditingElement is TextBox textBox)
        {
            // Try to set the new value
            if (property.IsEditable && property.TrySetValue(textBox.Text))
            {
                // Refresh the property display after successful edit
                var viewModel = DataContext as MainWindowViewModel;
                Avalonia.Threading.Dispatcher.UIThread.Post(() => viewModel?.RefreshPropertyGrids());
            }
            else
            {
                // Cancel the edit if the value couldn't be set
                e.Cancel = true;
            }
        }
    }

    private void EditTextBox_Loaded(object? sender, RoutedEventArgs e)
    {
        // Auto-select text when entering edit mode
        if (sender is TextBox textBox)
        {
            textBox.Focus();
            textBox.SelectAll();
        }
    }

    private void SetupTreeViewExpansion()
    {
        // Setup left TreeView expansion handling
        var leftTreeView = this.FindControl<TreeView>("LeftTreeView");
        if (leftTreeView != null)
        {
            SetupTreeViewItemExpansion(leftTreeView);
        }

        // Setup right TreeView expansion handling  
        var rightTreeView = this.FindControl<TreeView>("RightTreeView");
        if (rightTreeView != null)
        {
            SetupTreeViewItemExpansion(rightTreeView);
        }
    }

    private void SetupTreeViewItemExpansion(TreeView treeView)
    {
        treeView.ContainerPrepared += OnTreeViewContainerPrepared;
    }

    private void OnTreeViewContainerPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        if (e.Container is TreeViewItem treeViewItem)
        {
            // Get the data context which should be the CadObjectTreeNode
            treeViewItem.DataContextChanged += (s, args) =>
            {
                if (treeViewItem.DataContext is CadObjectTreeNode node)
                {
                    SetupTreeViewItemSync(treeViewItem, node);
                }
            };

            // If DataContext is already set
            if (treeViewItem.DataContext is CadObjectTreeNode existingNode)
            {
                SetupTreeViewItemSync(treeViewItem, existingNode);
            }
        }
    }

    private void SetupTreeViewItemSync(TreeViewItem treeViewItem, CadObjectTreeNode node)
    {
        // Sync the expansion state from the data model to the UI
        treeViewItem.IsExpanded = node.IsExpanded;

        // Listen for changes in the UI and update the data model
        treeViewItem.GetObservable(TreeViewItem.IsExpandedProperty).Subscribe(isExpanded =>
        {
            if (node.IsExpanded != isExpanded)
            {
                node.IsExpanded = isExpanded;
            }
        });

        // Listen for changes in the data model and update the UI
        node.PropertyChanged += (s, args) =>
        {
            if (args.PropertyName == nameof(CadObjectTreeNode.IsExpanded))
            {
                if (treeViewItem.IsExpanded != node.IsExpanded)
                {
                    treeViewItem.IsExpanded = node.IsExpanded;
                }
            }
        };
    }

}
