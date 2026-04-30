using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CVAnalysisHub.Avalonia.ViewModels;

namespace CVAnalysisHub.Avalonia.Views;

public partial class VideosView : UserControl
{
    public VideosView()
    {
        InitializeComponent();
    }

    private async void SelectUploadFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);

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
        viewModel.VideoWorkspace.SetSelectedUploadFile(filePath, selectedFile!.Name, fileInfo.Length);
    }
}
