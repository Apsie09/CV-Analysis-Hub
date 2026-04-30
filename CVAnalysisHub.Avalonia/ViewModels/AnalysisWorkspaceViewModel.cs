using System.ComponentModel;
using CVAnalysisHub.Application.AnalysisRuns;
using CVAnalysisHub.Application.Common.ObjectLists;
using CVAnalysisHub.Application.Common.ViewModels;
using CVAnalysisHub.Application.Videos;
using CVAnalysisHub.Avalonia.Services;

namespace CVAnalysisHub.Avalonia.ViewModels;

public sealed class AnalysisWorkspaceViewModel : ViewModelBase, IDisposable
{
    private VideoDto? selectedCreateAnalysisVideo;
    private int selectedAnalysisPreviewVersion;

    public AnalysisWorkspaceViewModel(
        AnalysisRunListViewModel analysisRuns,
        CreateAnalysisRunViewModel createAnalysis,
        AnalysisRunDetailsViewModel analysisDetails,
        DesktopNotificationViewModel notifications,
        DesktopMediaService mediaService)
    {
        AnalysisRuns = analysisRuns;
        CreateAnalysis = createAnalysis;
        AnalysisDetails = analysisDetails;
        this.notifications = notifications;
        this.mediaService = mediaService;

        AnalysisRuns.Browser.PropertyChanged += HandleAnalysisBrowserPropertyChanged;
        CreateAnalysis.PropertyChanged += HandleCreateAnalysisPropertyChanged;
        AnalysisDetails.PropertyChanged += HandleAnalysisDetailsPropertyChanged;

        foreach (var column in AnalysisRuns.Browser.AvailableColumns)
        {
            column.PropertyChanged += HandleAnalysisColumnPropertyChanged;
        }

        ReloadAnalysisRunsCommand = new AsyncDelegateCommand(ReloadAnalysisRunsAsync);
        ApplyAnalysisFiltersCommand = new AsyncDelegateCommand(ApplyAnalysisFiltersAsync);
        ClearAnalysisFiltersCommand = new AsyncDelegateCommand(ClearAnalysisFiltersAsync);
        ClearAnalysisRunsCommand = new AsyncDelegateCommand(ClearAnalysisRunsAsync);
        CreateAnalysisCommand = new AsyncDelegateCommand(CreateAnalysisAsync, () => CanCreateAnalysis);
        RefreshSelectedAnalysisDetailsCommand = new AsyncDelegateCommand(
            RefreshSelectedAnalysisDetailsAsync,
            () => AnalysisRuns.SelectedAnalysisRun is not null);
        OpenSelectedSourceVideoCommand = new AsyncDelegateCommand(
            OpenSelectedSourceVideoAsync,
            () => HasSelectedAnalysisSourceVideo);
        OpenSelectedOutputVideoCommand = new AsyncDelegateCommand(
            OpenSelectedOutputVideoAsync,
            () => HasSelectedAnalysisOutputVideo);
    }

    private readonly DesktopNotificationViewModel notifications;
    private readonly DesktopMediaService mediaService;

    public event Func<Task>? AnalysisRunsChangedAsync;

    public AnalysisRunListViewModel AnalysisRuns { get; }

    public CreateAnalysisRunViewModel CreateAnalysis { get; }

    public AnalysisRunDetailsViewModel AnalysisDetails { get; }

    public MediaPreviewViewModel SelectedAnalysisSourcePreview { get; } = new();

    public MediaPreviewViewModel SelectedAnalysisOutputPreview { get; } = new();

    public AsyncDelegateCommand ReloadAnalysisRunsCommand { get; }

    public AsyncDelegateCommand ApplyAnalysisFiltersCommand { get; }

    public AsyncDelegateCommand ClearAnalysisFiltersCommand { get; }

    public AsyncDelegateCommand ClearAnalysisRunsCommand { get; }

    public AsyncDelegateCommand CreateAnalysisCommand { get; }

    public AsyncDelegateCommand RefreshSelectedAnalysisDetailsCommand { get; }

    public AsyncDelegateCommand OpenSelectedSourceVideoCommand { get; }

    public AsyncDelegateCommand OpenSelectedOutputVideoCommand { get; }

    public bool CanCreateAnalysis =>
        CreateAnalysis.SelectedVideoId.HasValue &&
        !CreateAnalysis.IsSaving &&
        !string.IsNullOrWhiteSpace(CreateAnalysis.ModelName);

    public bool HasSelectedAnalysisDetails => AnalysisDetails.AnalysisRun is not null;

    public bool HasSelectedAnalysisSourceVideo =>
        mediaService.HasMedia(AnalysisDetails.AnalysisRun?.OriginalVideoRelativePath);

    public bool HasSelectedAnalysisOutputVideo =>
        mediaService.HasMedia(AnalysisDetails.AnalysisRun?.OutputVideoRelativePath);

