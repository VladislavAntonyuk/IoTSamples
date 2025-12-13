namespace HomeManagement.Application.WebHooks;

public class WebHookHandler(IServiceProvider serviceProvider)
{
    public async Task<HasErrorResult> Handle(WebHookModel model)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var services = scope.ServiceProvider.GetKeyedServices<IHandler>(model.Action);

        foreach (var service in services)
        {
            var result = await service.Handle();
            if (!result.IsSuccessful)
            {
                return result;
            }
        }

        return new HasErrorResult();
    }
}