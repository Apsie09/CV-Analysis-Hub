using System.Diagnostics;
using Avalonia.Media.Imaging;
using CVAnalysisHub.Infrastructure.Media;

namespace CVAnalysisHub.Avalonia.Services;

public sealed class DesktopMediaService(
    MediaStorageService mediaStorageService,
    VideoProcessingService videoProcessingService)
{
    private readonly string previewCacheDirectory = Path.Combine(
        Path.GetTempPath(),
        "CVAnalysisHub",
        "desktop-previews");

    public bool HasMedia(string? relativePath)
    {
        return TryResolveMediaPath(relativePath, out _);
    }

    public async Task OpenMediaAsync(string? relativePath)
    {
        if (!TryResolveMediaPath(relativePath, out var physicalPath))
        {
            throw new InvalidOperationException("The selected media file is not available.");
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = physicalPath,
            UseShellExecute = true
        });

        await Task.CompletedTask;
    }

    public async Task<Bitmap?> CreatePreviewAsync(string? relativePath, string previewFileName)
    {
        if (!TryResolveMediaPath(relativePath, out var physicalPath))
        {
            return null;
        }

        try
        {
            var previewPath = Path.Combine(previewCacheDirectory, previewFileName);
            await videoProcessingService.GeneratePreviewFrameAsync(physicalPath, previewPath);
            await using var stream = File.OpenRead(previewPath);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    private bool TryResolveMediaPath(string? relativePath, out string physicalPath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            physicalPath = string.Empty;
            return false;
        }

        try
        {
            return mediaStorageService.TryGetPhysicalPath(relativePath, out physicalPath) &&
                File.Exists(physicalPath);
        }
        catch
        {
            physicalPath = string.Empty;
            return false;
        }
    }
}
