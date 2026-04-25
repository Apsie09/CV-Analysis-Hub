namespace CVAnalysisHub.Application.AnalysisRuns;

public interface IAnalysisRunProcessor
{
    Task<bool> ProcessNextQueuedAsync(CancellationToken cancellationToken = default);
}
