using CVAnalysisHub.Application.AnalysisRuns;
using CVAnalysisHub.Core.Entities;
using CVAnalysisHub.Infrastructure.Media;
using CVAnalysisHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CVAnalysisHub.Infrastructure.AnalysisRuns;

public sealed class EfCoreAnalysisRunProcessor(
    IDbContextFactory<AppDbContext> dbContextFactory,
    IAnalysisInferenceEngine analysisInferenceEngine,
    MediaStorageService mediaStorageService,
    ILogger<EfCoreAnalysisRunProcessor> logger) : IAnalysisRunProcessor
{
    public async Task<bool> ProcessNextQueuedAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var analysisRun = await dbContext.AnalysisRuns
            .Include(run => run.Video)
            .Include(run => run.DetectionResults)
            .Where(run => run.Status == "Queued")
            .OrderBy(run => run.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (analysisRun is null)
        {
            return false;
        }

        analysisRun.Status = "Processing";
        analysisRun.FailureReason = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var inferenceResult = await analysisInferenceEngine.RunAsync(
                new AnalysisInferenceRequest(
                    analysisRun.Id,
                    analysisRun.VideoId,
                    analysisRun.Video.OriginalFileName,
                    analysisRun.Video.FileName,
                    analysisRun.Video.StoredRelativePath,
                    analysisRun.Video.Duration,
                    analysisRun.ModelName),
                cancellationToken);

            if (analysisRun.DetectionResults.Count > 0)
            {
                dbContext.DetectionResults.RemoveRange(analysisRun.DetectionResults);
            }

            foreach (var detection in inferenceResult.Detections)
            {
                dbContext.DetectionResults.Add(new DetectionResult
                {
                    Id = Guid.NewGuid(),
                    AnalysisRunId = analysisRun.Id,
                    FrameNumber = detection.FrameNumber,
                    Label = detection.Label,
                    Confidence = detection.Confidence,
                    X = detection.X,
                    Y = detection.Y,
                    Width = detection.Width,
                    Height = detection.Height
                });
            }

            analysisRun.Status = "Completed";
            analysisRun.CompletedAtUtc = inferenceResult.CompletedAtUtc;
            analysisRun.DetectedObjectCount = inferenceResult.Detections.Count;
            analysisRun.OutputRelativePath = inferenceResult.OutputRelativePath
                ?? await mediaStorageService.CreateProcessedOutputAsync(
                    analysisRun.Video.StoredRelativePath,
                    analysisRun.Video.OriginalFileName,
                    analysisRun.Id,
                    cancellationToken);
            analysisRun.FailureReason = null;

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Analysis run {AnalysisRunId} failed during inference processing.",
                analysisRun.Id);

            if (analysisRun.DetectionResults.Count > 0)
            {
                dbContext.DetectionResults.RemoveRange(analysisRun.DetectionResults);
            }

            analysisRun.Status = "Failed";
            analysisRun.CompletedAtUtc = DateTime.UtcNow;
            analysisRun.DetectedObjectCount = 0;
            analysisRun.OutputRelativePath = null;
            analysisRun.FailureReason = exception.Message;

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}
