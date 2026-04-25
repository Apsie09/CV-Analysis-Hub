namespace CVAnalysisHub.Infrastructure.Media;

public sealed record VideoProcessingWorkspace(
    string RootDirectory,
    string ExtractedFramesDirectory,
    string AnnotatedFramesDirectory);
