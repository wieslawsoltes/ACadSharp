using ACadSharp.Viewer.Interfaces;
using ACadSharp.Viewer.Models;
using ACadSharp.Viewer.Services;
using ACadSharp.Viewer.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACadSharp.Viewer.Views;

public partial class BatchConverterWindow : Window
{
    private readonly ISettingsService _settingsService;
    private const string WindowSettingsKey = "BatchConverterWindow";
    
    public BatchConverterWindow()
    {
        InitializeComponent();
        
        // Create services and view model
        var cadFileService = new CadFileService();
        var batchConverterService = new BatchConverterService(cadFileService);
        _settingsService = new SettingsService();
        var viewModel = new BatchConverterViewModel(batchConverterService, _settingsService);
        
        DataContext = viewModel;
        
        // Subscribe to view model events
        viewModel.RequestClose += (sender, e) => Close();
        
        // Set up folder browser commands
        SetupFolderBrowsers(viewModel);
        
        // Load window settings
        _ = LoadWindowSettingsAsync();
    }

    public BatchConverterWindow(IBatchConverterService batchConverterService, ISettingsService? settingsService = null)
    {
        InitializeComponent();
        
        _settingsService = settingsService ?? new SettingsService();
        var viewModel = new BatchConverterViewModel(batchConverterService, _settingsService);
        DataContext = viewModel;
        
        // Subscribe to view model events
        viewModel.RequestClose += (sender, e) => Close();
        
        // Set up folder browser commands
        SetupFolderBrowsers(viewModel);
        
        // Load window settings
        _ = LoadWindowSettingsAsync();
    }

    private void SetupFolderBrowsers(BatchConverterViewModel viewModel)
    {
        // Subscribe to the command events to handle folder browsing
        viewModel.BrowseInputFolderCommand.Subscribe(async _ =>
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Input Folder",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                viewModel.InputFolder = folders[0].Path.LocalPath;
            }
        });

        viewModel.BrowseOutputFolderCommand.Subscribe(async _ =>
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Output Folder",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                viewModel.OutputFolder = folders[0].Path.LocalPath;
            }
        });
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        // Focus the input folder textbox
        if (DataContext is BatchConverterViewModel viewModel)
        {
            // Set a reasonable default for the output folder if not set
            if (string.IsNullOrWhiteSpace(viewModel.OutputFolder))
            {
                viewModel.OutputFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Prevent closing while conversion is in progress
        if (DataContext is BatchConverterViewModel viewModel && viewModel.IsConverting)
        {
            e.Cancel = true;
            
            // Show a message or ask user to cancel conversion first
            // For now, we'll just prevent closing
            return;
        }
        
        // Save window settings before closing
        _ = SaveWindowSettingsAsync();
        
        base.OnClosing(e);
    }
    
    private async Task LoadWindowSettingsAsync()
    {
        try
        {
            var defaultSettings = new BatchConverterSettings();
            var settings = await _settingsService.LoadSettingsAsync(WindowSettingsKey, defaultSettings);
            
            // Apply window size settings
            if (settings.WindowWidth > 0 && settings.WindowHeight > 0)
            {
                Width = settings.WindowWidth;
                Height = settings.WindowHeight;
            }
        }
        catch (Exception ex)
        {
            // Log error but continue with defaults
            System.Diagnostics.Debug.WriteLine($"Failed to load window settings: {ex.Message}");
        }
    }
    
    private async Task SaveWindowSettingsAsync()
    {
        try
        {
            // Get current settings to preserve other properties
            var defaultSettings = new BatchConverterSettings();
            var settings = await _settingsService.LoadSettingsAsync(WindowSettingsKey, defaultSettings);
            
            // Update window size
            settings.WindowWidth = Width;
            settings.WindowHeight = Height;
            
            await _settingsService.SaveSettingsAsync(WindowSettingsKey, settings);
        }
        catch (Exception ex)
        {
            // Log error but don't show to user
            System.Diagnostics.Debug.WriteLine($"Failed to save window settings: {ex.Message}");
        }
    }
}