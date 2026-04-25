namespace CVAnalysisHub.Application.AnalysisRuns;

public sealed record AnalysisInferenceRequest(
    Guid AnalysisRunId,
    Guid VideoId,
    string OriginalFileName,
    string StoredFileName,
    string? SourceVideoRelativePath,
    TimeSpan Duration,
    string ModelName);
