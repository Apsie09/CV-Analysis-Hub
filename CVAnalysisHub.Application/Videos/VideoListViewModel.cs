using CVAnalysisHub.Application.Common.ViewModels;
using CVAnalysisHub.Application.Common.ObjectLists;

namespace CVAnalysisHub.Application.Videos;

public sealed class VideoListViewModel : ViewModelBase
{
    private static readonly ObjectListColumnDefinition<VideoDto>[] ColumnDefinitions =
    [
        new("originalFileName", "Original file", video => video.OriginalFileName),
        new("storedFileName", "Stored file", video => video.FileName),
        new("duration", "Duration", video => video.Duration.ToString(@"hh\:mm\:ss")),
        new("uploadedAtUtc", "Uploaded (UTC)", video => video.UploadedAtUtc.ToString("yyyy-MM-dd HH:mm")),
        new("status", "Status", video => video.Status)
    ];

    private IReadOnlyList<VideoDto> videos = Array.Empty<VideoDto>();
    private bool isLoading;
    private readonly IVideoService videoService;

    public VideoFilterViewModel Filters { get; } = new();

    public AsyncRelayCommand ReloadCommand { get; }

    public AsyncRelayCommand ApplyFiltersCommand { get; }

    public AsyncRelayCommand ClearFiltersCommand { get; }

    public ObjectListViewModel<VideoDto> Browser { get; } = new(
        ColumnDefinitions,
        ["originalFileName", "duration", "uploadedAtUtc", "status"]);

    public VideoListViewModel(IVideoService videoService)
    {
        this.videoService = videoService;
        ReloadCommand = new AsyncRelayCommand(() => LoadAsync());
        ApplyFiltersCommand = new AsyncRelayCommand(() => ApplyFiltersAsync());
        ClearFiltersCommand = new AsyncRelayCommand(() => ClearFiltersAsync());
    }

    public IReadOnlyList<VideoDto> Videos
    {
        get => videos;
        private set => SetProperty(ref videos, value);
    }

    public bool IsLoading
    {
        get => isLoading;
        private set => SetProperty(ref isLoading, value);
    }

    public bool HasActiveFilters => Filters.HasActiveValues;

    public VideoDto? SelectedVideo => Browser.SelectedItem;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;

        try
        {
            Videos = await videoService.SearchAsync(Filters.BuildRequest(), cancellationToken);
            Browser.SetItems(Videos);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ClearDisplay()
    {
        Videos = Array.Empty<VideoDto>();
        Browser.Clear();
        OnPropertyChanged(nameof(SelectedVideo));
    }

    public Task ApplyFiltersAsync(CancellationToken cancellationToken = default)
    {
        return LoadAsync(cancellationToken);
    }

    public Task ClearFiltersAsync(CancellationToken cancellationToken = default)
    {
        Filters.Clear();
        return LoadAsync(cancellationToken);
    }

    public void SetColumnVisibility(string columnKey, bool isVisible)
    {
        Browser.SetColumnVisibility(columnKey, isVisible);
    }

    public void SelectRow(ObjectListRowViewModel<VideoDto>? row)
    {
        Browser.SelectRow(row);
        OnPropertyChanged(nameof(SelectedVideo));
    }
}
