using System.ComponentModel;
using CVAnalysisHub.Application.Common.ObjectLists;
using CVAnalysisHub.Application.Common.ViewModels;
using CVAnalysisHub.Application.Videos;
using CVAnalysisHub.Avalonia.Services;

namespace CVAnalysisHub.Avalonia.ViewModels;

public sealed class VideoWorkspaceViewModel : ViewModelBase, IDisposable
{
    private int selectedVideoPreviewVersion;

    public VideoWorkspaceViewModel(
        VideoListViewModel videos,
        CreateVideoViewModel createVideo,
        DesktopUploadViewModel upload,
        DesktopNotificationViewModel notifications,
        DesktopMediaService mediaService)
    {
        Videos = videos;
        CreateVideo = createVideo;
        Upload = upload;
        this.notifications = notifications;
        this.mediaService = mediaService;

        Videos.Browser.PropertyChanged += HandleVideoBrowserPropertyChanged;
        CreateVideo.PropertyChanged += HandleCreateVideoPropertyChanged;

        foreach (var column in Videos.Browser.AvailableColumns)
        {
            column.PropertyChanged += HandleVideoColumnPropertyChanged;
        }

        ReloadVideosCommand = new AsyncDelegateCommand(ReloadVideosAsync);
        ApplyVideoFiltersCommand = new AsyncDelegateCommand(ApplyVideoFiltersAsync);
        ClearVideoFiltersCommand = new AsyncDelegateCommand(ClearVideoFiltersAsync);
        ClearVideosCommand = new AsyncDelegateCommand(ClearVideosAsync);
        UploadSelectedVideoCommand = new AsyncDelegateCommand(UploadSelectedVideoAsync, () => CanUploadSelectedVideo);
        OpenSelectedVideoCommand = new AsyncDelegateCommand(OpenSelectedVideoAsync, () => HasSelectedVideoSource);
    }

    private readonly DesktopNotificationViewModel notifications;
    private readonly DesktopMediaService mediaService;

    public event Action? SelectedVideoChanged;

    public event Func<VideoDto, Task>? VideoUploadedAsync;

    public VideoListViewModel Videos { get; }

    public CreateVideoViewModel CreateVideo { get; }

    public DesktopUploadViewModel Upload { get; }

    public MediaPreviewViewModel SelectedVideoPreview { get; } = new();

    public AsyncDelegateCommand ReloadVideosCommand { get; }

    public AsyncDelegateCommand ApplyVideoFiltersCommand { get; }

    public AsyncDelegateCommand ClearVideoFiltersCommand { get; }

    public AsyncDelegateCommand ClearVideosCommand { get; }

    public AsyncDelegateCommand UploadSelectedVideoCommand { get; }

    public AsyncDelegateCommand OpenSelectedVideoCommand { get; }

    public bool HasSelectedUploadFile => Upload.HasFile;

    public bool CanUploadSelectedVideo => HasSelectedUploadFile && !CreateVideo.IsSaving;

    public bool HasSelectedVideoSource => mediaService.HasMedia(Videos.SelectedVideo?.SourceVideoRelativePath);

    public async Task InitializeAsync()
    {
        await Videos.ReloadCommand.ExecuteAsync();
        await LoadSelectedVideoPreviewAsync();
    }

    public void SetSelectedUploadFile(string filePath, string fileName, long fileSizeBytes)
    {
        Upload.SelectFile(filePath, fileName, FormatBytes(fileSizeBytes));
        CreateVideo.SetSelectedFile(fileName, ResolveContentType(fileName), fileSizeBytes);
        notifications.ShowStatus($"Selected {fileName} for upload.");
        RefreshUploadState();
    }

    public void ClearSelectedUploadFile()
    {
        Upload.Clear();
        CreateVideo.ClearSelection();
        RefreshUploadState();
    }

    public void Dispose()
    {
        Videos.Browser.PropertyChanged -= HandleVideoBrowserPropertyChanged;
        CreateVideo.PropertyChanged -= HandleCreateVideoPropertyChanged;

        foreach (var column in Videos.Browser.AvailableColumns)
        {
            column.PropertyChanged -= HandleVideoColumnPropertyChanged;
        }

        SelectedVideoPreview.Dispose();
    }

    public void SelectVideoRow(Guid videoId)
    {
        var matchingRow = Videos.Browser.Rows.FirstOrDefault(row => row.Item.Id == videoId);
        Videos.SelectRow(matchingRow);
    }

    private async Task ReloadVideosAsync()
    {
        await ExecuteSafeAsync(
            async () =>
            {
                await Videos.ReloadCommand.ExecuteAsync();
                await LoadSelectedVideoPreviewAsync();
            },
            "Video library refreshed.");
    }

