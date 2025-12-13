using HomeManagement.Application.DeviceManagement;

namespace HomeManagement.Components.Dialogs;

public class NetworkDevice : Device
{
    public int UptimeSeconds { get; init; }
}