    public IReadOnlyList<string> SelectedAnalysisDetectionSummary =>
        AnalysisDetails.AnalysisRun?.DetectionResults
            .GroupBy(result => result.Label)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => $"{group.Key} {group.Count()}")
            .ToArray()
        ?? [];

    public VideoDto? SelectedCreateAnalysisVideo
    {
        get => selectedCreateAnalysisVideo;
        set
        {
            if (!SetProperty(ref selectedCreateAnalysisVideo, value))
            {
                return;
            }

            CreateAnalysis.SelectedVideoId = value?.Id;
            OnPropertyChanged(nameof(CanCreateAnalysis));
            CreateAnalysisCommand.RaiseCanExecuteChanged();
        }
    }

    public async Task InitializeAsync(VideoDto? selectedVideo)
    {
        await AnalysisRuns.ReloadCommand.ExecuteAsync();
        await CreateAnalysis.LoadAsync();
        SyncSelectedCreateAnalysisVideo(selectedVideo);
        await LoadSelectedAnalysisDetailsAsync();
    }

    public async Task ReloadAvailableVideosAsync(VideoDto? selectedVideo)
    {
        await CreateAnalysis.LoadAsync();
        SyncSelectedCreateAnalysisVideo(selectedVideo);
    }

    public void SyncSelectedCreateAnalysisVideo(VideoDto? selectedVideo)
    {
        if (CreateAnalysis.AvailableVideos.Count == 0)
        {
            SelectedCreateAnalysisVideo = null;
            return;
        }

        if (CreateAnalysis.SelectedVideoId.HasValue)
        {
            var currentlySelected = CreateAnalysis.AvailableVideos
                .FirstOrDefault(video => video.Id == CreateAnalysis.SelectedVideoId.Value);

            if (currentlySelected is not null)
            {
                SelectedCreateAnalysisVideo = currentlySelected;
                return;
            }
        }

        if (selectedVideo is not null)
        {
            var selectedFromList = CreateAnalysis.AvailableVideos
                .FirstOrDefault(video => video.Id == selectedVideo.Id);

            if (selectedFromList is not null)
            {
                SelectedCreateAnalysisVideo = selectedFromList;
                return;
            }
        }

        SelectedCreateAnalysisVideo = CreateAnalysis.AvailableVideos[0];
    }

    public void Dispose()
    {
        AnalysisRuns.Browser.PropertyChanged -= HandleAnalysisBrowserPropertyChanged;
        CreateAnalysis.PropertyChanged -= HandleCreateAnalysisPropertyChanged;
        AnalysisDetails.PropertyChanged -= HandleAnalysisDetailsPropertyChanged;

        foreach (var column in AnalysisRuns.Browser.AvailableColumns)
        {
            column.PropertyChanged -= HandleAnalysisColumnPropertyChanged;
        }

        SelectedAnalysisSourcePreview.Dispose();
        SelectedAnalysisOutputPreview.Dispose();
    }

    private async Task ReloadAnalysisRunsAsync()
    {
        await ExecuteSafeAsync(
            async () =>
            {
                await AnalysisRuns.ReloadCommand.ExecuteAsync();
                await LoadSelectedAnalysisDetailsAsync();
            },
            "Analysis list refreshed.");
    }

    private async Task ApplyAnalysisFiltersAsync()
    {
        await ExecuteSafeAsync(
            async () =>
            {
                await AnalysisRuns.ApplyFiltersCommand.ExecuteAsync();
                await LoadSelectedAnalysisDetailsAsync();
            },
            "Analysis filters applied.");
    }

    private async Task ClearAnalysisFiltersAsync()
    {
        await ExecuteSafeAsync(
            async () =>
            {
                await AnalysisRuns.ClearFiltersCommand.ExecuteAsync();
                await LoadSelectedAnalysisDetailsAsync();
            },
            "Analysis filters cleared.");
    }

    private Task ClearAnalysisRunsAsync()
    {
        AnalysisRuns.ClearDisplay();
        AnalysisDetails.Clear();
        SelectedAnalysisSourcePreview.Clear();
        SelectedAnalysisOutputPreview.Clear();
        notifications.ShowStatus("Analysis list cleared. Reload to bind a fresh object set.");
        OnPropertyChanged(nameof(HasSelectedAnalysisDetails));
        RefreshCommandStates();
        return Task.CompletedTask;
    }

    private async Task CreateAnalysisAsync()
    {
        if (!CanCreateAnalysis)
        {
            return;
        }

        await ExecuteSafeAsync(
            async () =>
            {
                var createdAnalysis = await CreateAnalysis.CreateAsync();

                await AnalysisRuns.ReloadCommand.ExecuteAsync();
                SelectAnalysisRow(createdAnalysis.Id);
                await LoadSelectedAnalysisDetailsAsync();

                if (AnalysisRunsChangedAsync is not null)
                {
                    await AnalysisRunsChangedAsync.Invoke();
                }
            },
            "Analysis queued successfully.");
    }

    private async Task RefreshSelectedAnalysisDetailsAsync()
    {
        await ExecuteSafeAsync(
            LoadSelectedAnalysisDetailsAsync,
            "Analysis details refreshed.");
    }

    private async Task LoadSelectedAnalysisDetailsAsync()
    {
        var selectedAnalysisRun = AnalysisRuns.SelectedAnalysisRun;

        if (selectedAnalysisRun is null)
        {
            AnalysisDetails.Clear();
            OnPropertyChanged(nameof(HasSelectedAnalysisDetails));
            RefreshCommandStates();
            return;
        }

        await AnalysisDetails.LoadAsync(selectedAnalysisRun.Id);
        OnPropertyChanged(nameof(HasSelectedAnalysisDetails));
        RefreshCommandStates();
    }

    private Task OpenSelectedSourceVideoAsync()
    {
        return ExecuteSafeAsync(
            async () => await mediaService.OpenMediaAsync(AnalysisDetails.AnalysisRun?.OriginalVideoRelativePath),
            "Source video opened in the system player.");
    }

    private Task OpenSelectedOutputVideoAsync()
    {
        return ExecuteSafeAsync(
            async () => await mediaService.OpenMediaAsync(AnalysisDetails.AnalysisRun?.OutputVideoRelativePath),
            "Annotated output video opened in the system player.");
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

    private void RefreshCommandStates()
    {
        OnPropertyChanged(nameof(CanCreateAnalysis));
        OnPropertyChanged(nameof(HasSelectedAnalysisDetails));
        OnPropertyChanged(nameof(HasSelectedAnalysisSourceVideo));
        OnPropertyChanged(nameof(HasSelectedAnalysisOutputVideo));
        OnPropertyChanged(nameof(SelectedAnalysisSourcePreview));
        OnPropertyChanged(nameof(SelectedAnalysisOutputPreview));
        OnPropertyChanged(nameof(SelectedAnalysisDetectionSummary));

        CreateAnalysisCommand.RaiseCanExecuteChanged();
        RefreshSelectedAnalysisDetailsCommand.RaiseCanExecuteChanged();
        OpenSelectedSourceVideoCommand.RaiseCanExecuteChanged();
        OpenSelectedOutputVideoCommand.RaiseCanExecuteChanged();
    }

    private void SelectAnalysisRow(Guid analysisRunId)
    {
        var matchingRow = AnalysisRuns.Browser.Rows.FirstOrDefault(row => row.Item.Id == analysisRunId);
        AnalysisRuns.SelectRow(matchingRow);
    }

    private void HandleAnalysisColumnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ObjectListColumnViewModel.IsVisible))
        {
            AnalysisRuns.Browser.ApplyCurrentColumns();
        }
    }

    private async void HandleAnalysisBrowserPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ObjectListViewModel<AnalysisRunDto>.SelectedRow))
        {
            await LoadSelectedAnalysisDetailsAsync();
        }
    }

    private void HandleCreateAnalysisPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CreateAnalysisRunViewModel.IsSaving)
            or nameof(CreateAnalysisRunViewModel.ModelName)
            or nameof(CreateAnalysisRunViewModel.SelectedVideoId)
            or nameof(CreateAnalysisRunViewModel.AvailableVideos))
        {
            OnPropertyChanged(nameof(CanCreateAnalysis));
            CreateAnalysisCommand.RaiseCanExecuteChanged();
        }
    }

    private void HandleAnalysisDetailsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AnalysisRunDetailsViewModel.AnalysisRun))
        {
            OnPropertyChanged(nameof(HasSelectedAnalysisDetails));
            OnPropertyChanged(nameof(HasSelectedAnalysisSourceVideo));
            OnPropertyChanged(nameof(HasSelectedAnalysisOutputVideo));
            OnPropertyChanged(nameof(SelectedAnalysisDetectionSummary));
            _ = LoadSelectedAnalysisPreviewsAsync();
            RefreshSelectedAnalysisDetailsCommand.RaiseCanExecuteChanged();
            OpenSelectedSourceVideoCommand.RaiseCanExecuteChanged();
            OpenSelectedOutputVideoCommand.RaiseCanExecuteChanged();
        }
    }

    private async Task LoadSelectedAnalysisPreviewsAsync()
    {
        var requestVersion = Interlocked.Increment(ref selectedAnalysisPreviewVersion);
        var analysisRun = AnalysisDetails.AnalysisRun;
        SelectedAnalysisSourcePreview.Clear();
        SelectedAnalysisOutputPreview.Clear();

        if (analysisRun is null)
        {
            if (requestVersion == selectedAnalysisPreviewVersion)
            {
                RefreshCommandStates();
            }

            return;
        }

        var sourcePreview = await mediaService.CreatePreviewAsync(
            analysisRun.OriginalVideoRelativePath,
            $"analysis-{analysisRun.Id:N}-source.png");
        var outputPreview = await mediaService.CreatePreviewAsync(
            analysisRun.OutputVideoRelativePath,
            $"analysis-{analysisRun.Id:N}-output.png");

        if (requestVersion != selectedAnalysisPreviewVersion)
        {
            sourcePreview?.Dispose();
            outputPreview?.Dispose();
            return;
        }

        SelectedAnalysisSourcePreview.SetBitmap(sourcePreview);
        SelectedAnalysisOutputPreview.SetBitmap(outputPreview);
        RefreshCommandStates();
    }
}
