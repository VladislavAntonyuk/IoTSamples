using LiveStreamingServerNet.StreamProcessor.FFmpeg.Contracts;

namespace HomeManagement.Application.IpCameras;

class LiveCameraOutputPathResolver(string outputDir) : IFFmpegOutputPathResolver
{
    public ValueTask<string> ResolveOutputPath(IServiceProvider services, Guid contextIdentifier, string streamPath, IReadOnlyDictionary<string, string> streamArguments)
    {
        return ValueTask.FromResult(Path.Combine(outputDir, streamPath.Trim('/'), $"output-{DateTime.UtcNow:yyyyMMddHHmmss}-{contextIdentifier}.ts"));
    }
}