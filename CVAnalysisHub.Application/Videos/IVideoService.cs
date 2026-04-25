namespace CVAnalysisHub.Application.Videos;

public interface IVideoService
{
    Task<IReadOnlyList<VideoDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VideoDto>> SearchAsync(VideoSearchRequest request, CancellationToken cancellationToken = default);

    Task<VideoDto> CreateAsync(
        CreateVideoRequest request,
        Stream content,
        IProgress<long>? progress = null,
        CancellationToken cancellationToken = default);
}
