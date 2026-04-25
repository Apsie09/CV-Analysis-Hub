namespace CVAnalysisHub.Application.AnalysisRuns;

public static class AnalysisRunPresentation
{
    public static bool IsActive(string status) =>
        status is "Queued" or "Processing";

    public static int GetProgressPercent(string status) =>
        status switch
        {
            "Queued" => 20,
            "Processing" => 72,
            "Completed" => 100,
            "Failed" => 100,
            _ => 0
        };

    public static string GetProgressHeadline(string status) =>
        status switch
        {
            "Queued" => "Queued for preprocessing",
            "Processing" => "Running preprocessing and inference",
            "Completed" => "Output video and detections are ready",
            "Failed" => "Processing stopped before completion",
            _ => "Waiting for analysis updates"
        };

    public static string GetProgressDescription(string status) =>
        status switch
        {
            "Queued" => "The job is in line and will start automatically when the worker is free.",
            "Processing" => "The worker is extracting sampled frames, running the model, rendering overlays, and persisting the results.",
            "Completed" => "You can now review the stored detections and play the annotated output video preview.",
            "Failed" => "Review the failure reason and adjust the model or media configuration before retrying.",
            _ => "The analysis state is not recognized by the current presentation rules."
        };

    public static string GetStatusCssClass(string status) =>
        status switch
        {
            "Queued" => "is-queued",
            "Processing" => "is-processing",
            "Completed" => "is-completed",
            "Failed" => "is-failed",
            _ => "is-neutral"
        };
}
