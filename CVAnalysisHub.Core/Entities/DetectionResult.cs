namespace CVAnalysisHub.Core.Entities;

public sealed class DetectionResult
{
    public Guid Id { get; set; }

    public Guid AnalysisRunId { get; set; }

    public int FrameNumber { get; set; }

    public string Label { get; set; } = string.Empty;

    public double Confidence { get; set; }

    public double X { get; set; }

    public double Y { get; set; }

    public double Width { get; set; }

    public double Height { get; set; }

    public AnalysisRun AnalysisRun { get; set; } = null!;
}
