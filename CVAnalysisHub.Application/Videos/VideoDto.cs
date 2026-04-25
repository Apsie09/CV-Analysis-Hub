namespace CVAnalysisHub.Application.Videos;

public sealed record VideoDto(
    Guid Id,
    string FileName,
    string? SourceVideoRelativePath,
    string? SourceVideoUrl,
    string OriginalFileName,
    string? ContentType,
    long? FileSizeBytes,
    TimeSpan Duration,
    DateTime UploadedAtUtc,
    string Status);
