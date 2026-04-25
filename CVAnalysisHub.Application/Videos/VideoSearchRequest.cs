namespace CVAnalysisHub.Application.Videos;

public sealed record VideoSearchRequest(
    string? OriginalFileNameContains,
    string? Status,
    int? MinDurationSeconds,
    int? MaxDurationSeconds,
    DateTime? UploadedFromUtc,
    DateTime? UploadedToExclusiveUtc);
