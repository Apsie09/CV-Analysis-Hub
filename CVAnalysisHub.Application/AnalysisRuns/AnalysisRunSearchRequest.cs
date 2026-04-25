namespace CVAnalysisHub.Application.AnalysisRuns;

public sealed record AnalysisRunSearchRequest(
    string? VideoNameContains,
    string? ModelNameContains,
    string? Status,
    int? MinDetectedObjectCount,
    int? MaxDetectedObjectCount,
    DateTime? CreatedFromUtc,
    DateTime? CreatedToExclusiveUtc);
