using LiveStreamingServerNet.StreamProcessor.FFmpeg.Contracts;

namespace HomeManagement.Application.IpCameras;

class Mp4OutputPathResolver(string outputDir) : IFFmpegOutputPathResolver
{
    public ValueTask<string> ResolveOutputPath(IServiceProvider services, Guid contextIdentifier, string streamPath, IReadOnlyDictionary<string, string> streamArguments)
    {
        return ValueTask.FromResult(Path.Combine(outputDir, streamPath, $"output-{DateTime.UtcNow:yyyyMMddHHmmss}-{contextIdentifier}.mp4"));
    }
}