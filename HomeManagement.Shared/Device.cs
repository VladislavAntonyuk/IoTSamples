namespace HomeManagement.Shared;

public class Device
{
    public required string Name { get; init; }

    public required string Address { get; init; }

    public IList<DeviceAction> Actions { get; init; } = [];
}