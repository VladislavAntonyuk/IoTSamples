using HomeManagement.Shared;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;

namespace HomeManagement.Infrastructure;

[McpServerToolType]
public class HomeManagementMcpTools
{
    [McpServerTool, Description("Lists all available devices and actions")]
    public static async Task<List<Device>> ListDevices(HomeManagementDbContext dbContext, CancellationToken token)
    {
        var devices = await dbContext.Devices.Include(x => x.Actions).ToListAsync(token);
        return devices;
    }

    [McpServerTool, Description("Searches for an action by name and a device by device name, Then executes the action")]
    public static async Task<string> RunAction(string deviceName, string action, IHttpClientFactory httpClientFactory, HomeManagementDbContext dbContext, CancellationToken token)
    {
        var device = await dbContext.Devices.Include(x => x.Actions).FirstOrDefaultAsync(x => x.Name == deviceName, token);
        if (device is null)
        {
            return "Device not found";
        }

        var deviceAction = device.Actions.FirstOrDefault(x => x.Action == action);
        if (deviceAction is null)
        {
            return "Action not found";
        }

        if (PhysicalAddress.TryParse(device.Address, out _))
        {
            var output = await Process.RunAndCaptureTextAsync(new ProcessStartInfo
            {
                FileName = "/home/vladislav/.local/bin/bluetti-read",
                Arguments = $"-m {device.Address} -t {device.Name} -e true",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }, token);
            return output.ExitStatus.ExitCode == 0 ? output.StandardOutput : output.StandardError;
        }

        using var httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri($"http://{device.Address}");
        var result = deviceAction.CommandType switch
        {
            CommandType.Get => await httpClient.GetAsync($"{deviceAction.Command}?{deviceAction.CommandArgs}", token),
            CommandType.Post => await httpClient.PostAsync(deviceAction.Command,
                string.IsNullOrWhiteSpace(deviceAction.CommandArgs) ? null : new StringContent(deviceAction.CommandArgs, Encoding.Default, "application/json"), token),
            _ => throw new ArgumentOutOfRangeException()
        };

        var content = await result.Content.ReadAsStringAsync(token);
        if (string.IsNullOrEmpty(content))
        {
            content = result.IsSuccessStatusCode
                ? $"{deviceAction.Action} successfully executed"
                : "Error has occured";
        }

        return content;
    }
}