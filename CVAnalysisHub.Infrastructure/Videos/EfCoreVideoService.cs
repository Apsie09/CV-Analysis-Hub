using CVAnalysisHub.Application.Videos;
using CVAnalysisHub.Core.Entities;
using CVAnalysisHub.Infrastructure.Media;
using CVAnalysisHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CVAnalysisHub.Infrastructure.Videos;

public sealed class EfCoreVideoService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    MediaStorageService mediaStorageService) : IVideoService
{
    public async Task<IReadOnlyList<VideoDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await SearchAsync(new VideoSearchRequest(null, null, null, null, null, null), cancellationToken);
    }

    public async Task<IReadOnlyList<VideoDto>> SearchAsync(
        VideoSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var query = dbContext.Videos
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.OriginalFileNameContains))
        {
            var searchTerm = request.OriginalFileNameContains.Trim().ToLowerInvariant();
            query = query.Where(video => video.OriginalFileName.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();
            query = query.Where(video => video.Status == status);
        }

        if (request.MinDurationSeconds is not null)
        {
            var minimumDuration = TimeSpan.FromSeconds(request.MinDurationSeconds.Value);
            query = query.Where(video => video.Duration >= minimumDuration);
        }

        if (request.MaxDurationSeconds is not null)
        {
            var maximumDuration = TimeSpan.FromSeconds(request.MaxDurationSeconds.Value);
            query = query.Where(video => video.Duration <= maximumDuration);
        }

        if (request.UploadedFromUtc is not null)
        {
            query = query.Where(video => video.UploadedAtUtc >= request.UploadedFromUtc.Value);
        }

        if (request.UploadedToExclusiveUtc is not null)
        {
            query = query.Where(video => video.UploadedAtUtc < request.UploadedToExclusiveUtc.Value);
        }

        return await query
            .OrderByDescending(video => video.UploadedAtUtc)
            .Select(ProjectToDto())
            .ToArrayAsync(cancellationToken);
    }

    public async Task<VideoDto> CreateAsync(
        CreateVideoRequest request,
        Stream content,
        IProgress<long>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OriginalFileName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.DurationSeconds);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var sanitizedOriginalFileName = request.OriginalFileName.Trim();
        var storedMediaFile = await mediaStorageService.SaveVideoAsync(
            sanitizedOriginalFileName,
            request.ContentType,
            content,
            progress,
            cancellationToken);

        var video = new Video
        {
            Id = Guid.NewGuid(),
            FileName = storedMediaFile.StoredFileName,
            StoredRelativePath = storedMediaFile.RelativePath,
            OriginalFileName = sanitizedOriginalFileName,
            ContentType = request.ContentType,
            FileSizeBytes = request.FileSizeBytes ?? storedMediaFile.SizeBytes,
            Duration = TimeSpan.FromSeconds(request.DurationSeconds),
            UploadedAtUtc = DateTime.UtcNow,
            Status = "Ready for analysis"
        };

        dbContext.Videos.Add(video);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new VideoDto(
            video.Id,
            video.FileName,
            video.StoredRelativePath,
            mediaStorageService.BuildMediaUrl(video.StoredRelativePath),
            video.OriginalFileName,
            video.ContentType,
            video.FileSizeBytes,
            video.Duration,
            video.UploadedAtUtc,
            video.Status);
    }

    private Expression<Func<Video, VideoDto>> ProjectToDto()
    {
        return video => new VideoDto(
            video.Id,
            video.FileName,
            video.StoredRelativePath,
            mediaStorageService.BuildMediaUrl(video.StoredRelativePath),
            video.OriginalFileName,
            video.ContentType,
            video.FileSizeBytes,
            video.Duration,
            video.UploadedAtUtc,
            video.Status);
    }
}
