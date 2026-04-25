using System.ComponentModel.DataAnnotations;
using CVAnalysisHub.Application.Common.ViewModels;
using CVAnalysisHub.Application.Videos;

namespace CVAnalysisHub.Application.AnalysisRuns;

public sealed class CreateAnalysisRunViewModel(
    IAnalysisRunService analysisRunService,
    IVideoService videoService) : ViewModelBase
{
    private IReadOnlyList<VideoDto> availableVideos = Array.Empty<VideoDto>();
    private Guid? selectedVideoId;
    private string modelName = "yolov8n.onnx";
    private bool isLoading;
    private bool isSaving;

    public IReadOnlyList<VideoDto> AvailableVideos
    {
        get => availableVideos;
        private set => SetProperty(ref availableVideos, value);
    }

    [Required(ErrorMessage = "Please select a video.")]
    public Guid? SelectedVideoId
    {
        get => selectedVideoId;
        set => SetProperty(ref selectedVideoId, value);
    }

    [Required(ErrorMessage = "Model name is required.")]
    [StringLength(128, ErrorMessage = "Model name must be 128 characters or fewer.")]
    public string ModelName
    {
        get => modelName;
        set => SetProperty(ref modelName, value);
    }

    public bool IsLoading
    {
        get => isLoading;
        private set => SetProperty(ref isLoading, value);
    }

    public bool IsSaving
    {
        get => isSaving;
        private set => SetProperty(ref isSaving, value);
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;

        try
        {
            AvailableVideos = await videoService.GetAllAsync(cancellationToken);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void SelectVideo(Guid videoId)
    {
        if (AvailableVideos.Any(video => video.Id == videoId))
        {
            SelectedVideoId = videoId;
        }
    }

    public async Task<AnalysisRunDto> CreateAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedVideoId is null)
        {
            throw new InvalidOperationException("A video must be selected before creating an analysis run.");
        }

        IsSaving = true;

        try
        {
            var analysisRun = await analysisRunService.CreateAsync(
                new CreateAnalysisRunRequest(SelectedVideoId.Value, ModelName.Trim()),
                cancellationToken);

            Reset();

            return analysisRun;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void Reset()
    {
        SelectedVideoId = null;
        ModelName = "yolov8n.onnx";
    }
}
