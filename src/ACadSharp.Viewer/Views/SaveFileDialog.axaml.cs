using ACadSharp.Viewer.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;

namespace ACadSharp.Viewer.Views;

public partial class SaveFileDialog : Window
{
    public SaveFileDialogViewModel ViewModel => (SaveFileDialogViewModel)DataContext!;

    public SaveFileDialog()
    {
        InitializeComponent();
    }

    private async void BrowseFolder_Click(object? sender, RoutedEventArgs e)
    {
        var options = new FolderPickerOpenOptions
        {
            Title = "Select Save Location",
            AllowMultiple = false
        };

        var result = await StorageProvider.OpenFolderPickerAsync(options);
        if (result.Count > 0)
        {
            ViewModel.FolderPath = result[0].Path.LocalPath;
        }
    }
}