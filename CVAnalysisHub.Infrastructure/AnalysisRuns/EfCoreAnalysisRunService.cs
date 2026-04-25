using CVAnalysisHub.Application.AnalysisRuns;
using CVAnalysisHub.Core.Entities;
using CVAnalysisHub.Infrastructure.Media;
using CVAnalysisHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CVAnalysisHub.Infrastructure.AnalysisRuns;

public sealed class EfCoreAnalysisRunService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    MediaStorageService mediaStorageService) : IAnalysisRunService
{
    public async Task<IReadOnlyList<AnalysisRunDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await SearchAsync(
            new AnalysisRunSearchRequest(null, null, null, null, null, null, null),
            cancellationToken);
    }

    public async Task<IReadOnlyList<AnalysisRunDto>> SearchAsync(
        AnalysisRunSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var query = dbContext.AnalysisRuns
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.VideoNameContains))
        {
            var searchTerm = request.VideoNameContains.Trim().ToLowerInvariant();
            query = query.Where(run => run.Video.OriginalFileName.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(request.ModelNameContains))
        {
            var searchTerm = request.ModelNameContains.Trim().ToLowerInvariant();
            query = query.Where(run => run.ModelName.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();
            query = query.Where(run => run.Status == status);
        }

        if (request.MinDetectedObjectCount is not null)
        {
            query = query.Where(run => run.DetectedObjectCount >= request.MinDetectedObjectCount.Value);
        }

        if (request.MaxDetectedObjectCount is not null)
        {
            query = query.Where(run => run.DetectedObjectCount <= request.MaxDetectedObjectCount.Value);
        }

        if (request.CreatedFromUtc is not null)
        {
            query = query.Where(run => run.CreatedAtUtc >= request.CreatedFromUtc.Value);
        }

        if (request.CreatedToExclusiveUtc is not null)
        {
            query = query.Where(run => run.CreatedAtUtc < request.CreatedToExclusiveUtc.Value);
        }

        return await query
            .OrderByDescending(run => run.CreatedAtUtc)
            .Select(run => new AnalysisRunDto(
                run.Id,
                run.VideoId,
                run.Video.OriginalFileName,
                run.ModelName,
                run.CreatedAtUtc,
                run.CompletedAtUtc,
                run.Status,
                run.DetectedObjectCount,
                run.FailureReason))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<AnalysisRunDto> CreateAsync(CreateAnalysisRunRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ModelName);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var video = await dbContext.Videos
            .AsNoTracking()
            .SingleOrDefaultAsync(existingVideo => existingVideo.Id == request.VideoId, cancellationToken);

        if (video is null)
        {
            throw new InvalidOperationException("The selected video does not exist.");
        }

        var analysisRun = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            VideoId = request.VideoId,
            ModelName = request.ModelName.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            Status = "Queued",
            DetectedObjectCount = 0
        };

        dbContext.AnalysisRuns.Add(analysisRun);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AnalysisRunDto(
            analysisRun.Id,
            analysisRun.VideoId,
            video.OriginalFileName,
            analysisRun.ModelName,
            analysisRun.CreatedAtUtc,
            analysisRun.CompletedAtUtc,
            analysisRun.Status,
            analysisRun.DetectedObjectCount,
            analysisRun.FailureReason);
    }

    public async Task<AnalysisRunDetailsDto?> GetByIdAsync(Guid analysisRunId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.AnalysisRuns
            .AsNoTracking()
            .Where(run => run.Id == analysisRunId)
            .Select(run => new AnalysisRunDetailsDto(
                run.Id,
                run.VideoId,
                run.Video.OriginalFileName,
                run.Video.FileName,
                run.Video.StoredRelativePath,
                mediaStorageService.BuildMediaUrl(run.Video.StoredRelativePath),
                run.OutputRelativePath,
                mediaStorageService.BuildMediaUrl(run.OutputRelativePath),
                run.ModelName,
                run.CreatedAtUtc,
                run.CompletedAtUtc,
                run.Status,
                run.FailureReason,
                run.DetectedObjectCount,
                run.DetectionResults
                    .OrderBy(result => result.FrameNumber)
                    .ThenBy(result => result.Label)
                    .Select(result => new DetectionResultDto(
                        result.Id,
                        result.FrameNumber,
                        result.Label,
                        result.Confidence,
                        result.X,
                        result.Y,
                        result.Width,
                        result.Height))
                    .ToArray()))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
