namespace CVAnalysisHub.Application.AnalysisRuns;

public sealed record AnalysisRunDetailsDto(
    Guid Id,
    Guid VideoId,
    string VideoName,
    string StoredVideoName,
    string? OriginalVideoRelativePath,
    string? OriginalVideoUrl,
    string? OutputVideoRelativePath,
    string? OutputVideoUrl,
    string ModelName,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    string Status,
    string? FailureReason,
    int DetectedObjectCount,
    IReadOnlyList<DetectionResultDto> DetectionResults)
{
    public bool IsActive => AnalysisRunPresentation.IsActive(Status);

    public int ProgressPercent => AnalysisRunPresentation.GetProgressPercent(Status);

    public string ProgressHeadline => AnalysisRunPresentation.GetProgressHeadline(Status);

    public string ProgressDescription => AnalysisRunPresentation.GetProgressDescription(Status);

    public string StatusCssClass => AnalysisRunPresentation.GetStatusCssClass(Status);
}
