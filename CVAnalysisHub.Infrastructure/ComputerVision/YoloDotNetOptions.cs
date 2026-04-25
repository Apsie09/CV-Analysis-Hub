namespace CVAnalysisHub.Infrastructure.ComputerVision;

public sealed class YoloDotNetOptions
{
    public string ModelPath { get; set; } = string.Empty;

    public float ConfidenceThreshold { get; set; } = 0.25f;

    public float IouThreshold { get; set; } = 0.7f;

    public int FrameSampleIntervalSeconds { get; set; } = 1;

    public int MaxFramesPerRun { get; set; } = 120;
}
