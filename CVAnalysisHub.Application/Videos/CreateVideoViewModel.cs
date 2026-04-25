using System.ComponentModel.DataAnnotations;
using CVAnalysisHub.Application.Common.ViewModels;

namespace CVAnalysisHub.Application.Videos;

public sealed class CreateVideoViewModel(IVideoService videoService) : ViewModelBase
{
    private string originalFileName = string.Empty;
    private string? contentType;
    private int durationSeconds = 60;
    private long? fileSizeBytes;
    private int progressPercent;
    private string progressMessage = "Drag and drop a video to begin.";
    private bool isSaving;
    private VideoDto? uploadedVideo;

    public string? ContentType
    {
        get => contentType;
        private set => SetProperty(ref contentType, value);
    }

    [Required(ErrorMessage = "Original file name is required.")]
    [StringLength(255, ErrorMessage = "Original file name must be 255 characters or fewer.")]
    public string OriginalFileName
    {
        get => originalFileName;
        set
        {
            if (!SetProperty(ref originalFileName, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasSelectedFile));
        }
    }

    [Range(1, 86_400, ErrorMessage = "Duration must be between 1 and 86400 seconds.")]
    public int DurationSeconds
    {
        get => durationSeconds;
        set => SetProperty(ref durationSeconds, value);
    }

    public long? FileSizeBytes
    {
        get => fileSizeBytes;
        private set => SetProperty(ref fileSizeBytes, value);
    }

    public int ProgressPercent
    {
        get => progressPercent;
        private set => SetProperty(ref progressPercent, value);
    }

    public string ProgressMessage
    {
        get => progressMessage;
        private set => SetProperty(ref progressMessage, value);
    }

    public bool IsSaving
    {
        get => isSaving;
        private set => SetProperty(ref isSaving, value);
    }

    public VideoDto? UploadedVideo
    {
        get => uploadedVideo;
        private set => SetProperty(ref uploadedVideo, value);
    }

    public bool HasSelectedFile => !string.IsNullOrWhiteSpace(OriginalFileName);

    public void SetSelectedFile(string fileName, string? selectedContentType, long sizeBytes)
    {
        OriginalFileName = fileName;
        ContentType = selectedContentType;
        FileSizeBytes = sizeBytes;
        UploadedVideo = null;
        ProgressPercent = 0;
        ProgressMessage = "Ready to upload and preprocess the selected video.";
    }

    public void ClearSelection()
    {
        UploadedVideo = null;
        ProgressPercent = 0;
        ProgressMessage = "Drag and drop a video to begin.";
        ResetSelection();
    }

    public async Task<VideoDto> CreateAsync(Stream content, CancellationToken cancellationToken = default)
    {
        if (!HasSelectedFile)
        {
            throw new InvalidOperationException("A file must be selected before uploading.");
        }

        IsSaving = true;
        ProgressPercent = 0;
        ProgressMessage = "Uploading video...";

        try
        {
            var progress = new Progress<long>(bytesTransferred =>
            {
                var totalBytes = Math.Max(1L, FileSizeBytes ?? 1L);
                var percent = (int)Math.Min(90, Math.Round(bytesTransferred * 90d / totalBytes));
                ProgressPercent = percent;
                ProgressMessage = $"Uploading {OriginalFileName}...";
            });

            var createdVideo = await videoService.CreateAsync(
                new CreateVideoRequest(OriginalFileName.Trim(), DurationSeconds, ContentType, FileSizeBytes),
                content,
                progress,
                cancellationToken);

            UploadedVideo = createdVideo;
            ProgressPercent = 100;
            ProgressMessage = "Upload complete. The video is ready for analysis.";
            ResetSelection();

            return createdVideo;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void ResetSelection()
    {
        OriginalFileName = string.Empty;
        ContentType = null;
        FileSizeBytes = null;
        DurationSeconds = 60;
    }
}
