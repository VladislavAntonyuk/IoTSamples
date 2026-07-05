using Coravel;
using Coravel.Queuing.Interfaces;
using HomeManagement.Shared;
using Innovative.SolarCalculator;
using System.Diagnostics;
using HomeManagementDeviceApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddQueue();
builder.Services.AddScheduler();

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

app.MapGet("/info", (IConfiguration configuration) => new NetworkDevice
{
    Name = configuration["DeviceName"],
    Address = NetworkManager.GetLocalIp(),
    Actions = [
        new DeviceAction("SHUTDOWN", CommandType.Post, "shutdown"),
        new DeviceAction("RESTART", CommandType.Post, "restart"),
    ],
    UptimeSeconds = DeviceManager.GetUptime(),
    Temperature = DeviceManager.GetTemperature()
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