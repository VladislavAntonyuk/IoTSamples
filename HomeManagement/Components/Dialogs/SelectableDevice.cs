namespace HomeManagement.Components.Dialogs;

public class NetworkDevice : Device
{
    public int UptimeSeconds { get; init; }
}

public class SelectableDevice
{
    public required NetworkDevice Device { get; init; }
    public bool Selected { get; set; }
}