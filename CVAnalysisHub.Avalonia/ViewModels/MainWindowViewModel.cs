using CVAnalysisHub.Application.Common.ViewModels;
using CVAnalysisHub.Application.Home;
using CVAnalysisHub.Application.Videos;

namespace CVAnalysisHub.Avalonia.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    public MainWindowViewModel(
        HomeViewModel home,
        VideoWorkspaceViewModel videoWorkspace,
        AnalysisWorkspaceViewModel analysisWorkspace,
        DesktopNotificationViewModel notifications)
    {
        Home = home;
        VideoWorkspace = videoWorkspace;
        AnalysisWorkspace = analysisWorkspace;
        Notifications = notifications;

        VideoWorkspace.SelectedVideoChanged += HandleSelectedVideoChanged;
        VideoWorkspace.VideoUploadedAsync += HandleVideoUploadedAsync;
        AnalysisWorkspace.AnalysisRunsChangedAsync += HandleAnalysisRunsChangedAsync;

        RefreshOverviewCommand = new AsyncDelegateCommand(RefreshOverviewAsync);
    }

    public HomeViewModel Home { get; }

    public VideoWorkspaceViewModel VideoWorkspace { get; }

    public AnalysisWorkspaceViewModel AnalysisWorkspace { get; }

    public DesktopNotificationViewModel Notifications { get; }

    public AsyncDelegateCommand RefreshOverviewCommand { get; }

    public async Task InitializeAsync()
    {
        await ExecuteSafeAsync(
            async () =>
            {
                await Home.LoadAsync();
                await VideoWorkspace.InitializeAsync();
                await AnalysisWorkspace.InitializeAsync(VideoWorkspace.Videos.SelectedVideo);
            },
            "Desktop client synchronized with the shared application layer.");
    }

    public void Dispose()
    {
        VideoWorkspace.SelectedVideoChanged -= HandleSelectedVideoChanged;
        VideoWorkspace.VideoUploadedAsync -= HandleVideoUploadedAsync;
        AnalysisWorkspace.AnalysisRunsChangedAsync -= HandleAnalysisRunsChangedAsync;

        VideoWorkspace.Dispose();
        AnalysisWorkspace.Dispose();
    }

    private async Task RefreshOverviewAsync()
    {
        await ExecuteSafeAsync(
            () => Home.LoadAsync(),
            "Overview metrics refreshed.");
    }

    private void HandleSelectedVideoChanged()
    {
        AnalysisWorkspace.SyncSelectedCreateAnalysisVideo(VideoWorkspace.Videos.SelectedVideo);
    }

    private async Task HandleVideoUploadedAsync(VideoDto uploadedVideo)
    {
        await ExecuteSafeAsync(
            async () =>
            {
                await AnalysisWorkspace.ReloadAvailableVideosAsync(uploadedVideo);
                await Home.LoadAsync();
            },
            "Video uploaded and added to the shared library.");
    }

    private Task HandleAnalysisRunsChangedAsync()
    {
        return ExecuteSafeAsync(
            () => Home.LoadAsync(),
            "Overview metrics refreshed after analysis update.");
    }

    private async Task ExecuteSafeAsync(Func<Task> action, string successMessage)
    {
        Notifications.ClearError();

        try
        {
            await action();
            Notifications.ShowStatus(successMessage);
        }
        catch (Exception exception)
        {
            Notifications.ShowError(exception.Message);
        }
    }
}
