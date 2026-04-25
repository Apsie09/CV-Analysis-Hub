namespace CVAnalysisHub.Application.AnalysisRuns;

public interface IAnalysisInferenceEngine
{
    Task<AnalysisInferenceResult> RunAsync(
        AnalysisInferenceRequest request,
        CancellationToken cancellationToken = default);
}
