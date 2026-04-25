using System.Diagnostics;
using System.ComponentModel;
using Avalonia.Media.Imaging;
using CVAnalysisHub.Application.AnalysisRuns;
using CVAnalysisHub.Application.Common.Filters;
using CVAnalysisHub.Application.Common.ObjectLists;
using CVAnalysisHub.Application.Common.ViewModels;
using CVAnalysisHub.Application.Home;
using CVAnalysisHub.Application.Videos;
using CVAnalysisHub.Infrastructure.Media;

namespace CVAnalysisHub.Avalonia.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private delegate bool MediaPathResolver(out string physicalPath);

    private string selectedUploadFileName = "No video selected";
    private string selectedUploadFileSize = string.Empty;
    private string? selectedUploadFilePath;
    private string statusMessage = "Desktop client ready to browse videos and queue analyses.";
    private string? errorMessage;
    private VideoDto? selectedCreateAnalysisVideo;
    private readonly MediaStorageService mediaStorageService;
    private readonly VideoProcessingService videoProcessingService;
    private readonly string previewCacheDirectory = Path.Combine(
        Path.GetTempPath(),
        "CVAnalysisHub",
        "desktop-previews");
    private Bitmap? selectedVideoPreview;
    private Bitmap? selectedAnalysisSourcePreview;
    private Bitmap? selectedAnalysisOutputPreview;
    private int selectedVideoPreviewVersion;
    private int selectedAnalysisPreviewVersion;

    public MainWindowViewModel(
        HomeViewModel home,
        VideoListViewModel videos,
        AnalysisRunListViewModel analysisRuns,
        CreateVideoViewModel createVideo,
        CreateAnalysisRunViewModel createAnalysis,
        AnalysisRunDetailsViewModel analysisDetails,
        MediaStorageService mediaStorageService,
        VideoProcessingService videoProcessingService)
    {
        Home = home;
        Videos = videos;
        AnalysisRuns = analysisRuns;
        CreateVideo = createVideo;
        CreateAnalysis = createAnalysis;
        AnalysisDetails = analysisDetails;
        this.mediaStorageService = mediaStorageService;
        this.videoProcessingService = videoProcessingService;

        Videos.Browser.PropertyChanged += HandleVideoBrowserPropertyChanged;
        AnalysisRuns.Browser.PropertyChanged += HandleAnalysisBrowserPropertyChanged;
        CreateVideo.PropertyChanged += HandleCreateVideoPropertyChanged;
        CreateAnalysis.PropertyChanged += HandleCreateAnalysisPropertyChanged;
        AnalysisDetails.PropertyChanged += HandleAnalysisDetailsPropertyChanged;

        foreach (var column in Videos.Browser.AvailableColumns)
        {
            column.PropertyChanged += HandleVideoColumnPropertyChanged;
        }

        foreach (var column in AnalysisRuns.Browser.AvailableColumns)
        {
            column.PropertyChanged += HandleAnalysisColumnPropertyChanged;
        }

        RefreshOverviewCommand = new AsyncDelegateCommand(RefreshOverviewAsync);
        ReloadVideosCommand = new AsyncDelegateCommand(ReloadVideosAsync);
        ApplyVideoFiltersCommand = new AsyncDelegateCommand(ApplyVideoFiltersAsync);
        ClearVideoFiltersCommand = new AsyncDelegateCommand(ClearVideoFiltersAsync);
        ClearVideosCommand = new AsyncDelegateCommand(ClearVideosAsync);
        UploadSelectedVideoCommand = new AsyncDelegateCommand(UploadSelectedVideoAsync, () => CanUploadSelectedVideo);
        ReloadAnalysisRunsCommand = new AsyncDelegateCommand(ReloadAnalysisRunsAsync);
        ApplyAnalysisFiltersCommand = new AsyncDelegateCommand(ApplyAnalysisFiltersAsync);
        ClearAnalysisFiltersCommand = new AsyncDelegateCommand(ClearAnalysisFiltersAsync);
        ClearAnalysisRunsCommand = new AsyncDelegateCommand(ClearAnalysisRunsAsync);
        CreateAnalysisCommand = new AsyncDelegateCommand(CreateAnalysisAsync, () => CanCreateAnalysis);
        RefreshSelectedAnalysisDetailsCommand = new AsyncDelegateCommand(
            RefreshSelectedAnalysisDetailsAsync,
            () => AnalysisRuns.SelectedAnalysisRun is not null);
        OpenSelectedVideoCommand = new AsyncDelegateCommand(
            OpenSelectedVideoAsync,
            () => HasSelectedVideoSource);
        OpenSelectedSourceVideoCommand = new AsyncDelegateCommand(
            OpenSelectedSourceVideoAsync,
            () => HasSelectedAnalysisSourceVideo);
        OpenSelectedOutputVideoCommand = new AsyncDelegateCommand(
            OpenSelectedOutputVideoAsync,
            () => HasSelectedAnalysisOutputVideo);
    }

    public HomeViewModel Home { get; }

    public VideoListViewModel Videos { get; }

    public AnalysisRunListViewModel AnalysisRuns { get; }

    public CreateVideoViewModel CreateVideo { get; }

    public CreateAnalysisRunViewModel CreateAnalysis { get; }

    public AnalysisRunDetailsViewModel AnalysisDetails { get; }

    public AsyncDelegateCommand RefreshOverviewCommand { get; }

    public AsyncDelegateCommand ReloadVideosCommand { get; }

    public AsyncDelegateCommand ApplyVideoFiltersCommand { get; }

    public AsyncDelegateCommand ClearVideoFiltersCommand { get; }

    public AsyncDelegateCommand ClearVideosCommand { get; }

    public AsyncDelegateCommand UploadSelectedVideoCommand { get; }

    public AsyncDelegateCommand ReloadAnalysisRunsCommand { get; }

    public AsyncDelegateCommand ApplyAnalysisFiltersCommand { get; }

    public AsyncDelegateCommand ClearAnalysisFiltersCommand { get; }

    public AsyncDelegateCommand ClearAnalysisRunsCommand { get; }

    public AsyncDelegateCommand CreateAnalysisCommand { get; }

    public AsyncDelegateCommand RefreshSelectedAnalysisDetailsCommand { get; }

    public AsyncDelegateCommand OpenSelectedVideoCommand { get; }

    public AsyncDelegateCommand OpenSelectedSourceVideoCommand { get; }

    public AsyncDelegateCommand OpenSelectedOutputVideoCommand { get; }

    public string SelectedUploadFileName
    {
        get => selectedUploadFileName;
        private set => SetProperty(ref selectedUploadFileName, value);
    }

    public string SelectedUploadFileSize
    {
        get => selectedUploadFileSize;
        private set => SetProperty(ref selectedUploadFileSize, value);
    }

    public bool HasSelectedUploadFile => !string.IsNullOrWhiteSpace(selectedUploadFilePath);

    public bool CanUploadSelectedVideo => HasSelectedUploadFile && !CreateVideo.IsSaving;

    public bool CanCreateAnalysis =>
        CreateAnalysis.SelectedVideoId.HasValue &&
        !CreateAnalysis.IsSaving &&
        !string.IsNullOrWhiteSpace(CreateAnalysis.ModelName);

    public bool HasSelectedVideoSource => TryResolveSelectedVideoSourcePath(out _);

    public bool HasSelectedAnalysisDetails => AnalysisDetails.AnalysisRun is not null;

    public bool HasSelectedAnalysisSourceVideo =>
        TryResolveSelectedAnalysisSourceVideoPath(out _);

    public bool HasSelectedAnalysisOutputVideo =>
        TryResolveSelectedAnalysisOutputVideoPath(out _);

    public Bitmap? SelectedVideoPreview
    {
        get => selectedVideoPreview;
        private set => SetPreview(
            ref selectedVideoPreview,
            value,
            nameof(SelectedVideoPreview),
            nameof(HasSelectedVideoPreview),
            nameof(MissingSelectedVideoPreview));
    }

    public bool HasSelectedVideoPreview => SelectedVideoPreview is not null;

    public bool MissingSelectedVideoPreview => !HasSelectedVideoPreview;

    public Bitmap? SelectedAnalysisSourcePreview
    {
        get => selectedAnalysisSourcePreview;
        private set => SetPreview(
            ref selectedAnalysisSourcePreview,
            value,
            nameof(SelectedAnalysisSourcePreview),
            nameof(HasSelectedAnalysisSourcePreview),
            nameof(MissingSelectedAnalysisSourcePreview));
    }

    public bool HasSelectedAnalysisSourcePreview => SelectedAnalysisSourcePreview is not null;

    public bool MissingSelectedAnalysisSourcePreview => !HasSelectedAnalysisSourcePreview;

    public Bitmap? SelectedAnalysisOutputPreview
    {
        get => selectedAnalysisOutputPreview;
        private set => SetPreview(
            ref selectedAnalysisOutputPreview,
            value,
            nameof(SelectedAnalysisOutputPreview),
            nameof(HasSelectedAnalysisOutputPreview),
            nameof(MissingSelectedAnalysisOutputPreview));
    }

    public bool HasSelectedAnalysisOutputPreview => SelectedAnalysisOutputPreview is not null;

    public bool MissingSelectedAnalysisOutputPreview => !HasSelectedAnalysisOutputPreview;

    public IReadOnlyList<string> SelectedAnalysisDetectionSummary =>
        AnalysisDetails.AnalysisRun?.DetectionResults
            .GroupBy(result => result.Label)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => $"{group.Key} {group.Count()}")
            .ToArray()
        ?? [];

    public string StatusMessage
    {
        get => statusMessage;
        private set
        {
            if (!SetProperty(ref statusMessage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public string? ErrorMessage
    {
        get => errorMessage;
        private set
        {
            if (!SetProperty(ref errorMessage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasErrorMessage));
        }
    }

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

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

    public FilterFieldViewModel VideoNameFilter => Videos.Filters.Fields[0];

    public FilterFieldViewModel VideoStatusFilter => Videos.Filters.Fields[1];

    public FilterFieldViewModel VideoDurationFilter => Videos.Filters.Fields[2];

    public FilterFieldViewModel VideoUploadedFilter => Videos.Filters.Fields[3];

    public FilterFieldViewModel AnalysisVideoNameFilter => AnalysisRuns.Filters.Fields[0];

    public FilterFieldViewModel AnalysisModelNameFilter => AnalysisRuns.Filters.Fields[1];

    public FilterFieldViewModel AnalysisStatusFilter => AnalysisRuns.Filters.Fields[2];

    public FilterFieldViewModel AnalysisDetectedObjectCountFilter => AnalysisRuns.Filters.Fields[3];

    public FilterFieldViewModel AnalysisCreatedFilter => AnalysisRuns.Filters.Fields[4];

    public IReadOnlyList<FilterOption> VideoStatusOptions => VideoStatusFilter.Options;

    public IReadOnlyList<FilterOption> AnalysisStatusOptions => AnalysisStatusFilter.Options;

    public FilterOption? SelectedVideoStatusOption
    {
        get => FindSelectedOption(VideoStatusOptions, VideoStatusFilter.PrimaryValue);
        set
        {
            var nextValue = value?.Value;

            if (string.Equals(VideoStatusFilter.PrimaryValue, nextValue, StringComparison.Ordinal))
            {
                return;
            }

            VideoStatusFilter.PrimaryValue = nextValue;
            OnPropertyChanged();
        }
    }

    public FilterOption? SelectedAnalysisStatusOption
    {
        get => FindSelectedOption(AnalysisStatusOptions, AnalysisStatusFilter.PrimaryValue);
        set
        {
            var nextValue = value?.Value;

            if (string.Equals(AnalysisStatusFilter.PrimaryValue, nextValue, StringComparison.Ordinal))
            {
                return;
            }

            AnalysisStatusFilter.PrimaryValue = nextValue;
            OnPropertyChanged();
        }
    }

    public async Task InitializeAsync()
    {
        await ExecuteSafeAsync(
            async () =>
            {
                await Home.LoadAsync();
                await Videos.LoadAsync();
                await AnalysisRuns.LoadAsync();
                await CreateAnalysis.LoadAsync();
                SyncSelectedCreateAnalysisVideo();
                await LoadSelectedVideoPreviewAsync();
                await LoadSelectedAnalysisDetailsAsync();
            },
            "Desktop client synchronized with the shared application layer.");
    }

    public void SetSelectedUploadFile(string filePath, string fileName, long fileSizeBytes)
    {
        selectedUploadFilePath = filePath;
        SelectedUploadFileName = fileName;
        SelectedUploadFileSize = FormatBytes(fileSizeBytes);
        CreateVideo.SetSelectedFile(fileName, ResolveContentType(fileName), fileSizeBytes);
        ErrorMessage = null;
        StatusMessage = $"Selected {fileName} for upload.";
        RefreshUploadState();
    }

    public void ClearSelectedUploadFile()
    {
        selectedUploadFilePath = null;
        SelectedUploadFileName = "No video selected";
        SelectedUploadFileSize = string.Empty;
        CreateVideo.ClearSelection();
        RefreshUploadState();
    }

    public void Dispose()
    {
        Videos.Browser.PropertyChanged -= HandleVideoBrowserPropertyChanged;
        AnalysisRuns.Browser.PropertyChanged -= HandleAnalysisBrowserPropertyChanged;
        CreateVideo.PropertyChanged -= HandleCreateVideoPropertyChanged;
        CreateAnalysis.PropertyChanged -= HandleCreateAnalysisPropertyChanged;
        AnalysisDetails.PropertyChanged -= HandleAnalysisDetailsPropertyChanged;

        foreach (var column in Videos.Browser.AvailableColumns)
        {
            column.PropertyChanged -= HandleVideoColumnPropertyChanged;
        }

        foreach (var column in AnalysisRuns.Browser.AvailableColumns)
        {
            column.PropertyChanged -= HandleAnalysisColumnPropertyChanged;
        }

        SelectedVideoPreview = null;
        SelectedAnalysisSourcePreview = null;
        SelectedAnalysisOutputPreview = null;
    }

    private static FilterOption? FindSelectedOption(IEnumerable<FilterOption> options, string? currentValue)
    {
        return options.FirstOrDefault(option =>
            string.Equals(option.Value, currentValue, StringComparison.Ordinal));
    }

    private async Task RefreshOverviewAsync()
    {
        await ExecuteSafeAsync(
            () => Home.LoadAsync(),
            "Overview metrics refreshed.");
    }

    private async Task ReloadVideosAsync()
    {
        await ExecuteSafeAsync(
            async () =>
            {
                await Videos.LoadAsync();
                await CreateAnalysis.LoadAsync();
                SyncSelectedCreateAnalysisVideo();
                await LoadSelectedVideoPreviewAsync();
                await Home.LoadAsync();
            },
            "Video library refreshed.");
    }

    private async Task ApplyVideoFiltersAsync()
    {
        await ExecuteSafeAsync(
            async () =>
            {
                await Videos.ApplyFiltersAsync();
                SyncSelectedCreateAnalysisVideo();
                await LoadSelectedVideoPreviewAsync();
            },
            "Video filters applied.");
    }

    private async Task ClearVideoFiltersAsync()
    {
        await ExecuteSafeAsync(
            async () =>
            {
                SelectedVideoStatusOption = null;
                await Videos.ClearFiltersAsync();
                SyncSelectedCreateAnalysisVideo();
                await LoadSelectedVideoPreviewAsync();
            },
            "Video filters cleared.");
    }

    private Task ClearVideosAsync()
    {
        Videos.ClearDisplay();
        SelectedVideoPreview = null;
        StatusMessage = "Video list cleared. Reload to bind a fresh object set.";
        ErrorMessage = null;
        RefreshCommandStates();
        return Task.CompletedTask;
    }

    private async Task UploadSelectedVideoAsync()
    {
        if (!CanUploadSelectedVideo || string.IsNullOrWhiteSpace(selectedUploadFilePath))
        {
            return;
        }

        await ExecuteSafeAsync(
            async () =>
            {
                await using var stream = File.OpenRead(selectedUploadFilePath);
                var uploadedVideo = await CreateVideo.CreateAsync(stream);

                ClearSelectedUploadFile();
                await Videos.LoadAsync();
                await CreateAnalysis.LoadAsync();
                await Home.LoadAsync();

                SelectVideoRow(uploadedVideo.Id);
                SelectedCreateAnalysisVideo = CreateAnalysis.AvailableVideos
                    .FirstOrDefault(video => video.Id == uploadedVideo.Id);
            },
            "Video uploaded and added to the shared library.");
    }

    private async Task ReloadAnalysisRunsAsync()
    {
        await ExecuteSafeAsync(
            async () =>
            {
                await AnalysisRuns.LoadAsync();
                await LoadSelectedAnalysisDetailsAsync();
                await Home.LoadAsync();
            },
            "Analysis list refreshed.");
    }

    private async Task ApplyAnalysisFiltersAsync()
    {
        await ExecuteSafeAsync(
            async () =>
            {
                await AnalysisRuns.ApplyFiltersAsync();
                await LoadSelectedAnalysisDetailsAsync();
            },
            "Analysis filters applied.");
    }

    private async Task ClearAnalysisFiltersAsync()
    {
        await ExecuteSafeAsync(
            async () =>
            {
                SelectedAnalysisStatusOption = null;
                await AnalysisRuns.ClearFiltersAsync();
                await LoadSelectedAnalysisDetailsAsync();
            },
            "Analysis filters cleared.");
    }

    private Task ClearAnalysisRunsAsync()
    {
        AnalysisRuns.ClearDisplay();
        AnalysisDetails.Clear();
        SelectedAnalysisSourcePreview = null;
        SelectedAnalysisOutputPreview = null;
        StatusMessage = "Analysis list cleared. Reload to bind a fresh object set.";
        ErrorMessage = null;
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

                await AnalysisRuns.LoadAsync();
                await Home.LoadAsync();
                SelectAnalysisRow(createdAnalysis.Id);
                await LoadSelectedAnalysisDetailsAsync();
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

    private async Task ExecuteSafeAsync(Func<Task> action, string successMessage)
    {
        ErrorMessage = null;

        try
        {
            await action();
            StatusMessage = successMessage;
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
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
        OnPropertyChanged(nameof(CanCreateAnalysis));
        OnPropertyChanged(nameof(HasSelectedVideoSource));
        OnPropertyChanged(nameof(MissingSelectedVideoPreview));
        OnPropertyChanged(nameof(HasSelectedAnalysisDetails));
        OnPropertyChanged(nameof(HasSelectedAnalysisSourceVideo));
        OnPropertyChanged(nameof(HasSelectedAnalysisOutputVideo));
        OnPropertyChanged(nameof(MissingSelectedAnalysisSourcePreview));
        OnPropertyChanged(nameof(MissingSelectedAnalysisOutputPreview));
        OnPropertyChanged(nameof(SelectedAnalysisDetectionSummary));

        UploadSelectedVideoCommand.RaiseCanExecuteChanged();
        CreateAnalysisCommand.RaiseCanExecuteChanged();
        RefreshSelectedAnalysisDetailsCommand.RaiseCanExecuteChanged();
        OpenSelectedVideoCommand.RaiseCanExecuteChanged();
        OpenSelectedSourceVideoCommand.RaiseCanExecuteChanged();
        OpenSelectedOutputVideoCommand.RaiseCanExecuteChanged();
    }

    private void SelectVideoRow(Guid videoId)
    {
        var matchingRow = Videos.Browser.Rows.FirstOrDefault(row => row.Item.Id == videoId);
        Videos.SelectRow(matchingRow);
    }

    private void SelectAnalysisRow(Guid analysisRunId)
    {
        var matchingRow = AnalysisRuns.Browser.Rows.FirstOrDefault(row => row.Item.Id == analysisRunId);
        AnalysisRuns.SelectRow(matchingRow);
    }

    private void SyncSelectedCreateAnalysisVideo()
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

        if (Videos.SelectedVideo is not null)
        {
            var selectedFromList = CreateAnalysis.AvailableVideos
                .FirstOrDefault(video => video.Id == Videos.SelectedVideo.Id);

            if (selectedFromList is not null)
            {
                SelectedCreateAnalysisVideo = selectedFromList;
                return;
            }
        }

        SelectedCreateAnalysisVideo = CreateAnalysis.AvailableVideos[0];
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

    private void HandleVideoColumnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ObjectListColumnViewModel.IsVisible))
        {
            Videos.Browser.ApplyCurrentColumns();
        }
    }

    private void HandleAnalysisColumnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ObjectListColumnViewModel.IsVisible))
        {
            AnalysisRuns.Browser.ApplyCurrentColumns();
        }
    }

    private void HandleVideoBrowserPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ObjectListViewModel<VideoDto>.SelectedRow))
        {
            SyncSelectedCreateAnalysisVideo();
            OnPropertyChanged(nameof(HasSelectedVideoSource));
            OpenSelectedVideoCommand.RaiseCanExecuteChanged();
            _ = LoadSelectedVideoPreviewAsync();
        }
    }

    private async void HandleAnalysisBrowserPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ObjectListViewModel<AnalysisRunDto>.SelectedRow))
        {
            await LoadSelectedAnalysisDetailsAsync();
        }
    }

    private void HandleCreateVideoPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CreateVideoViewModel.IsSaving) or nameof(CreateVideoViewModel.OriginalFileName))
        {
            RefreshUploadState();
        }
    }

    private void HandleCreateAnalysisPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CreateAnalysisRunViewModel.IsSaving)
            or nameof(CreateAnalysisRunViewModel.ModelName)
            or nameof(CreateAnalysisRunViewModel.SelectedVideoId)
            or nameof(CreateAnalysisRunViewModel.AvailableVideos))
        {
            SyncSelectedCreateAnalysisVideo();
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
            OpenSelectedVideoCommand.RaiseCanExecuteChanged();
            OpenSelectedSourceVideoCommand.RaiseCanExecuteChanged();
            OpenSelectedOutputVideoCommand.RaiseCanExecuteChanged();
        }
    }

    private Task OpenSelectedVideoAsync()
    {
        return OpenSelectedMediaAsync(
            TryResolveSelectedVideoSourcePath,
            "Video opened in the system player.");
    }

    private Task OpenSelectedSourceVideoAsync()
    {
        return OpenSelectedMediaAsync(
            TryResolveSelectedAnalysisSourceVideoPath,
            "Source video opened in the system player.");
    }

    private Task OpenSelectedOutputVideoAsync()
    {
        return OpenSelectedMediaAsync(
            TryResolveSelectedAnalysisOutputVideoPath,
            "Annotated output video opened in the system player.");
    }

    private Task OpenSelectedMediaAsync(
        MediaPathResolver pathResolver,
        string successMessage)
    {
        ErrorMessage = null;

        try
        {
            if (!pathResolver(out var physicalPath))
            {
                throw new InvalidOperationException("The selected media file is not available.");
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = physicalPath,
                UseShellExecute = true
            });

            StatusMessage = successMessage;
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }

        return Task.CompletedTask;
    }

    private async Task LoadSelectedVideoPreviewAsync()
    {
        var requestVersion = Interlocked.Increment(ref selectedVideoPreviewVersion);
        SelectedVideoPreview = null;

        if (!TryResolveSelectedVideoSourcePath(out var physicalPath) || Videos.SelectedVideo is null)
        {
            if (requestVersion == selectedVideoPreviewVersion)
            {
                RefreshCommandStates();
            }

            return;
        }

        var previewPath = Path.Combine(
            previewCacheDirectory,
            $"video-{Videos.SelectedVideo.Id:N}.png");
        var preview = await TryCreatePreviewBitmapAsync(physicalPath, previewPath);

        if (requestVersion != selectedVideoPreviewVersion)
        {
            preview?.Dispose();
            return;
        }

        SelectedVideoPreview = preview;
    }

    private async Task LoadSelectedAnalysisPreviewsAsync()
    {
        var requestVersion = Interlocked.Increment(ref selectedAnalysisPreviewVersion);
        var analysisRun = AnalysisDetails.AnalysisRun;
        SelectedAnalysisSourcePreview = null;
        SelectedAnalysisOutputPreview = null;

        if (analysisRun is null)
        {
            if (requestVersion == selectedAnalysisPreviewVersion)
            {
                RefreshCommandStates();
            }

            return;
        }

        var sourcePreview = await CreateAnalysisPreviewAsync(
            TryResolveSelectedAnalysisSourceVideoPath,
            $"analysis-{analysisRun.Id:N}-source.png");
        var outputPreview = await CreateAnalysisPreviewAsync(
            TryResolveSelectedAnalysisOutputVideoPath,
            $"analysis-{analysisRun.Id:N}-output.png");

        if (requestVersion != selectedAnalysisPreviewVersion)
        {
            sourcePreview?.Dispose();
            outputPreview?.Dispose();
            return;
        }

        SelectedAnalysisSourcePreview = sourcePreview;
        SelectedAnalysisOutputPreview = outputPreview;
    }

    private async Task<Bitmap?> CreateAnalysisPreviewAsync(
        MediaPathResolver pathResolver,
        string previewFileName)
    {
        if (!pathResolver(out var physicalPath))
        {
            return null;
        }

        var previewPath = Path.Combine(previewCacheDirectory, previewFileName);
        return await TryCreatePreviewBitmapAsync(physicalPath, previewPath);
    }

    private async Task<Bitmap?> TryCreatePreviewBitmapAsync(string sourceVideoPath, string previewPath)
    {
        try
        {
            await videoProcessingService.GeneratePreviewFrameAsync(sourceVideoPath, previewPath);
            await using var stream = File.OpenRead(previewPath);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    private void SetPreview(
        ref Bitmap? field,
        Bitmap? value,
        string propertyName,
        string hasPropertyName,
        string missingPropertyName)
    {
        if (ReferenceEquals(field, value))
        {
            return;
        }

        var previous = field;
        field = value;
        OnPropertyChanged(propertyName);
        OnPropertyChanged(hasPropertyName);
        OnPropertyChanged(missingPropertyName);
        previous?.Dispose();
    }

    private bool TryResolveSelectedVideoSourcePath(out string physicalPath)
    {
        var relativePath = Videos.SelectedVideo?.SourceVideoRelativePath;
        return TryResolveMediaPath(relativePath, out physicalPath);
    }

    private bool TryResolveSelectedAnalysisSourceVideoPath(out string physicalPath)
    {
        var relativePath = AnalysisDetails.AnalysisRun?.OriginalVideoRelativePath;
        return TryResolveMediaPath(relativePath, out physicalPath);
    }

    private bool TryResolveSelectedAnalysisOutputVideoPath(out string physicalPath)
    {
        var relativePath = AnalysisDetails.AnalysisRun?.OutputVideoRelativePath;
        return TryResolveMediaPath(relativePath, out physicalPath);
    }

    private bool TryResolveMediaPath(string? relativePath, out string physicalPath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            physicalPath = string.Empty;
            return false;
        }

        return mediaStorageService.TryGetPhysicalPath(relativePath, out physicalPath) &&
            File.Exists(physicalPath);
    }
}
