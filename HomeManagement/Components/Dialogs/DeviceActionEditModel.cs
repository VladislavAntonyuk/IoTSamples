using HomeManagement.Application.DeviceManagement;

namespace HomeManagement.Components.Dialogs;

public class DeviceActionEditModel
{
    public string Action { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public CommandType CommandType { get; set; } = CommandType.Get;
    public string? CommandArgs { get; set; } = null;
}