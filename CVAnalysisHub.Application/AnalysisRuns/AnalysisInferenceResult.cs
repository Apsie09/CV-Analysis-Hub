namespace CVAnalysisHub.Application.AnalysisRuns;

public sealed record AnalysisInferenceResult(
    DateTime CompletedAtUtc,
    IReadOnlyList<AnalysisInferenceDetection> Detections,
    string? OutputRelativePath = null);
