using System.Diagnostics;
using HomeManagement.Application.DeviceManagement;
using HomeManagement.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace HomeManagement.Components.Pages;

public partial class Home(
     ISnackbar snackbar,
     IDbContextFactory<HomeManagementDbContext> dbContextFactory) : ComponentBase
{
    private int _totalDevices;
    private string? _localIp;
    private string? _uptime;
    private string? _diskSpace;
    private string? _networkData;
    private string? _cpuInfo;

    protected override async Task OnInitializedAsync()
    {
        _localIp = NetworkManager.GetLocalIp();
        _uptime = GetUptime();
        _diskSpace = GetDiskSpace();
        _networkData = GetNetworkData();
        _cpuInfo = GetCpuInfo();
        await using var db = await dbContextFactory.CreateDbContextAsync();
        _totalDevices = await db.Devices.CountAsync();
    }

    private string? GetUptime()
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
            var parts = _terminalCommand.Split(' ', 2);
            var fileName = parts[0];
            var arguments = parts.Length > 1 ? parts[1] : string.Empty;
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
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
}