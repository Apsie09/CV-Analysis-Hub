using CVAnalysisHub.Application.Common.ViewModels;

namespace CVAnalysisHub.Application.AnalysisRuns;

public sealed class AnalysisRunDetailsViewModel(IAnalysisRunService analysisRunService) : ViewModelBase
{
    private AnalysisRunDetailsDto? analysisRun;
    private bool isLoading;
    private DateTime? lastUpdatedAtUtc;

    public AnalysisRunDetailsDto? AnalysisRun
    {
        get => analysisRun;
        private set
        {
            if (!SetProperty(ref analysisRun, value))
            {
                return;
            }

            OnPropertyChanged(nameof(ShouldAutoRefresh));
        }
    }

    public bool IsLoading
    {
        get => isLoading;
        private set => SetProperty(ref isLoading, value);
    }

    public bool ShouldAutoRefresh => AnalysisRun?.IsActive == true;

    public DateTime? LastUpdatedAtUtc
    {
        get => lastUpdatedAtUtc;
        private set => SetProperty(ref lastUpdatedAtUtc, value);
    }

    public async Task LoadAsync(Guid analysisRunId, CancellationToken cancellationToken = default)
    {
        IsLoading = true;

        try
        {
            AnalysisRun = await analysisRunService.GetByIdAsync(analysisRunId, cancellationToken);
            LastUpdatedAtUtc = DateTime.UtcNow;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void Clear()
    {
        AnalysisRun = null;
        LastUpdatedAtUtc = null;
    }
}
