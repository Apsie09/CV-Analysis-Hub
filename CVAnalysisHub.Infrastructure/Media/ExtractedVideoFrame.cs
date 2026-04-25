namespace CVAnalysisHub.Infrastructure.Media;

public sealed record ExtractedVideoFrame(
    int SequenceNumber,
    int FrameNumber,
    string PhysicalPath);
