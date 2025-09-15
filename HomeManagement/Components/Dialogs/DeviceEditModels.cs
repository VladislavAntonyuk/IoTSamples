namespace HomeManagement.Components.Dialogs;

public class DeviceEditModel
{
    public string Name { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public List<DeviceActionEditModel> Actions { get; set; } = new();
}

public class DeviceActionEditModel
{
    public string Action { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
}
