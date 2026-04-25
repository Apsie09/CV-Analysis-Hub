using CVAnalysisHub.Application.Common.ViewModels;
using CVAnalysisHub.Application.Common.ObjectLists;

namespace CVAnalysisHub.Application.AnalysisRuns;

public sealed class AnalysisRunListViewModel(IAnalysisRunService analysisRunService) : ViewModelBase
{
    private static readonly ObjectListColumnDefinition<AnalysisRunDto>[] ColumnDefinitions =
    [
        new("videoName", "Video", run => run.VideoName),
        new("modelName", "Model", run => run.ModelName),
        new("createdAtUtc", "Created (UTC)", run => run.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm")),
        new("completedAtUtc", "Completed (UTC)", run => run.CompletedAtUtc?.ToString("yyyy-MM-dd HH:mm") ?? "-"),
        new("status", "Status", run => run.Status),
        new("detectedObjectCount", "Objects detected", run => run.DetectedObjectCount.ToString())
    ];

    private IReadOnlyList<AnalysisRunDto> analysisRuns = Array.Empty<AnalysisRunDto>();
    private bool isLoading;
    private DateTime? lastUpdatedAtUtc;

    public AnalysisRunFilterViewModel Filters { get; } = new();

    public ObjectListViewModel<AnalysisRunDto> Browser { get; } = new(
        ColumnDefinitions,
        ["videoName", "createdAtUtc", "status", "detectedObjectCount"]);

    public IReadOnlyList<AnalysisRunDto> AnalysisRuns
    {
        get => analysisRuns;
        private set
        {
            if (!SetProperty(ref analysisRuns, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasActiveRuns));
        }
    }

    public bool IsLoading
    {
        get => isLoading;
        private set => SetProperty(ref isLoading, value);
    }

    public bool HasActiveRuns => AnalysisRuns.Any(run => run.IsActive);

    public bool HasActiveFilters => Filters.HasActiveValues;

    public AnalysisRunDto? SelectedAnalysisRun => Browser.SelectedItem;

    public DateTime? LastUpdatedAtUtc
    {
        get => lastUpdatedAtUtc;
        private set => SetProperty(ref lastUpdatedAtUtc, value);
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;

        try
        {
            AnalysisRuns = await analysisRunService.SearchAsync(Filters.BuildRequest(), cancellationToken);
            Browser.SetItems(AnalysisRuns);
            LastUpdatedAtUtc = DateTime.UtcNow;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ClearDisplay()
    {
        AnalysisRuns = Array.Empty<AnalysisRunDto>();
        Browser.Clear();
        LastUpdatedAtUtc = null;
        OnPropertyChanged(nameof(SelectedAnalysisRun));
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

    public void SelectRow(ObjectListRowViewModel<AnalysisRunDto>? row)
    {
        Browser.SelectRow(row);
        OnPropertyChanged(nameof(SelectedAnalysisRun));
    }
}
