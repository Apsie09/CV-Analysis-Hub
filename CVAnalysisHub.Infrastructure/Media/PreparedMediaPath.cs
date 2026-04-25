namespace CVAnalysisHub.Infrastructure.Media;

public sealed record PreparedMediaPath(
    string StoredFileName,
    string RelativePath,
    string PhysicalPath);
