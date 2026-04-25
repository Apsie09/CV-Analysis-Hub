using Microsoft.Extensions.Options;

namespace CVAnalysisHub.Infrastructure.Media;

public sealed class MediaStorageService(IOptions<MediaStorageOptions> options)
{
    private static readonly string[] AllowedVideoExtensions = [".mp4", ".mov", ".avi", ".mkv", ".webm"];
    private readonly string rootPath = Path.GetFullPath(options.Value.RootPath);

    public async Task<StoredMediaFile> SaveVideoAsync(
        string originalFileName,
        string? contentType,
        Stream content,
        IProgress<long>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var extension = GetSafeExtension(originalFileName);
        var safeBaseName = GetSafeBaseName(originalFileName);
        var storedFileName = $"{safeBaseName}-{Guid.NewGuid():N}{extension}";
        var relativePath = $"uploads/{storedFileName}";
        var physicalPath = GetPhysicalPath(relativePath);
        var totalBytesWritten = 0L;

        EnsureDirectory("uploads");

        await using var fileStream = new FileStream(
            physicalPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81_920,
            useAsync: true);

        var buffer = new byte[81_920];

        while (true)
        {
            var bytesRead = await content.ReadAsync(buffer, cancellationToken);

            if (bytesRead == 0)
            {
                break;
            }

            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalBytesWritten += bytesRead;
            progress?.Report(totalBytesWritten);
        }

        return new StoredMediaFile(storedFileName, relativePath, contentType, totalBytesWritten);
    }

    public async Task<string?> CreateProcessedOutputAsync(
        string? sourceRelativePath,
        string originalFileName,
        Guid analysisRunId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceRelativePath) || !TryGetPhysicalPath(sourceRelativePath, out var sourcePath))
        {
            return null;
        }

        if (!File.Exists(sourcePath))
        {
            return null;
        }

        var preparedOutput = PrepareProcessedOutputPath(originalFileName, analysisRunId, GetSafeExtension(originalFileName));

        EnsureDirectory("outputs");

        await using var sourceStream = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81_920,
            useAsync: true);
        await using var outputStream = new FileStream(
            preparedOutput.PhysicalPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81_920,
            useAsync: true);

        await sourceStream.CopyToAsync(outputStream, cancellationToken);

        return preparedOutput.RelativePath;
    }

    public PreparedMediaPath PrepareProcessedOutputPath(
        string originalFileName,
        Guid analysisRunId,
        string outputExtension = ".mp4")
    {
        var normalizedExtension = string.IsNullOrWhiteSpace(outputExtension)
            ? ".mp4"
            : outputExtension.StartsWith(".", StringComparison.Ordinal)
                ? outputExtension.ToLowerInvariant()
                : $".{outputExtension.ToLowerInvariant()}";
        var safeBaseName = GetSafeBaseName(originalFileName);
        var storedFileName = $"{safeBaseName}-analysis-{analysisRunId:N}{normalizedExtension}";
        var relativePath = $"outputs/{storedFileName}";
        var physicalPath = GetPhysicalPath(relativePath);

        EnsureDirectory("outputs");

        return new PreparedMediaPath(storedFileName, relativePath, physicalPath);
    }

    public string? BuildMediaUrl(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var segments = relativePath
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Uri.EscapeDataString);

        return "/media/" + string.Join("/", segments);
    }

    public bool TryGetPhysicalPath(string relativePath, out string physicalPath)
    {
        try
        {
            physicalPath = GetPhysicalPath(relativePath);
            return true;
        }
        catch (InvalidOperationException)
        {
            physicalPath = string.Empty;
            return false;
        }
    }

    private string GetPhysicalPath(string relativePath)
    {
        var normalizedRelativePath = relativePath
            .Replace('\\', '/')
            .TrimStart('/');
        var candidatePath = Path.GetFullPath(Path.Combine(
            rootPath,
            normalizedRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        var rootWithSeparator = rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? rootPath
            : rootPath + Path.DirectorySeparatorChar;

        if (!candidatePath.StartsWith(rootWithSeparator, StringComparison.Ordinal) &&
            !string.Equals(candidatePath, rootPath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The requested media path is outside the configured storage root.");
        }

        return candidatePath;
    }

    private void EnsureDirectory(string relativeDirectory)
    {
        Directory.CreateDirectory(GetPhysicalPath(relativeDirectory));
    }

    private static string GetSafeExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        if (string.IsNullOrWhiteSpace(extension))
        {
            return ".mp4";
        }

        var normalizedExtension = extension.ToLowerInvariant();

        return AllowedVideoExtensions.Contains(normalizedExtension)
            ? normalizedExtension
            : ".mp4";
    }

    private static string GetSafeBaseName(string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);

        if (string.IsNullOrWhiteSpace(baseName))
        {
            return "video";
        }

        var sanitized = new string(baseName
            .Trim()
            .Select(character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-')
            .ToArray());
        var collapsed = string.Join('-', sanitized.Split('-', StringSplitOptions.RemoveEmptyEntries));

        return string.IsNullOrWhiteSpace(collapsed)
            ? "video"
            : collapsed.Length > 24
                ? collapsed[..24]
                : collapsed;
    }
}
