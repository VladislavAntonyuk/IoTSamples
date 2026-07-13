using Coravel;
using HomeManagement.Shared;
using HomeManagementDeviceApi;
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

    scheduler.Schedule<StartServiceInvocable>()
        .AtSunrise(tzInfo, latitude, longitude)
        .PreventOverlapping("StartServiceInvocable");
    scheduler.Schedule<StopServiceInvocable>()
        .AtSunset(tzInfo, latitude, longitude)
        .PreventOverlapping("StopServiceInvocable");
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
    var result = await Process.RunAndCaptureTextAsync(command.FileName, command.Arguments.ToList());
    return result.ExitStatus.ExitCode == 0 ? result.StandardOutput : result.StandardError;
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