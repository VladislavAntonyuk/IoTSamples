using Coravel;
using Coravel.Queuing.Interfaces;
using HomeManagement.Shared;
using HomeManagementDeviceApi;
using Innovative.SolarCalculator;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddQueue();
builder.Services.AddScheduler();

builder.Services.AddHttpClient<MonitorInvocable>();
builder.Services.AddTransient<MonitorInvocable>();
builder.Services.AddTransient<StartServiceInvocable>();
builder.Services.AddTransient<StopServiceInvocable>();
builder.Services.Configure<CommandsSettings>(builder.Configuration.GetSection("CommandsSettings"));

var app = builder.Build();

app.UseHttpsRedirection();

double latitude = 48.4647;
double longitude = 35.0462;
string timeZoneId = "Europe/Kyiv";
var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

app.Services.UseScheduler(scheduler =>
{
    scheduler.Schedule<MonitorInvocable>()
        .EveryFiveMinutes()
        .RunOnceAtStart()
        .PreventOverlapping("Monitor");

    scheduler.Schedule(() =>
        {
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo);

            var solarTimes = new SolarTimes(localNow.Date, latitude, longitude);
            var localSunrise = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.Sunrise.ToUniversalTime(), tzInfo);
            var localSunset = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.Sunset.ToUniversalTime(), tzInfo);

            var queue = app.Services.GetRequiredService<IQueue>();
            if (localSunrise > localNow)
            {
                var delayUntilSunrise = localSunrise - localNow;
                queue.QueueAsyncTask(async () =>
                {
                    await Task.Delay(delayUntilSunrise);
                    var invocable = app.Services.GetRequiredService<StartServiceInvocable>();
                    await invocable.Invoke();
                });
            }

            if (localSunset > localNow)
            {
                var delayUntilSunset = localSunset - localNow;
                queue.QueueAsyncTask(async () =>
                {
                    await Task.Delay(delayUntilSunset);
                    var invocable = app.Services.GetRequiredService<StopServiceInvocable>();
                    await invocable.Invoke();
                });
            }
        })
        .DailyAtHour(0)
        .Zoned(tzInfo)
        .PreventOverlapping("DailySolarPlanner")
        .RunOnceAtStart();
});

app.MapGet("/info", (IConfiguration configuration, IOptions<CommandsSettings> commandsOptions) =>
{
    var actions = new List<DeviceAction>()
    {
        new ("SHUTDOWN", CommandType.Post, "shutdown"),
        new ("RESTART", CommandType.Post, "restart")
    };
    actions.AddRange(commandsOptions.Value.Commands.Select(x => new DeviceAction($"Start {x.Name}", CommandType.Post, "command", JsonSerializer.Serialize(x.StartCommand))));
    actions.AddRange(commandsOptions.Value.Commands.Select(x => new DeviceAction($"Stop {x.Name}", CommandType.Post, "command", JsonSerializer.Serialize(x.StopCommand))));

    return new NetworkDevice
    {
        Name = configuration["DeviceName"],
        Address = NetworkManager.GetLocalIp(),
        Actions = actions,
        UptimeSeconds = DeviceManager.GetUptime(),
        Temperature = DeviceManager.GetTemperature()
    };
});
app.MapPost("/command", async (Command command) =>
{
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = command.FileName,
            Arguments = string.Join(' ', command.Arguments),
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }
    };
    process.Start();
    return await process.StandardOutput.ReadToEndAsync();
});

app.MapPost("/shutdown", () =>
{
    Process.Start("poweroff");
});

app.MapPost("/restart", () =>
{
    Process.Start("reboot");
});

app.Run();