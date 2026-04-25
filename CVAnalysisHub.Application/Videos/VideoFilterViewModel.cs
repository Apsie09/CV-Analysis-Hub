using CVAnalysisHub.Application.Common.Filters;

namespace CVAnalysisHub.Application.Videos;

public sealed class VideoFilterViewModel : FilterSetViewModel<VideoSearchRequest>
{
    public VideoFilterViewModel()
        : base(
        [
            new FilterFieldViewModel(
                "originalFileName",
                "Original file name",
                "Text search against the uploaded file name.",
                FilterFieldType.Text,
                "warehouse"),
            new FilterFieldViewModel(
                "status",
                "Status",
                "Select the persisted workflow state to match.",
                FilterFieldType.Select,
                options:
                [
                    new FilterOption("Ready for analysis", "Ready for analysis"),
                    new FilterOption("Queued", "Queued"),
                    new FilterOption("Completed", "Completed")
                ]),
            new FilterFieldViewModel(
                "durationSeconds",
                "Duration range",
                "Match videos whose duration in seconds falls inside the selected interval.",
                FilterFieldType.NumberRange,
                "Min seconds",
                "Max seconds"),
            new FilterFieldViewModel(
                "uploadedAtUtc",
                "Uploaded date range",
                "Find videos uploaded inside the selected UTC date interval.",
                FilterFieldType.DateRange,
                "From",
                "To")
        ])
    {
    }

    public override VideoSearchRequest BuildRequest()
    {
        return new VideoSearchRequest(
            GetPrimaryText("originalFileName"),
            GetPrimaryText("status"),
            GetPrimaryInt("durationSeconds"),
            GetSecondaryInt("durationSeconds"),
            GetPrimaryDateStartUtc("uploadedAtUtc"),
            GetSecondaryDateExclusiveUtc("uploadedAtUtc"));
    }
}
