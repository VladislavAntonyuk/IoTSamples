using LiveStreamingServerNet.StreamProcessor.FFmpeg.Contracts;

namespace HomeManagement.Application.IpCameras;

class LiveCameraOutputPathResolver(string outputDir) : IFFmpegOutputPathResolver
{
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromHours(72);

    public ValueTask<string> ResolveOutputPath(IServiceProvider services, Guid contextIdentifier, string streamPath, IReadOnlyDictionary<string, string> streamArguments)
    {
        var streamDirectory = Path.Combine(outputDir, streamPath.Trim('/'));
        Directory.CreateDirectory(streamDirectory);

        DeleteOldFiles(streamDirectory);

        return ValueTask.FromResult(Path.Combine(streamDirectory, $"output-{DateTime.UtcNow:yyyyMMddHHmmss}-{contextIdentifier}.ts"));
    }

    private static void DeleteOldFiles(string streamDirectory)
    {
        var threshold = DateTime.UtcNow - RetentionPeriod;
        var files = Directory.EnumerateFiles(streamDirectory, "*.ts", SearchOption.AllDirectories)
            .Select(path => new FileInfo(path))
            .OrderBy(file => file.LastWriteTimeUtc)
            .ToList();

        if (files.Count <= 1)
        {
            return;
        }

        var remainingFiles = files.Count;
        foreach (var file in files)
        {
            if (remainingFiles <= 1)
            {
                break;
            }

            if (file.LastWriteTimeUtc < threshold)
            {
                file.Delete();
                remainingFiles--;
            }
        }
    }
}