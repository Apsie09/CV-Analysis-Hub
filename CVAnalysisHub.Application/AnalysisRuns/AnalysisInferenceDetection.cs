namespace CVAnalysisHub.Application.AnalysisRuns;

public sealed record AnalysisInferenceDetection(
    int FrameNumber,
    string Label,
    double Confidence,
    double X,
    double Y,
    double Width,
    double Height);