    private async Task ApplyVideoFiltersAsync()
    {
        await ExecuteSafeAsync(
            async () =>
            {
                await Videos.ApplyFiltersCommand.ExecuteAsync();
                await LoadSelectedVideoPreviewAsync();
            },
            "Video filters applied.");
    }

    private async Task ClearVideoFiltersAsync()
    {
        await ExecuteSafeAsync(
            async () =>
            {
                await Videos.ClearFiltersCommand.ExecuteAsync();
                await LoadSelectedVideoPreviewAsync();
            },
            "Video filters cleared.");
    }

    private Task ClearVideosAsync()
    {
        Videos.ClearDisplay();
        SelectedVideoPreview.Clear();
        notifications.ShowStatus("Video list cleared. Reload to bind a fresh object set.");
        RefreshCommandStates();
        return Task.CompletedTask;
    }

    private async Task UploadSelectedVideoAsync()
    {
        if (!CanUploadSelectedVideo || string.IsNullOrWhiteSpace(Upload.FilePath))
        {
            return;
        }

        await ExecuteSafeAsync(
            async () =>
            {
                await using var stream = File.OpenRead(Upload.FilePath);
                var uploadedVideo = await CreateVideo.CreateAsync(stream);

                ClearSelectedUploadFile();
                await Videos.ReloadCommand.ExecuteAsync();
                SelectVideoRow(uploadedVideo.Id);

                if (VideoUploadedAsync is not null)
                {
                    await VideoUploadedAsync.Invoke(uploadedVideo);
                }
            },
            "Video uploaded and added to the shared library.");
    }

    private Task OpenSelectedVideoAsync()
    {
        return ExecuteSafeAsync(
            async () => await mediaService.OpenMediaAsync(Videos.SelectedVideo?.SourceVideoRelativePath),
            "Video opened in the system player.");
    }

    private async Task ExecuteSafeAsync(Func<Task> action, string successMessage)
    {
        notifications.ClearError();

        try
        {
            await action();
            notifications.ShowStatus(successMessage);
        }
        catch (Exception exception)
        {
            notifications.ShowError(exception.Message);
        }
        finally
        {
            RefreshCommandStates();
        }
    }

    private void RefreshUploadState()
    {
        OnPropertyChanged(nameof(HasSelectedUploadFile));
        OnPropertyChanged(nameof(CanUploadSelectedVideo));
        UploadSelectedVideoCommand.RaiseCanExecuteChanged();
    }

    private void RefreshCommandStates()
    {
        OnPropertyChanged(nameof(CanUploadSelectedVideo));
        OnPropertyChanged(nameof(HasSelectedVideoSource));
        OnPropertyChanged(nameof(SelectedVideoPreview));

        UploadSelectedVideoCommand.RaiseCanExecuteChanged();
        OpenSelectedVideoCommand.RaiseCanExecuteChanged();
    }

    private void HandleVideoColumnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ObjectListColumnViewModel.IsVisible))
        {
            Videos.Browser.ApplyCurrentColumns();
        }
    }

    private void HandleVideoBrowserPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ObjectListViewModel<VideoDto>.SelectedRow))
        {
            OnPropertyChanged(nameof(HasSelectedVideoSource));
            OpenSelectedVideoCommand.RaiseCanExecuteChanged();
            SelectedVideoChanged?.Invoke();
            _ = LoadSelectedVideoPreviewAsync();
        }
    }

    private void HandleCreateVideoPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CreateVideoViewModel.IsSaving) or nameof(CreateVideoViewModel.OriginalFileName))
        {
            RefreshUploadState();
        }
    }

    private async Task LoadSelectedVideoPreviewAsync()
    {
        var requestVersion = Interlocked.Increment(ref selectedVideoPreviewVersion);
        SelectedVideoPreview.Clear();

        var selectedVideo = Videos.SelectedVideo;

        if (selectedVideo is null)
        {
            if (requestVersion == selectedVideoPreviewVersion)
            {
                RefreshCommandStates();
            }

            return;
        }

        var preview = await mediaService.CreatePreviewAsync(
            selectedVideo.SourceVideoRelativePath,
            $"video-{selectedVideo.Id:N}.png");

        if (requestVersion != selectedVideoPreviewVersion)
        {
            preview?.Dispose();
            return;
        }

        SelectedVideoPreview.SetBitmap(preview);
        RefreshCommandStates();
    }

    private static string ResolveContentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".avi" => "video/x-msvideo",
            ".mkv" => "video/x-matroska",
            ".mov" => "video/quicktime",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            _ => "application/octet-stream"
        };
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var size = (double)bytes;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.##} {units[unitIndex]}";
    }
}
