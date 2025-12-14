using System.Threading.Channels;

namespace HomeManagement.Application.WebHooks;

public class WebHookMessageProcessor(
    Channel<WebHookActions> channel,
    IServiceProvider serviceProvider,
    ILogger<WebHookMessageProcessor> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in channel.Reader.ReadAllAsync(stoppingToken))
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var services = scope.ServiceProvider.GetKeyedServices<IHandler>(message);

            foreach (var service in services)
            {
                var result = await service.Handle();
                if (!result.IsSuccessful)
                {
                    logger.LogError("Error processing webhook action {Action} with handler {Handler}: {Errors}",
                        message,
                        service.GetType().Name,
                        result.Error);
                }
            }
        }
    }
}