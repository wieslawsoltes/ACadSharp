using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Input;
using ACadSharp.Viewer.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;

namespace ACadSharp.Viewer.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SetupDragAndDrop();
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
    }
} 