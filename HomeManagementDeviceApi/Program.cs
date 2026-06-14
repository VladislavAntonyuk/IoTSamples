using Coravel;
using Coravel.Invocable;
using Coravel.Queuing.Interfaces;
using HomeManagement.Shared;
using Innovative.SolarCalculator;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddQueue();
builder.Services.AddScheduler();

builder.Services.AddTransient<StartServiceInvocable>();
builder.Services.AddTransient<StopServiceInvocable>();

var app = builder.Build();

app.UseHttpsRedirection();

double latitude = 48.4647;
double longitude = 35.0462;
string timeZoneId = "Europe/Kyiv";
var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

app.Services.UseScheduler(scheduler =>
{
    // Runs once a day at midnight to plan the layout for the entire day
    scheduler.Schedule(async () =>
        {
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo);

            var solarTimes = new SolarTimes(localNow.Date, latitude, longitude);
            DateTime localSunrise = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.Sunrise.ToUniversalTime(), tzInfo);
            DateTime localSunset = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.Sunset.ToUniversalTime(), tzInfo);

            var queue = app.Services.GetRequiredService<IQueue>();

            // Schedule Sunrise Job if it hasn't passed yet today
            if (localSunrise > localNow)
            {
                TimeSpan delayUntilSunrise = localSunrise - localNow;
                queue.QueueAsyncTask(async () =>
                {
                    await Task.Delay(delayUntilSunrise);
                    var invocable = app.Services.GetRequiredService<StartServiceInvocable>();
                    await invocable.Invoke();
                });
            }

            // Schedule Sunset Job if it hasn't passed yet today
            if (localSunset > localNow)
            {
                TimeSpan delayUntilSunset = localSunset - localNow;
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
    Ip = NetworkManager.GetLocalIp(),
    Actions = [
        new DeviceAction("SHUTDOWN", CommandType.Post, "shutdown"),
        new DeviceAction("RESTART", CommandType.Post, "restart"),
        new DeviceAction("COMMAND", CommandType.Post, "command", "{\"fileName\":\"echo\",\"arguments\":[\"Hello HomeManagement\"]}"),
    ],
    UptimeSeconds = DeviceManager.GetUptime()
});
app.MapPost("/shutdown", () =>
{
    Process.Start("poweroff");
});

app.MapPost("/restart", () =>
{
    Process.Start("reboot");
});

app.MapPost("/command", (Command command) =>
{
    Process.Start(command.FileName, command.Arguments);
});

app.Run();

record Command(string FileName, IEnumerable<string> Arguments);

public class StartServiceInvocable : IInvocable
{
    public Task Invoke()
    {
        Process.Start("sudo", "systemctl start live-camera");
        return Task.CompletedTask;
    }
}

public class StopServiceInvocable : IInvocable
{
    public Task Invoke()
    {
        Process.Start("sudo", "systemctl stop live-camera");
        return Task.CompletedTask;
    }
}