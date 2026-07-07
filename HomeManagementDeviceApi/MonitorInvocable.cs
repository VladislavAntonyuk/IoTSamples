using Coravel.Invocable;
using HomeManagement.Application.WebHooks;
using HomeManagement.Shared;

namespace HomeManagementDeviceApi;

public class MonitorInvocable(HttpClient httpClient, IConfiguration configuration) : IInvocable
{
    public async Task Invoke()
    {
        httpClient.BaseAddress = new Uri(configuration.GetValue<string>("Host:Url"));
        httpClient.DefaultRequestHeaders.Add("key", configuration.GetValue<string>("Host:Key"));
        var temperature = DeviceManager.GetTemperature();
        if (temperature > configuration.GetValue<double>("Monitor:TemperatureThreshold"))
        {
            var result = await httpClient.PostAsJsonAsync("/api/webhook", new WebHookModel() { Message = $"Temperature exceeded threshold: {temperature}" });
            result.EnsureSuccessStatusCode();
        }
    }
}