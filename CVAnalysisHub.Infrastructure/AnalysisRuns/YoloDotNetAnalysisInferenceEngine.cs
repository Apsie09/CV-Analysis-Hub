using System.Reflection;
using CVAnalysisHub.Application.AnalysisRuns;
using CVAnalysisHub.Infrastructure.ComputerVision;
using CVAnalysisHub.Infrastructure.Media;
using Microsoft.Extensions.Options;
using SkiaSharp;
using YoloDotNet;
using YoloDotNet.ExecutionProvider.Cpu;
using YoloDotNet.Models;

namespace CVAnalysisHub.Infrastructure.AnalysisRuns;

public sealed class YoloDotNetAnalysisInferenceEngine(
    IOptions<ComputerVisionOptions> options,
    MediaStorageService mediaStorageService,
    VideoProcessingService videoProcessingService)
    : IAnalysisInferenceEngine, IDisposable
{
    private readonly Lock syncRoot = new();
    private Yolo? yolo;
    private bool disposed;

    public async Task<AnalysisInferenceResult> RunAsync(
        AnalysisInferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var yoloOptions = options.Value.YoloDotNet;
        var modelPath = GetRequiredPath(yoloOptions.ModelPath, "ComputerVision:YoloDotNet:ModelPath");

        if (string.IsNullOrWhiteSpace(request.SourceVideoRelativePath) ||
            !mediaStorageService.TryGetPhysicalPath(request.SourceVideoRelativePath, out var sourceVideoPath) ||
            !File.Exists(sourceVideoPath))
        {
            throw new FileNotFoundException(
                "The uploaded source video could not be found for this analysis run.",
                request.SourceVideoRelativePath);
        }

        var workspace = videoProcessingService.CreateWorkspace(request.AnalysisRunId);

        try
        {
            var extractedFrames = await videoProcessingService.ExtractFramesAsync(
                sourceVideoPath,
                workspace,
                yoloOptions.FrameSampleIntervalSeconds,
                yoloOptions.MaxFramesPerRun,
                cancellationToken);
            var allDetections = new List<AnalysisInferenceDetection>();

            foreach (var frame in extractedFrames)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var image = SKBitmap.Decode(frame.PhysicalPath)
                    ?? throw new InvalidOperationException($"Unable to decode extracted frame at '{frame.PhysicalPath}'.");

                var detections = GetOrCreateYolo(modelPath)
                    .RunObjectDetection(
                        image,
                        confidence: yoloOptions.ConfidenceThreshold,
                        iou: yoloOptions.IouThreshold)
                    .Cast<object>()
                    .Select(detection => MapDetection(detection, frame.FrameNumber))
                    .ToArray();

                DrawDetections(image, detections);
                await videoProcessingService.SaveAnnotatedFrameAsync(
                    image,
                    workspace,
                    frame.SequenceNumber,
                    cancellationToken);

                allDetections.AddRange(detections);
            }

            var preparedOutput = mediaStorageService.PrepareProcessedOutputPath(
                request.OriginalFileName,
                request.AnalysisRunId);

            await videoProcessingService.RenderAnnotatedVideoAsync(
                workspace,
                preparedOutput.PhysicalPath,
                yoloOptions.FrameSampleIntervalSeconds,
                cancellationToken);

            return new AnalysisInferenceResult(
                CompletedAtUtc: DateTime.UtcNow,
                Detections: allDetections,
                OutputRelativePath: preparedOutput.RelativePath);
        }
        finally
        {
            videoProcessingService.CleanupWorkspace(workspace);
        }
    }

    public void Dispose()
    {
        lock (syncRoot)
        {
            if (disposed)
            {
                return;
            }

            yolo?.Dispose();
            disposed = true;
        }
    }

    private Yolo GetOrCreateYolo(string modelPath)
    {
        lock (syncRoot)
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            yolo ??= new Yolo(new YoloOptions
            {
                ExecutionProvider = new CpuExecutionProvider(modelPath)
            });

            return yolo;
        }
    }

    private static string GetRequiredPath(string configuredPath, string settingName)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new InvalidOperationException($"'{settingName}' must be configured before YoloDotNet inference can run.");
        }

        var fullPath = Path.GetFullPath(configuredPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Configured file for '{settingName}' was not found.", fullPath);
        }

        return fullPath;
    }

    private static AnalysisInferenceDetection MapDetection(object detection, int frameNumber)
    {
        var labelValue = GetPropertyValue(detection, "Label");
        var boundingBox = GetPropertyValue(detection, "BoundingBox")
            ?? GetPropertyValue(detection, "Box")
            ?? GetPropertyValue(detection, "Bounds")
            ?? GetPropertyValue(detection, "Rectangle");

        return new AnalysisInferenceDetection(
            FrameNumber: frameNumber,
            Label: GetLabel(labelValue),
            Confidence: GetDouble(detection, "Confidence", "Score"),
            X: GetDouble(boundingBox, "X", "Left"),
            Y: GetDouble(boundingBox, "Y", "Top"),
            Width: GetDouble(boundingBox, "Width", "W"),
            Height: GetDouble(boundingBox, "Height", "H"));
    }

    private static void DrawDetections(SKBitmap image, IReadOnlyList<AnalysisInferenceDetection> detections)
    {
        using var canvas = new SKCanvas(image);
        using var boxPaint = new SKPaint
        {
            Color = new SKColor(34, 197, 94),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3,
            IsAntialias = true
        };
        using var labelBackgroundPaint = new SKPaint
        {
            Color = new SKColor(15, 23, 42, 220),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true
        };
        using var font = new SKFont
        {
            Size = Math.Clamp(image.Width / 36f, 18f, 34f)
        };

        foreach (var detection in detections)
        {
            var rectangle = SKRect.Create(
                x: (float)detection.X,
                y: (float)detection.Y,
                width: (float)detection.Width,
                height: (float)detection.Height);

            canvas.DrawRect(rectangle, boxPaint);

            var labelText = $"{detection.Label} {detection.Confidence:0.00}";
            font.MeasureText(labelText, out var textBounds, textPaint);
            var labelHeight = textBounds.Height + 18f;
            var labelWidth = textBounds.Width + 20f;
            var labelTop = Math.Max(0, rectangle.Top - labelHeight);
            var labelRectangle = new SKRoundRect(
                new SKRect(rectangle.Left, labelTop, rectangle.Left + labelWidth, labelTop + labelHeight),
                10,
                10);

            canvas.DrawRoundRect(labelRectangle, labelBackgroundPaint);
            canvas.DrawText(
                labelText,
                rectangle.Left + 10f,
                labelTop + labelHeight - 8f,
                SKTextAlign.Left,
                font,
                textPaint);
        }
    }

    private static string GetLabel(object? labelValue)
    {
        if (labelValue is null)
        {
            return "unknown";
        }

        if (labelValue is string text)
        {
            return text;
        }

        return GetPropertyValue(labelValue, "Name")?.ToString()
            ?? GetPropertyValue(labelValue, "Label")?.ToString()
            ?? labelValue.ToString()
            ?? "unknown";
    }

    private static double GetDouble(object? target, params string[] propertyNames)
    {
        if (target is null)
        {
            return 0;
        }

        foreach (var propertyName in propertyNames)
        {
            var value = GetPropertyValue(target, propertyName);

            if (value is null)
            {
                continue;
            }

            return Convert.ToDouble(value);
        }

        return 0;
    }

    private static object? GetPropertyValue(object target, string propertyName)
    {
        return target.GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
            ?.GetValue(target);
    }
}
