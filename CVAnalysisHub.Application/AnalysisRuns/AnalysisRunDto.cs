namespace CVAnalysisHub.Application.AnalysisRuns;

public sealed record AnalysisRunDto(
    Guid Id,
    Guid VideoId,
    string VideoName,
    string ModelName,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    string Status,
    int DetectedObjectCount,
    string? FailureReason)
{
    public bool IsActive => AnalysisRunPresentation.IsActive(Status);

    public int ProgressPercent => AnalysisRunPresentation.GetProgressPercent(Status);

    public string ProgressHeadline => AnalysisRunPresentation.GetProgressHeadline(Status);

    public string ProgressDescription => AnalysisRunPresentation.GetProgressDescription(Status);

    public string StatusCssClass => AnalysisRunPresentation.GetStatusCssClass(Status);
}
