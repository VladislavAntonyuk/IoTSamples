namespace HomeManagement;

public record Device(string Name, string Ip, int UptimeSeconds)
{
    public IList<DeviceAction> Actions { get; init; } = new List<DeviceAction>();
}

public record DeviceAction(string Action, string Command);
