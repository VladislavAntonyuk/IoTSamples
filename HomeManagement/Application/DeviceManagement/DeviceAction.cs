namespace HomeManagement.Application.DeviceManagement;

public record DeviceAction(string Action, CommandType CommandType, string Command, string? CommandArgs = null);