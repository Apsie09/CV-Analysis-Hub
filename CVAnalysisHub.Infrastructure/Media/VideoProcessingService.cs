using System.Diagnostics;
using System.Globalization;
using SkiaSharp;

namespace CVAnalysisHub.Infrastructure.Media;

public sealed class VideoProcessingService(VideoProcessingOptions options)
{
    public VideoProcessingWorkspace CreateWorkspace(Guid analysisRunId)
    {
        var rootDirectory = Path.Combine(
            Path.GetTempPath(),
            "CVAnalysisHub",
            "processing",
            analysisRunId.ToString("N"));
        var extractedFramesDirectory = Path.Combine(rootDirectory, "frames");
        var annotatedFramesDirectory = Path.Combine(rootDirectory, "annotated");

        Directory.CreateDirectory(extractedFramesDirectory);
        Directory.CreateDirectory(annotatedFramesDirectory);

        return new VideoProcessingWorkspace(
            rootDirectory,
            extractedFramesDirectory,
            annotatedFramesDirectory);
    }

    public async Task<IReadOnlyList<ExtractedVideoFrame>> ExtractFramesAsync(
        string sourceVideoPath,
        VideoProcessingWorkspace workspace,
        int frameSampleIntervalSeconds,
        int maxFramesPerRun,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceVideoPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frameSampleIntervalSeconds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFramesPerRun);

        var outputPattern = Path.Combine(workspace.ExtractedFramesDirectory, "frame-%06d.png");
        var sampleRateExpression = frameSampleIntervalSeconds == 1
            ? "1"
            : $"1/{frameSampleIntervalSeconds.ToString(CultureInfo.InvariantCulture)}";

        await RunProcessAsync(
            options.FfmpegPath,
            [
                "-hide_banner",
                "-loglevel", "error",
                "-y",
                "-i", sourceVideoPath,
                "-vf", $"fps={sampleRateExpression}",
                "-frames:v", maxFramesPerRun.ToString(CultureInfo.InvariantCulture),
                outputPattern
            ],
            cancellationToken,
            "extract sampled video frames");

        var frameFiles = Directory.GetFiles(workspace.ExtractedFramesDirectory, "frame-*.png")
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (frameFiles.Length == 0)
        {
            throw new InvalidOperationException("No frames were extracted from the uploaded video.");
        }

        return frameFiles
            .Select((path, index) => new ExtractedVideoFrame(
                SequenceNumber: index + 1,
                FrameNumber: index * frameSampleIntervalSeconds,
                PhysicalPath: path))
            .ToArray();
    }

    public async Task<string> SaveAnnotatedFrameAsync(
        SKBitmap bitmap,
        VideoProcessingWorkspace workspace,
        int sequenceNumber,
        CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(workspace.AnnotatedFramesDirectory, $"annotated-{sequenceNumber:000000}.png");
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        await File.WriteAllBytesAsync(filePath, data.ToArray(), cancellationToken);

        return filePath;
    }

    public async Task RenderAnnotatedVideoAsync(
        VideoProcessingWorkspace workspace,
        string outputPhysicalPath,
        int frameSampleIntervalSeconds,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPhysicalPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frameSampleIntervalSeconds);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPhysicalPath)!);

        var framePattern = Path.Combine(workspace.AnnotatedFramesDirectory, "annotated-%06d.png");
        var outputFrameRate = frameSampleIntervalSeconds == 1
            ? "1"
            : $"1/{frameSampleIntervalSeconds.ToString(CultureInfo.InvariantCulture)}";

        await RunProcessAsync(
            options.FfmpegPath,
            [
                "-hide_banner",
                "-loglevel", "error",
                "-y",
                "-framerate", outputFrameRate,
                "-i", framePattern,
                "-c:v", "libx264",
                "-pix_fmt", "yuv420p",
                "-movflags", "+faststart",
                outputPhysicalPath
            ],
            cancellationToken,
            "render annotated output video");
    }

    public async Task GeneratePreviewFrameAsync(
        string sourceVideoPath,
        string outputImagePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceVideoPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputImagePath);

        Directory.CreateDirectory(Path.GetDirectoryName(outputImagePath)!);

        await RunProcessAsync(
            options.FfmpegPath,
            [
                "-hide_banner",
                "-loglevel", "error",
                "-y",
                "-i", sourceVideoPath,
                "-vf", "select=eq(n\\,0),scale=960:-1:force_original_aspect_ratio=decrease",
                "-frames:v", "1",
                outputImagePath
            ],
            cancellationToken,
            "render a video preview frame");
    }

    public void CleanupWorkspace(VideoProcessingWorkspace workspace)
    {
        if (Directory.Exists(workspace.RootDirectory))
        {
            Directory.Delete(workspace.RootDirectory, recursive: true);
        }
    }

    private static async Task RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken,
        string operationDescription)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start the external process used to {operationDescription}.");
        }

        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
            }
        });

        var standardErrorTask = process.StandardError.ReadToEndAsync();
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();

        await process.WaitForExitAsync(cancellationToken);

        var standardError = await standardErrorTask;
        var standardOutput = await standardOutputTask;

        if (process.ExitCode == 0)
        {
            return;
        }

        var details = string.IsNullOrWhiteSpace(standardError)
            ? standardOutput
            : standardError;

        throw new InvalidOperationException(
            $"The external media tool failed to {operationDescription}. Details: {details.Trim()}");
    }
}
