using CVAnalysisHub.Application.AnalysisRuns;

namespace CVAnalysisHub.Infrastructure.AnalysisRuns;

public sealed class PlaceholderAnalysisInferenceEngine : IAnalysisInferenceEngine
{
    private static readonly string[] Labels = ["person", "car", "backpack", "bicycle", "dog"];

    public async Task<AnalysisInferenceResult> RunAsync(
        AnalysisInferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(750), cancellationToken);

        var seed = HashCode.Combine(
            request.AnalysisRunId,
            request.VideoId,
            request.ModelName,
            request.StoredFileName);
        var random = new Random(seed);

        var detectionCount = random.Next(1, 5);
        var detections = new List<AnalysisInferenceDetection>(detectionCount);

        for (var index = 0; index < detectionCount; index++)
        {
            detections.Add(new AnalysisInferenceDetection(
                FrameNumber: random.Next(0, Math.Max(1, (int)request.Duration.TotalSeconds)),
                Label: Labels[random.Next(Labels.Length)],
                Confidence: Math.Round(0.55 + (random.NextDouble() * 0.4), 2),
                X: Math.Round(20 + (random.NextDouble() * 400), 2),
                Y: Math.Round(20 + (random.NextDouble() * 220), 2),
                Width: Math.Round(30 + (random.NextDouble() * 120), 2),
                Height: Math.Round(30 + (random.NextDouble() * 140), 2)));
        }

        return new AnalysisInferenceResult(
            CompletedAtUtc: DateTime.UtcNow,
            Detections: detections,
            OutputRelativePath: null);
    }
}
