namespace CVAnalysisHub.Application.Videos;

public sealed record CreateVideoRequest(
    string OriginalFileName,
    int DurationSeconds,
    string? ContentType,
    long? FileSizeBytes);
