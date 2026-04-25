namespace CVAnalysisHub.Application.AnalysisRuns;

public interface IAnalysisRunService
{
    Task<IReadOnlyList<AnalysisRunDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AnalysisRunDto>> SearchAsync(
        AnalysisRunSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<AnalysisRunDto> CreateAsync(CreateAnalysisRunRequest request, CancellationToken cancellationToken = default);

    Task<AnalysisRunDetailsDto?> GetByIdAsync(Guid analysisRunId, CancellationToken cancellationToken = default);
}
