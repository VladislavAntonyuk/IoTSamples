using System.Diagnostics;
using Coravel.Invocable;
using Microsoft.Extensions.Options;

namespace HomeManagementDeviceApi;

public class StopServiceInvocable(IOptions<CommandsSettings> commandsOptions) : IInvocable
{
    public Task Invoke()
    {
        foreach (var command in commandsOptions.Value.Commands)
        {
            Process.Start(command.StopCommand.FileName, command.StopCommand.Arguments);
        }

        return Task.CompletedTask;
    }
}