using ACadSharp.Viewer.Interfaces;
using ACadSharp.Viewer.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace ACadSharp.Viewer.ViewModels;

/// <summary>
/// ViewModel for the batch converter window
/// </summary>
public class BatchConverterViewModel : ViewModelBase
{
    private readonly IBatchConverterService _batchConverterService;
    private readonly ISettingsService _settingsService;
    private CancellationTokenSource? _cancellationTokenSource;
    private const string SettingsKey = "BatchConverter";
    
    #region Fields
    private string _inputFolder = string.Empty;
    private string _outputFolder = string.Empty;
    private bool _recurseFolders = true;  // Default to true
    private bool _audit = true;
    private InputFileFilter _selectedInputFilter = InputFileFilter.DwgAndDxf;
    private OutputFormat _selectedOutputFormat = OutputFormat.DxfAscii2000;
    private bool _isConverting = false;
    private int _overallProgress = 0;
    private string _statusMessage = "Ready";
    private string _currentFile = string.Empty;
    private int _filesProcessed = 0;
    private int _totalFiles = 0;
    private int _successfulConversions = 0;
    private int _failedConversions = 0;
    private List<string> _conversionErrors = new List<string>();
    private bool _isCompleted = false;
    private TimeSpan _elapsedTime = TimeSpan.Zero;
    private TimeSpan _estimatedTime = TimeSpan.Zero;
    #endregion

    #region Properties
    public string InputFolder
    {
        get => _inputFolder;
        set
        {
            this.RaiseAndSetIfChanged(ref _inputFolder, value);
            UpdateFileCount();
            UpdateEstimatedTime();
        }
    }

    public string OutputFolder
    {
        get => _outputFolder;
        set
        {
            this.RaiseAndSetIfChanged(ref _outputFolder, value);
        }
    }

    public bool RecurseFolders
    {
        get => _recurseFolders;
        set
        {
            this.RaiseAndSetIfChanged(ref _recurseFolders, value);
            UpdateFileCount();
            UpdateEstimatedTime();
        }
    }

    public bool Audit
    {
        get => _audit;
        set => this.RaiseAndSetIfChanged(ref _audit, value);
    }

