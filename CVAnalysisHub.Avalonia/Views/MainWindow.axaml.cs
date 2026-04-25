using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CVAnalysisHub.Avalonia.ViewModels;

namespace CVAnalysisHub.Avalonia.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? viewModel;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        DataContext = this.viewModel = viewModel;
        Opened += OnOpened;
        Closed += OnClosed;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        Opened -= OnOpened;

        if (viewModel is not null)
        {
            await viewModel.InitializeAsync();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        viewModel?.Dispose();
    }

    private async void SelectUploadFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (viewModel is null)
        {
            return;
        }

        var topLevel = GetTopLevel(this);

        if (topLevel?.StorageProvider is null)
        {
            return;
        }

        var selectedFiles = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Select a video file",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Video files")
                    {
                        Patterns = ["*.mp4", "*.mov", "*.avi", "*.mkv", "*.webm"]
                    }
                ]
            });

        var selectedFile = selectedFiles.FirstOrDefault();
        var filePath = selectedFile?.TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var fileInfo = new FileInfo(filePath);
        viewModel.SetSelectedUploadFile(filePath, selectedFile!.Name, fileInfo.Length);
    }
}
