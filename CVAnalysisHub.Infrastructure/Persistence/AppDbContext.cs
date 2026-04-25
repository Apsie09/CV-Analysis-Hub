using CVAnalysisHub.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CVAnalysisHub.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AnalysisRun> AnalysisRuns => Set<AnalysisRun>();

    public DbSet<DetectionResult> DetectionResults => Set<DetectionResult>();

    public DbSet<Video> Videos => Set<Video>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Video>(entity =>
        {
            entity.ToTable("Videos");
            entity.HasKey(video => video.Id);

            entity.Property(video => video.FileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(video => video.StoredRelativePath)
                .HasMaxLength(512);

            entity.Property(video => video.OriginalFileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(video => video.ContentType)
                .HasMaxLength(128);

            entity.Property(video => video.FileSizeBytes);

            entity.Property(video => video.Duration)
                .HasConversion(
                    duration => duration.Ticks,
                    ticks => TimeSpan.FromTicks(ticks));

            entity.Property(video => video.UploadedAtUtc)
                .IsRequired();

            entity.Property(video => video.Status)
                .HasMaxLength(64)
                .IsRequired();
        });

        modelBuilder.Entity<AnalysisRun>(entity =>
        {
            entity.ToTable("AnalysisRuns");
            entity.HasKey(analysisRun => analysisRun.Id);

            entity.Property(analysisRun => analysisRun.ModelName)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(analysisRun => analysisRun.CreatedAtUtc)
                .IsRequired();

            entity.Property(analysisRun => analysisRun.Status)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(analysisRun => analysisRun.OutputRelativePath)
                .HasMaxLength(512);

            entity.Property(analysisRun => analysisRun.FailureReason)
                .HasMaxLength(2048);

            entity.Property(analysisRun => analysisRun.DetectedObjectCount)
                .IsRequired();

            entity.HasOne(analysisRun => analysisRun.Video)
                .WithMany(video => video.AnalysisRuns)
                .HasForeignKey(analysisRun => analysisRun.VideoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DetectionResult>(entity =>
        {
            entity.ToTable("DetectionResults");
            entity.HasKey(detectionResult => detectionResult.Id);

            entity.Property(detectionResult => detectionResult.Label)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(detectionResult => detectionResult.Confidence)
                .IsRequired();

            entity.Property(detectionResult => detectionResult.X)
                .IsRequired();

            entity.Property(detectionResult => detectionResult.Y)
                .IsRequired();

            entity.Property(detectionResult => detectionResult.Width)
                .IsRequired();

            entity.Property(detectionResult => detectionResult.Height)
                .IsRequired();

            entity.HasOne(detectionResult => detectionResult.AnalysisRun)
                .WithMany(analysisRun => analysisRun.DetectionResults)
                .HasForeignKey(detectionResult => detectionResult.AnalysisRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
