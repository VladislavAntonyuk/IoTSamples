using System.Threading.Channels;

namespace HomeManagement.Application.WebHooks;

public class WebHookMessageProcessor(
    Channel<WebHookModel> channel,
    IServiceProvider serviceProvider,
    SenderRequestFactory senderRequestFactory,
    ILogger<WebHookMessageProcessor> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var model in channel.Reader.ReadAllAsync(stoppingToken))
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var services = scope.ServiceProvider.GetServices<ISender>();

            foreach (var service in services)
            {
                var message = senderRequestFactory.Create(model, service);
                if (message is null)
                {
                    continue;
                }

                var result = await service.Send(message, stoppingToken);
                if (!result.IsSuccessful)
                {
                    logger.LogError("Error processing webhook action {Action} with handler {Handler}: {Error}",
                        message,
                        service.GetType().Name,
                        result.Error);
                }
            }
        }
    }
}