namespace CVAnalysisHub.Application.AnalysisRuns;

public sealed record CreateAnalysisRunRequest(
    Guid VideoId,
    string ModelName);