    public InputFileFilter SelectedInputFilter
    {
        get => _selectedInputFilter;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedInputFilter, value);
            this.RaisePropertyChanged(nameof(SelectedInputFilterItem));
            UpdateFileCount();
            UpdateEstimatedTime();
        }
    }

    public OutputFormat SelectedOutputFormat
    {
        get => _selectedOutputFormat;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedOutputFormat, value);
            this.RaisePropertyChanged(nameof(SelectedOutputFormatItem));
        }
    }

    public bool IsConverting
    {
        get => _isConverting;
        set
        {
            this.RaiseAndSetIfChanged(ref _isConverting, value);
        }
    }

    public int OverallProgress
    {
        get => _overallProgress;
        set => this.RaiseAndSetIfChanged(ref _overallProgress, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public string CurrentFile
    {
        get => _currentFile;
        set => this.RaiseAndSetIfChanged(ref _currentFile, value);
    }

    public int FilesProcessed
    {
        get => _filesProcessed;
        set => this.RaiseAndSetIfChanged(ref _filesProcessed, value);
    }

    public int TotalFiles
    {
        get => _totalFiles;
        set => this.RaiseAndSetIfChanged(ref _totalFiles, value);
    }

    public int SuccessfulConversions
    {
        get => _successfulConversions;
        set => this.RaiseAndSetIfChanged(ref _successfulConversions, value);
    }

    public int FailedConversions
    {
        get => _failedConversions;
        set => this.RaiseAndSetIfChanged(ref _failedConversions, value);
    }

    public List<string> ConversionErrors
    {
        get => _conversionErrors;
        set => this.RaiseAndSetIfChanged(ref _conversionErrors, value);
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set => this.RaiseAndSetIfChanged(ref _isCompleted, value);
    }

    public TimeSpan ElapsedTime
    {
        get => _elapsedTime;
        set => this.RaiseAndSetIfChanged(ref _elapsedTime, value);
    }

    public TimeSpan EstimatedTime
    {
        get => _estimatedTime;
        set => this.RaiseAndSetIfChanged(ref _estimatedTime, value);
    }



    // Collections for UI binding
    public Dictionary<InputFileFilter, string> InputFilterOptions { get; }
    public Dictionary<OutputFormat, string> OutputFormatOptions { get; }
    
    // Selected items for ComboBox binding (handles KeyValuePair)
    public KeyValuePair<InputFileFilter, string> SelectedInputFilterItem
    {
        get => InputFilterOptions.FirstOrDefault(x => x.Key == SelectedInputFilter);
        set => SelectedInputFilter = value.Key;
    }
    
    public KeyValuePair<OutputFormat, string> SelectedOutputFormatItem
    {
        get => OutputFormatOptions.FirstOrDefault(x => x.Key == SelectedOutputFormat);
        set => SelectedOutputFormat = value.Key;
    }

    // Commands
    public ReactiveCommand<Unit, Unit> StartCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseInputFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseOutputFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }

    #endregion

    #region Events
    public event EventHandler? RequestClose;
    #endregion

    public BatchConverterViewModel(IBatchConverterService batchConverterService, ISettingsService settingsService)
    {
        _batchConverterService = batchConverterService ?? throw new ArgumentNullException(nameof(batchConverterService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        // Initialize collections
        InputFilterOptions = EnumExtensions.GetDescriptions<InputFileFilter>();
        OutputFormatOptions = EnumExtensions.GetDescriptions<OutputFormat>();

        // Initialize commands
        var canStart = this.WhenAnyValue(x => x.IsConverting, x => x.InputFolder, x => x.OutputFolder,
            (isConverting, inputFolder, outputFolder) => 
                !isConverting && !string.IsNullOrWhiteSpace(inputFolder) && !string.IsNullOrWhiteSpace(outputFolder));
                
        var canCancel = this.WhenAnyValue(x => x.IsConverting);
        var canClose = this.WhenAnyValue(x => x.IsConverting, isConverting => !isConverting);

        StartCommand = ReactiveCommand.CreateFromTask(StartConversionAsync, canStart);
        CancelCommand = ReactiveCommand.Create(CancelConversion, canCancel);
        BrowseInputFolderCommand = ReactiveCommand.Create(BrowseInputFolder);
        BrowseOutputFolderCommand = ReactiveCommand.Create(BrowseOutputFolder);
        CloseCommand = ReactiveCommand.Create(CloseWindow, canClose);

        // Subscribe to service events
        _batchConverterService.ProgressChanged += OnProgressChanged;
        _batchConverterService.FileConversionStarted += OnFileConversionStarted;
        _batchConverterService.FileConversionCompleted += OnFileConversionCompleted;
        _batchConverterService.FileConversionFailed += OnFileConversionFailed;

        // Load settings on initialization
        _ = LoadSettingsAsync();
    }

    #region Private Methods
    private async Task StartConversionAsync()
    {
        try
        {
            IsConverting = true;
            IsCompleted = false;
            OverallProgress = 0;
            FilesProcessed = 0;
            SuccessfulConversions = 0;
            FailedConversions = 0;
            ConversionErrors = new List<string>(); // Create a new list instead of clearing
            StatusMessage = "Preparing conversion...";

            var configuration = new BatchConverterConfiguration
            {
                InputFolder = InputFolder,
                OutputFolder = OutputFolder,
                RecurseFolders = RecurseFolders,
                Audit = Audit,
                InputFileFilter = SelectedInputFilter,
                OutputFormat = SelectedOutputFormat
            };

            _cancellationTokenSource = new CancellationTokenSource();
            
            var startTime = DateTime.Now;
            var result = await _batchConverterService.ConvertAsync(configuration, _cancellationTokenSource.Token);
            ElapsedTime = DateTime.Now - startTime;

            // Update results on UI thread
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                SuccessfulConversions = result.SuccessfulConversions;
                FailedConversions = result.FailedConversions;
                ConversionErrors = result.Errors;

                if (result.Success)
                {
                    StatusMessage = $"Conversion completed successfully. {SuccessfulConversions} files converted.";
                }
                else if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    StatusMessage = "Conversion cancelled by user.";
                }
                else
                {
                    StatusMessage = $"Conversion completed with errors. {SuccessfulConversions} successful, {FailedConversions} failed.";
                }

                IsCompleted = true;
            });
        }
        catch (Exception ex)
        {
            // Update error information on UI thread
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusMessage = $"Conversion failed: {ex.Message}";
                // Add to existing errors list instead of replacing it
                var updatedErrors = new List<string>(ConversionErrors) { ex.Message };
                ConversionErrors = updatedErrors;
            });
        }
        finally
        {
            // Ensure UI updates happen on the UI thread
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsConverting = false;
                CurrentFile = string.Empty;
            });
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void CancelConversion()
    {
        _cancellationTokenSource?.Cancel();
        StatusMessage = "Cancelling conversion...";
    }

    private void BrowseInputFolder()
    {
        // This would typically open a folder browser dialog
        // Implementation would depend on the UI framework being used
        // For now, this is a placeholder
    }

    private void BrowseOutputFolder()
    {
        // This would typically open a folder browser dialog
        // Implementation would depend on the UI framework being used
        // For now, this is a placeholder
    }

    private void CloseWindow()
    {
        // Save settings when closing
        _ = SaveSettingsAsync();
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateFileCount()
    {
        if (string.IsNullOrWhiteSpace(InputFolder))
        {
            TotalFiles = 0;
            return;
        }

        try
        {
            var configuration = new BatchConverterConfiguration
            {
                InputFolder = InputFolder,
                RecurseFolders = RecurseFolders,
                InputFileFilter = SelectedInputFilter
            };

            var files = _batchConverterService.GetFilesToProcess(configuration);
            TotalFiles = files.Length;
        }
        catch
        {
            TotalFiles = 0;
        }
    }

    private void UpdateEstimatedTime()
    {
        if (string.IsNullOrWhiteSpace(InputFolder))
        {
            EstimatedTime = TimeSpan.Zero;
            return;
        }

        try
        {
            var configuration = new BatchConverterConfiguration
            {
                InputFolder = InputFolder,
                RecurseFolders = RecurseFolders,
                InputFileFilter = SelectedInputFilter
            };

            EstimatedTime = _batchConverterService.EstimateConversionTime(configuration);
        }
        catch
        {
            EstimatedTime = TimeSpan.Zero;
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var defaultSettings = new BatchConverterSettings();
            var settings = await _settingsService.LoadSettingsAsync(SettingsKey, defaultSettings);
            
            // Apply loaded settings
            InputFolder = settings.LastInputFolder;
            OutputFolder = settings.LastOutputFolder;
            RecurseFolders = settings.DefaultRecurseFolders;
            Audit = settings.DefaultAudit;
            SelectedInputFilter = settings.DefaultInputFileFilter;
            SelectedOutputFormat = settings.DefaultOutputFormat;
        }
        catch (Exception ex)
        {
            // Log error but continue with defaults
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }
    }
    
    private async Task SaveSettingsAsync()
    {
        try
        {
            var settings = new BatchConverterSettings
            {
                LastInputFolder = InputFolder,
                LastOutputFolder = OutputFolder,
                DefaultRecurseFolders = RecurseFolders,
                DefaultAudit = Audit,
                DefaultInputFileFilter = SelectedInputFilter,
                DefaultOutputFormat = SelectedOutputFormat
            };
            
            await _settingsService.SaveSettingsAsync(SettingsKey, settings);
        }
        catch (Exception ex)
        {
            // Log error but don't show to user
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    #endregion

    #region Event Handlers
    private void OnProgressChanged(object? sender, BatchConversionProgress progress)
    {
        // Ensure UI updates happen on the UI thread
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            OverallProgress = progress.OverallProgress;
            StatusMessage = progress.StatusMessage;
            CurrentFile = progress.CurrentFile;
            FilesProcessed = progress.FilesProcessed;
            TotalFiles = progress.TotalFiles;
            SuccessfulConversions = progress.SuccessfulConversions;
            FailedConversions = progress.FailedConversions;

            // Always update the conversion errors with the full accumulated list
            ConversionErrors = new List<string>(progress.Errors);
        });
    }

    private void OnFileConversionStarted(object? sender, string fileName)
    {
        // Ensure UI updates happen on the UI thread
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            CurrentFile = fileName;
        });
    }

    private void OnFileConversionCompleted(object? sender, string fileName)
    {
        // File completed successfully - no UI update needed
    }

    private void OnFileConversionFailed(object? sender, (string FileName, string Error) args)
    {
        // This event is handled by OnProgressChanged which already accumulates all errors
        // No need to manually add errors here as they're already added by the service
        // and will be updated via OnProgressChanged
    }
    #endregion

}