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