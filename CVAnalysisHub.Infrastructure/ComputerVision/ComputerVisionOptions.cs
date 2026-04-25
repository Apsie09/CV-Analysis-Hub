namespace CVAnalysisHub.Infrastructure.ComputerVision;

public sealed class ComputerVisionOptions
{
    public string Provider { get; set; } = "Placeholder";

    public YoloDotNetOptions YoloDotNet { get; set; } = new();
}
