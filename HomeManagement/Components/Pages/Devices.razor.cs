using System.Net.NetworkInformation;
using HomeManagement.Application.DeviceManagement;
using HomeManagement.Components.Dialogs;
using HomeManagement.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace HomeManagement.Components.Pages;

public partial class Devices(
    ISnackbar snackbar,
    IDbContextFactory<HomeManagementDbContext> dbContextFactory,
    IDialogService dialogService) : ComponentBase
{
    private MudTable<Device> _table = null!;
    private readonly Dictionary<string, DeviceStatus> _statuses = new();
    private readonly HashSet<(string DeviceName, DeviceAction Action)> _runningActions = new();

    private async Task<TableData<Device>> ServerReload(TableState state, CancellationToken token)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(token);
        var query = dbContext.Devices.Include(d => d.Actions).OrderBy(d => d.Name);

        var totalItems = await query.CountAsync(token);
        var devices = await query.Skip(state.Page * state.PageSize).Take(state.PageSize).ToListAsync(token);

        // Start/refresh status fetch for current page devices
        _ = UpdateStatusesAsync(devices, token);

        return new TableData<Device>() { TotalItems = totalItems, Items = devices };
    }

    private async Task UpdateStatusesAsync(IEnumerable<Device> devices, CancellationToken token)
    {
        var tasks = new List<Task>();
        foreach (var device in devices)
        {
            if (!_statuses.TryGetValue(device.Name, out var st))
            {
                st = new DeviceStatus();
                _statuses[device.Name] = st;
            }

            st.Loading = true;
            tasks.Add(UpdateDeviceStatusAsync(device, st, token));
        }

        StateHasChanged();
        await Task.WhenAll(tasks);
        StateHasChanged();
    }

    private async Task UpdateDeviceStatusAsync(Device device, DeviceStatus status, CancellationToken token)
    {
        try
        {
            var info = await NetworkManager.GetDeviceInfoAsync(device.Ip, token);
            if (info is not null)
            {
                status.Online = true;
                status.UptimeSeconds = info.UptimeSeconds;
            }
            else
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(device.Ip, 250);
                status.Online = reply.Status == IPStatus.Success;
                status.UptimeSeconds = null;
            }
        }
        catch
        {
            status.Online = false;
            status.UptimeSeconds = null;
        }
        finally
        {
            status.Loading = false;
        }
    }

    private DeviceStatus? GetStatus(string name) => _statuses.TryGetValue(name, out var st) ? st : null;

    private async Task RunAction(Device device, DeviceAction action)
    {
        var key = (device.Name, action);
        if (!_runningActions.Add(key))
        {
            return;
        }

        try
        {
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri($"http://{device.Ip}");
            var result = action.CommandType switch
            {
                CommandType.Get => await httpClient.GetAsync($"{action.Command}?{action.CommandArgs}"),
                CommandType.Post => await httpClient.PostAsync(action.Command,
                    string.IsNullOrWhiteSpace(action.CommandArgs) ? null : new StringContent(action.CommandArgs)),
                _ => throw new ArgumentOutOfRangeException()
            };

            snackbar.Add(await result.Content.ReadAsStringAsync(),
                result.IsSuccessStatusCode ? Severity.Success : Severity.Error);
        }
        catch (Exception ex)
        {
            snackbar.Add(ex.Message, Severity.Error);
        }
        finally
        {
            _runningActions.Remove(key);
        }
    }

    public static string FormatDuration(int? seconds)
    {
        if (seconds is null)
        {
            return string.Empty;
        }

        var ts = TimeSpan.FromSeconds(seconds.Value);
        var parts = new List<string>();
        if (ts.Days > 0)
        {
            parts.Add($"{ts.Days}d");
        }

        if (ts.Hours > 0 || parts.Count > 0)
        {
            parts.Add($"{ts.Hours}h");
        }

        if (ts.Minutes > 0 || parts.Count > 0)
        {
            parts.Add($"{ts.Minutes}m");
        }

        parts.Add($"{ts.Seconds}s");
        return string.Join(" ", parts);
    }

    private async Task ScanNetworkDevices()
    {
        var dialog = await dialogService.ShowAsync<ScanDevicesDialog>("Scan Network");
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        var devices = (List<NetworkDevice>)result.Data!;
        if (devices.Count == 0)
        {
            return;
        }

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            foreach (var device in devices)
            {
                dbContext.Devices.Add(device);
            }

            await dbContext.SaveChangesAsync();
            snackbar.Add($"Added {devices.Count} device(s).", Severity.Success);
            await _table.ReloadServerData();
        }
        catch (Exception ex)
        {
            snackbar.Add($"Failed to add scanned devices: {ex.InnerException?.Message ?? ex.Message}", Severity.Error);
        }
    }

    private async Task Add()
    {
        var dialog = await dialogService.ShowAsync<IpAddressDialog>("Enter Address");
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        var device = (NetworkDevice)result.Data!;
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            dbContext.Devices.Add(device);
            await dbContext.SaveChangesAsync();

            snackbar.Add($"Device '{device.Name}' added.", Severity.Success);
            await _table.ReloadServerData();
        }
        catch (Exception ex)
        {
            snackbar.Add($"Failed to add device: {ex.Message}", Severity.Error);
        }
    }

    private async Task Edit(Device device)
    {
        var parameters = new DialogParameters
        {
            ["Model"] = new DeviceEditModel
            {
                Name = device.Name,
                Ip = device.Ip,
                Actions = device.Actions.Select(a => new DeviceActionEditModel
                {
                    Action = a.Action,
                    Command = a.Command,
                    CommandArgs = a.CommandArgs,
                    CommandType = a.CommandType
                }).ToList()
            }
        };
        var dialog = await dialogService.ShowAsync<DeviceEditDialog>($"Edit {device.Name}", parameters);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        var model = (DeviceEditModel)result.Data!;
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var existing = await dbContext.Devices.FirstOrDefaultAsync(d => d.Name == device.Name);
            if (existing is null)
            {
                snackbar.Add("Device not found.", Severity.Error);
                return;
            }

            dbContext.Devices.Remove(existing);
            var replacement = new Device()
            {
                Name = model.Name,
                Ip = model.Ip,
                Actions = model.Actions.Select(a => new DeviceAction(a.Action, a.CommandType, a.Command, a.CommandArgs))
                    .ToList()
            };
            dbContext.Devices.Add(replacement);
            await dbContext.SaveChangesAsync();

            snackbar.Add("Device updated.", Severity.Success);
            await _table.ReloadServerData();
        }
        catch (Exception ex)
        {
            snackbar.Add($"Failed to update device: {ex.Message}", Severity.Error);
        }
    }

    private async Task Delete(Device device)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var existing = await dbContext.Devices.FirstOrDefaultAsync(d => d.Name == device.Name);
            if (existing is null)
            {
                snackbar.Add("Device not found in DB.", Severity.Warning);
                return;
            }

            dbContext.Devices.Remove(existing);
            await dbContext.SaveChangesAsync();
            snackbar.Add($"Deleted {device.Name}.", Severity.Success);
            await _table.ReloadServerData();
        }
        catch (Exception ex)
        {
            snackbar.Add($"Failed to delete device: {ex.Message}", Severity.Error);
        }
    }

    private class DeviceStatus
    {
        public bool Online { get; set; }
        public int? UptimeSeconds { get; set; }
        public bool Loading { get; set; } = true;
    }
}