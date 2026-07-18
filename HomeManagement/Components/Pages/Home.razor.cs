using HomeManagement.Application.Router;
using HomeManagement.Infrastructure;
using HomeManagement.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using System.Diagnostics;

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
        _uptime = await GetUptime();
        _temperature = await GetTemperature();
        _diskSpace = await GetDiskSpace();
        _networkData = await GetNetworkData();
        _cpuInfo = await GetCpuInfo();
        await using var db = await dbContextFactory.CreateDbContextAsync();
        _totalDevices = await db.Devices.CountAsync();
    }

    private static async Task<string> GetUptime()
    {
        var result = await Process.RunAndCaptureTextAsync("uptime",["-p"]);
        return result.ExitStatus.ExitCode == 0 ? result.StandardOutput : result.StandardError;
    }

    static async Task<string?> GetTemperature()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add(
            "paste <(cat /sys/class/thermal/thermal_zone*/type) <(cat /sys/class/thermal/thermal_zone*/temp | awk '{print $1/1000 \"°C\"}')");

        var result = await Process.RunAndCaptureTextAsync(startInfo);
        return result.ExitStatus.ExitCode == 0 ? result.StandardOutput : result.StandardError;
    }

    private async Task<string> GetDiskSpace()
    {
        var result = await Process.RunAndCaptureTextAsync("df", ["-T", "-h"]);
        return result.ExitStatus.ExitCode == 0 ? result.StandardOutput : result.StandardError;
    }
    private async Task<string> GetNetworkData()
    {
        var result = await Process.RunAndCaptureTextAsync("ip", ["-s addr show"]);
        return result.ExitStatus.ExitCode == 0 ? result.StandardOutput : result.StandardError;
    }

    private async Task<string?> GetCpuInfo()
    {
        var result = await Process.RunAndCaptureTextAsync("cat", ["/proc/cpuinfo"]);
        return result.ExitStatus.ExitCode == 0 ? result.StandardOutput : result.StandardError;
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

        var result = await Process.RunAndCaptureTextAsync("/bin/bash", [$"-l -c \"{_terminalCommand}\""]);
        _terminalOutput = $"> {_terminalCommand}{Environment.NewLine}{result.StandardOutput}{(string.IsNullOrWhiteSpace(result.StandardError) ? "" : Environment.NewLine + result.StandardError)}";
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