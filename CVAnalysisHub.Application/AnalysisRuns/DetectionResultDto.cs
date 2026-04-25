namespace CVAnalysisHub.Application.AnalysisRuns;

public sealed record DetectionResultDto(
    Guid Id,
    int FrameNumber,
    string Label,
    double Confidence,
    double X,
    double Y,
    double Width,
    double Height);
