using CVAnalysisHub.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CVAnalysisHub.Infrastructure.Persistence;

public sealed class AppDbInitializer(IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (dbContext.Database.IsSqlite())
        {
            await EnsureVideoColumnsAsync(dbContext, cancellationToken);
            await EnsureAnalysisRunsTableAsync(dbContext, cancellationToken);
            await EnsureAnalysisRunColumnsAsync(dbContext, cancellationToken);
            await EnsureDetectionResultsTableAsync(dbContext, cancellationToken);
        }

        if (!await dbContext.Videos.AnyAsync(cancellationToken))
        {
            dbContext.Videos.AddRange(
                new Video
                {
                    Id = Guid.Parse("6b0d7d8f-9e8c-4b5d-8ce8-a9558b7d46f2"),
                    FileName = "warehouse-inspection-001.mp4",
                    OriginalFileName = "warehouse-inspection.mp4",
                    Duration = TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(14),
                    UploadedAtUtc = new DateTime(2026, 03, 20, 09, 15, 00, DateTimeKind.Utc),
                    Status = "Ready for analysis"
                },
                new Video
                {
                    Id = Guid.Parse("a4224076-1c29-4c58-9dd9-d27b0a510f79"),
                    FileName = "traffic-camera-007.mp4",
                    OriginalFileName = "traffic-junction.mp4",
                    Duration = TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(42),
                    UploadedAtUtc = new DateTime(2026, 03, 21, 13, 40, 00, DateTimeKind.Utc),
                    Status = "Queued"
                },
                new Video
                {
                    Id = Guid.Parse("f7a7ce26-b4bf-406b-bbf0-b9e0c8cc05fd"),
                    FileName = "campus-entry-003.mp4",
                    OriginalFileName = "campus-entry.mov",
                    Duration = TimeSpan.FromSeconds(58),
                    UploadedAtUtc = new DateTime(2026, 03, 22, 16, 05, 00, DateTimeKind.Utc),
                    Status = "Completed"
                });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.AnalysisRuns.AnyAsync(cancellationToken))
        {
            var warehouseVideoId = Guid.Parse("6b0d7d8f-9e8c-4b5d-8ce8-a9558b7d46f2");
            var campusVideoId = Guid.Parse("f7a7ce26-b4bf-406b-bbf0-b9e0c8cc05fd");

            dbContext.AnalysisRuns.AddRange(
                new AnalysisRun
                {
                    Id = Guid.Parse("1b490ee0-8ebc-451a-9c6c-8399e95c52a2"),
                    VideoId = warehouseVideoId,
                    ModelName = "yolov8n.onnx",
                    CreatedAtUtc = new DateTime(2026, 03, 22, 10, 00, 00, DateTimeKind.Utc),
                    Status = "Queued",
                    DetectedObjectCount = 0
                },
                new AnalysisRun
                {
                    Id = Guid.Parse("77e720e8-7bd0-4d67-934e-45db713dc966"),
                    VideoId = campusVideoId,
                    ModelName = "yolov8n.onnx",
                    CreatedAtUtc = new DateTime(2026, 03, 23, 08, 30, 00, DateTimeKind.Utc),
                    CompletedAtUtc = new DateTime(2026, 03, 23, 08, 31, 10, DateTimeKind.Utc),
                    Status = "Completed",
                    DetectedObjectCount = 3
                });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.DetectionResults.AnyAsync(cancellationToken))
        {
            var completedAnalysisId = Guid.Parse("77e720e8-7bd0-4d67-934e-45db713dc966");

            dbContext.DetectionResults.AddRange(
                new DetectionResult
                {
                    Id = Guid.Parse("12a0bd47-9a53-45d3-84eb-861d77d2d2ff"),
                    AnalysisRunId = completedAnalysisId,
                    FrameNumber = 14,
                    Label = "person",
                    Confidence = 0.94,
                    X = 124,
                    Y = 68,
                    Width = 52,
                    Height = 141
                },
                new DetectionResult
                {
                    Id = Guid.Parse("7a4ac4f9-6653-4580-98f7-94de08892d8e"),
                    AnalysisRunId = completedAnalysisId,
                    FrameNumber = 14,
                    Label = "backpack",
                    Confidence = 0.81,
                    X = 130,
                    Y = 112,
                    Width = 29,
                    Height = 36
                },
                new DetectionResult
                {
                    Id = Guid.Parse("55b7665b-234e-4b21-af4b-112be64913f3"),
                    AnalysisRunId = completedAnalysisId,
                    FrameNumber = 27,
                    Label = "person",
                    Confidence = 0.91,
                    X = 301,
                    Y = 74,
                    Width = 48,
                    Height = 136
                });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await SynchronizeSampleAnalysisObjectCountAsync(dbContext, cancellationToken);
    }

    private static async Task EnsureAnalysisRunsTableAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT 1
            FROM sqlite_master
            WHERE type = 'table' AND name = 'AnalysisRuns'
            LIMIT 1;
            """;

        var tableExists = await command.ExecuteScalarAsync(cancellationToken) is not null;

        if (tableExists)
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE "AnalysisRuns" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_AnalysisRuns" PRIMARY KEY,
                "VideoId" TEXT NOT NULL,
                "ModelName" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "CompletedAtUtc" TEXT NULL,
                "Status" TEXT NOT NULL,
                "DetectedObjectCount" INTEGER NOT NULL,
                CONSTRAINT "FK_AnalysisRuns_Videos_VideoId"
                    FOREIGN KEY ("VideoId") REFERENCES "Videos" ("Id") ON DELETE CASCADE
            );

            CREATE INDEX "IX_AnalysisRuns_VideoId" ON "AnalysisRuns" ("VideoId");
            """,
            cancellationToken);
    }

    private static async Task EnsureVideoColumnsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        await EnsureColumnAsync(dbContext, "Videos", "StoredRelativePath", "TEXT", cancellationToken);
        await EnsureColumnAsync(dbContext, "Videos", "ContentType", "TEXT", cancellationToken);
        await EnsureColumnAsync(dbContext, "Videos", "FileSizeBytes", "INTEGER", cancellationToken);
    }

    private static async Task EnsureAnalysisRunColumnsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        await EnsureColumnAsync(dbContext, "AnalysisRuns", "OutputRelativePath", "TEXT", cancellationToken);
        await EnsureColumnAsync(dbContext, "AnalysisRuns", "FailureReason", "TEXT", cancellationToken);
    }

    private static async Task EnsureDetectionResultsTableAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT 1
            FROM sqlite_master
            WHERE type = 'table' AND name = 'DetectionResults'
            LIMIT 1;
            """;

        var tableExists = await command.ExecuteScalarAsync(cancellationToken) is not null;

        if (tableExists)
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE "DetectionResults" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_DetectionResults" PRIMARY KEY,
                "AnalysisRunId" TEXT NOT NULL,
                "FrameNumber" INTEGER NOT NULL,
                "Label" TEXT NOT NULL,
                "Confidence" REAL NOT NULL,
                "X" REAL NOT NULL,
                "Y" REAL NOT NULL,
                "Width" REAL NOT NULL,
                "Height" REAL NOT NULL,
                CONSTRAINT "FK_DetectionResults_AnalysisRuns_AnalysisRunId"
                    FOREIGN KEY ("AnalysisRunId") REFERENCES "AnalysisRuns" ("Id") ON DELETE CASCADE
            );

            CREATE INDEX "IX_DetectionResults_AnalysisRunId" ON "DetectionResults" ("AnalysisRunId");
            """,
            cancellationToken);
    }

    private static async Task EnsureColumnAsync(
        AppDbContext dbContext,
        string tableName,
        string columnName,
        string columnType,
        CancellationToken cancellationToken)
    {
        var safeTableName = EnsureSafeSqlIdentifier(tableName);
        var safeColumnName = EnsureSafeSqlIdentifier(columnName);
        var safeColumnType = EnsureSafeSqlType(columnType);
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{safeTableName}\");";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        var sql = $"ALTER TABLE \"{safeTableName}\" ADD COLUMN \"{safeColumnName}\" {safeColumnType} NULL;";
        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private static string EnsureSafeSqlIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier) ||
            identifier.Any(character => !char.IsLetterOrDigit(character) && character != '_'))
        {
            throw new InvalidOperationException("Unsafe SQL identifier detected during schema initialization.");
        }

        return identifier;
    }

    private static string EnsureSafeSqlType(string columnType)
    {
        if (columnType is not "TEXT" and not "INTEGER" and not "REAL")
        {
            throw new InvalidOperationException("Unsupported SQL column type detected during schema initialization.");
        }

        return columnType;
    }

    private static async Task SynchronizeSampleAnalysisObjectCountAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var completedAnalysisId = Guid.Parse("77e720e8-7bd0-4d67-934e-45db713dc966");

        var sampleAnalysis = await dbContext.AnalysisRuns
            .SingleOrDefaultAsync(run => run.Id == completedAnalysisId, cancellationToken);

        if (sampleAnalysis is null)
        {
            return;
        }

        var storedDetectionCount = await dbContext.DetectionResults
            .CountAsync(result => result.AnalysisRunId == completedAnalysisId, cancellationToken);

        if (sampleAnalysis.DetectedObjectCount == storedDetectionCount)
        {
            return;
        }

        sampleAnalysis.DetectedObjectCount = storedDetectionCount;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
