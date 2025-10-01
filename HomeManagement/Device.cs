namespace HomeManagement;

public class Device
{
    public required string Name { get; init; }

    public required string Ip { get; init; }

    public IList<DeviceAction> Actions { get; init; } = [];
}

public record DeviceAction(string Action, CommandType CommandType, string Command, string? CommandArgs = null);

public enum CommandType
{
    Get,
    Post
}