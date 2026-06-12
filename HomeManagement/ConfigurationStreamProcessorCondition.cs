using LiveStreamingServerNet.StreamProcessor.Contracts;

public class ConfigurationStreamProcessorCondition : IStreamProcessorCondition
{
    /// <inheritdoc />
    public ValueTask<bool> IsEnabled(IServiceProvider services, string streamPath, IReadOnlyDictionary<string, string> streamArguments)
    {
        return ValueTask.FromResult(services.GetRequiredService<IConfiguration>().GetValue<bool>("StreamProcessor:Enabled"));
    }
}