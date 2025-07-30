using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ACadSharp.Viewer.ViewModels;
using ACadSharp.Viewer.Views;
using ACadSharp.Viewer.Services;

namespace ACadSharp.Viewer;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            var fileDialogService = new FileDialogService(mainWindow);
            var viewModel = new MainWindowViewModel(fileDialogService);
                
            mainWindow.DataContext = viewModel;
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
