namespace HomeManagement.Components.Dialogs;

public class SelectableDevice
{
    public required NetworkDevice Device { get; init; }
    public bool Selected { get; set; }
}