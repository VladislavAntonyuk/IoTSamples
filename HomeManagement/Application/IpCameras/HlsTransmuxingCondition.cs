using LiveStreamingServerNet.StreamProcessor.Contracts;

namespace HomeManagement.Application.IpCameras;

public class HlsTransmuxingCondition : IStreamProcessorCondition
{
    public ValueTask<bool> IsEnabled(IServiceProvider services, string streamPath, IReadOnlyDictionary<string, string> streamArguments)
    {
        return ValueTask.FromResult(streamArguments.GetValueOrDefault("hls", "false") == "true");
    }
}