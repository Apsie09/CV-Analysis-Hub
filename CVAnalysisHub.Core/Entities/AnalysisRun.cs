namespace CVAnalysisHub.Core.Entities;

public sealed class AnalysisRun
{
    public Guid Id { get; set; }

    public Guid VideoId { get; set; }

    public string ModelName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? OutputRelativePath { get; set; }

    public string? FailureReason { get; set; }

    public int DetectedObjectCount { get; set; }

    public Video Video { get; set; } = null!;

    public ICollection<DetectionResult> DetectionResults { get; set; } = [];
}
