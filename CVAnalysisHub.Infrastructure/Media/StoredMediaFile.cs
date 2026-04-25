namespace CVAnalysisHub.Infrastructure.Media;

public sealed record StoredMediaFile(
    string StoredFileName,
    string RelativePath,
    string? ContentType,
    long SizeBytes);
