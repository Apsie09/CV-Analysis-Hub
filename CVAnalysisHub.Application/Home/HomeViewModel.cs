using CVAnalysisHub.Application.Common.ViewModels;
using CVAnalysisHub.Application.AnalysisRuns;
using CVAnalysisHub.Application.Videos;

namespace CVAnalysisHub.Application.Home;

public sealed class HomeViewModel(
    IVideoService videoService,
    IAnalysisRunService analysisRunService) : ViewModelBase
{
    private int totalVideos;
    private int totalAnalysisRuns;
    private int queuedAnalysisRuns;
    private int completedCount;
    private string latestVideoName = "No videos yet";
    private DateTime? latestUploadUtc;
    private bool isLoading;

    public int TotalVideos
    {
        get => totalVideos;
        private set => SetProperty(ref totalVideos, value);
    }

    public int TotalAnalysisRuns
    {
        get => totalAnalysisRuns;
        private set => SetProperty(ref totalAnalysisRuns, value);
    }

    public int QueuedAnalysisRuns
    {
        get => queuedAnalysisRuns;
        private set => SetProperty(ref queuedAnalysisRuns, value);
    }

    public int CompletedCount
    {
        get => completedCount;
        private set => SetProperty(ref completedCount, value);
    }

    public string LatestVideoName
    {
        get => latestVideoName;
        private set => SetProperty(ref latestVideoName, value);
    }

    public DateTime? LatestUploadUtc
    {
        get => latestUploadUtc;
        private set => SetProperty(ref latestUploadUtc, value);
    }

    public bool IsLoading
    {
        get => isLoading;
        private set => SetProperty(ref isLoading, value);
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;

        try
        {
            var videos = await videoService.GetAllAsync(cancellationToken);
            var analysisRuns = await analysisRunService.GetAllAsync(cancellationToken);
            var latestVideo = videos
                .OrderByDescending(video => video.UploadedAtUtc)
                .FirstOrDefault();

            TotalVideos = videos.Count;
            TotalAnalysisRuns = analysisRuns.Count;
            QueuedAnalysisRuns = analysisRuns.Count(run => run.Status == "Queued");
            CompletedCount = analysisRuns.Count(run => run.Status == "Completed");
            LatestVideoName = latestVideo?.OriginalFileName ?? "No videos yet";
            LatestUploadUtc = latestVideo?.UploadedAtUtc;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
