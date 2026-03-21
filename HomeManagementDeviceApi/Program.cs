using System.Diagnostics;
using HomeManagement.Shared;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/info", (IConfiguration configuration) => new NetworkDevice
{
    Name = configuration["DeviceName"],
    Ip = NetworkManager.GetLocalIp(),
    Actions = [
        new DeviceAction("SHUTDOWN", CommandType.Post, "shutdown"),
        new DeviceAction("RESTART", CommandType.Post, "restart"),
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

app.Run();