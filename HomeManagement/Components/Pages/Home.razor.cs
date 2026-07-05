using System.Diagnostics;
using HomeManagement.Application.Router;
using HomeManagement.Infrastructure;
using HomeManagement.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace HomeManagement.Components.Pages;

public partial class Home(
     ISnackbar snackbar,
     IRouterController routerController,
     IDbContextFactory<HomeManagementDbContext> dbContextFactory) : ComponentBase
{
    private int _totalDevices;
    private string? _localIp;
    private string? _uptime;
    private string? _temperature;
    private string? _diskSpace;
    private string? _networkData;
    private string? _cpuInfo;

    protected override async Task OnInitializedAsync()
    {
        _localIp = NetworkManager.GetLocalIp();
        _uptime = GetUptime();
        _temperature = GetTemperature();
        _diskSpace = GetDiskSpace();
        _networkData = GetNetworkData();
        _cpuInfo = GetCpuInfo();
        await using var db = await dbContextFactory.CreateDbContextAsync();
        _totalDevices = await db.Devices.CountAsync();
    }

    static string? GetUptime()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "uptime",
                    Arguments = "-p",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output.Trim();
        }
        catch
        {
            return null;
        }
    }

    static string? GetTemperature()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.StartInfo.ArgumentList.Add("-c");
            process.StartInfo.ArgumentList.Add(
                "paste <(cat /sys/class/thermal/thermal_zone*/type) <(cat /sys/class/thermal/thermal_zone*/temp | awk '{print $1/1000 \"°C\"}')");

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output.Trim();
        }
        catch
        {
            return null;
        }
    }

    private string? GetDiskSpace()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "df",
                    Arguments = "-T -h",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output.Trim();
        }
        catch
        {
            return null;
        }
    }

    private string? GetNetworkData()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ip",
                    Arguments = "-s addr show",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output.Trim();
        }
        catch
        {
            return null;
        }
    }

    private string? GetCpuInfo()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cat",
                    Arguments = "/proc/cpuinfo",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output.Trim();
        }
        catch
        {
            return null;
        }
    }

    private void Reboot()
    {
        try
        {
            Process.Start("reboot");
            snackbar.Add("Rebooting...", Severity.Success);
        }
        catch (Exception e)
        {
            snackbar.Add(e.Message, Severity.Error);
        }
    }

    private void Shutdown()
    {
        try
        {
            Process.Start("poweroff");
            snackbar.Add("Shutting down...", Severity.Success);
        }
        catch (Exception e)
        {
            snackbar.Add(e.Message, Severity.Error);
        }
    }

    private string _terminalCommand = string.Empty;
    private string _terminalOutput = string.Empty;

    private async Task RunTerminalCommand()
    {
        if (string.IsNullOrWhiteSpace(_terminalCommand))
        {
            return;
        }

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-l -c \"{_terminalCommand}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            _terminalOutput =
                $"> {_terminalCommand}{Environment.NewLine}{output}{(string.IsNullOrWhiteSpace(error) ? "" : Environment.NewLine + error)}";
        }
        catch (Exception ex)
        {
            _terminalOutput = $"> {_terminalCommand}{Environment.NewLine}Error: {ex.Message}";
        }
    }

    private async Task OnTerminalKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await RunTerminalCommand();
        }
    }

    private async Task RebootRouter()
    {
        var result = await routerController.Reboot();
        if (string.IsNullOrEmpty(result.ErrorStatus))
        {
            snackbar.Add("Router rebooted successfully.", Severity.Success);
        }
        else
        {
            snackbar.Add($"Error rebooting router: {result.ErrorStatus}", Severity.Error);
        }
    }

    private async Task RebootRouterModem()
    {
        var result = await routerController.RebootModem();
        if (string.IsNullOrEmpty(result.ErrorStatus))
        {
            snackbar.Add("Router Modem rebooted successfully.", Severity.Success);
        }
        else
        {
            snackbar.Add($"Error rebooting router modem: {result.ErrorStatus}", Severity.Error);
        }
    }

    private async Task TurnOffRouterLeds()
    {
        var result = await routerController.SetLeds(false);
        if (string.IsNullOrEmpty(result.ErrorStatus))
        {
            snackbar.Add("Router LEDs turned off successfully.", Severity.Success);
        }
        else
        {
            snackbar.Add($"Error turning off router LEDs: {result.ErrorStatus}", Severity.Error);
        }
    }
}