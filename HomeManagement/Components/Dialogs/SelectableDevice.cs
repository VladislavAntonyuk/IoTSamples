using HomeManagement.Shared;

namespace HomeManagement.Components.Dialogs;

public class SelectableDevice
{
    public required Device Device { get; init; }
    public bool Selected { get; set; }
}