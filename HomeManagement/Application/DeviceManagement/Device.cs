namespace HomeManagement.Application.DeviceManagement;

public class Device
{
    public required string Name { get; init; }

    public required string Ip { get; init; }

    public IList<DeviceAction> Actions { get; init; } = [];
}