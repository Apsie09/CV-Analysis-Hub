using CVAnalysisHub.Application.Common.Filters;

namespace CVAnalysisHub.Application.AnalysisRuns;

public sealed class AnalysisRunFilterViewModel : FilterSetViewModel<AnalysisRunSearchRequest>
{
    public AnalysisRunFilterViewModel()
        : base(
        [
            new FilterFieldViewModel(
                "videoName",
                "Video name",
                "Text search against the related uploaded video.",
                FilterFieldType.Text,
                "campus"),
            new FilterFieldViewModel(
                "modelName",
                "Model name",
                "Text search against the selected inference model.",
                FilterFieldType.Text,
                "yolov8n"),
            new FilterFieldViewModel(
                "status",
                "Status",
                "Select the current analysis workflow state.",
                FilterFieldType.Select,
                options:
                [
                    new FilterOption("Queued", "Queued"),
                    new FilterOption("Processing", "Processing"),
                    new FilterOption("Completed", "Completed"),
                    new FilterOption("Failed", "Failed")
                ]),
            new FilterFieldViewModel(
                "detectedObjectCount",
                "Detected objects range",
                "Restrict results by the stored object count interval.",
                FilterFieldType.NumberRange,
                "Min objects",
                "Max objects"),
            new FilterFieldViewModel(
                "createdAtUtc",
                "Created date range",
                "Find analyses created inside the selected UTC date interval.",
                FilterFieldType.DateRange,
                "From",
                "To")
        ])
    {
    }

    public override AnalysisRunSearchRequest BuildRequest()
    {
        return new AnalysisRunSearchRequest(
            GetPrimaryText("videoName"),
            GetPrimaryText("modelName"),
            GetPrimaryText("status"),
            GetPrimaryInt("detectedObjectCount"),
            GetSecondaryInt("detectedObjectCount"),
            GetPrimaryDateStartUtc("createdAtUtc"),
            GetSecondaryDateExclusiveUtc("createdAtUtc"));
    }
}
