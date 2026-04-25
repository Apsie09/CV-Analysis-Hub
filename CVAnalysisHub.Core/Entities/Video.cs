namespace CVAnalysisHub.Core.Entities;

public sealed class Video
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string? StoredRelativePath { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public long? FileSizeBytes { get; set; }

    public TimeSpan Duration { get; set; }

    public DateTime UploadedAtUtc { get; set; }

    public string Status { get; set; } = string.Empty;

    public ICollection<AnalysisRun> AnalysisRuns { get; set; } = [];
}
