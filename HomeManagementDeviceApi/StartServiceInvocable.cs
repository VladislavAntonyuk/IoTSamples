using System.Diagnostics;
using Coravel.Invocable;
using Microsoft.Extensions.Options;

namespace HomeManagementDeviceApi;

public class StartServiceInvocable(IOptions<CommandsSettings> commandsOptions) : IInvocable
{
    public Task Invoke()
    {
        foreach (var command in commandsOptions.Value.Commands)
        {
            Process.Start(command.StartCommand.FileName, string.Join(' ', command.StartCommand.Arguments));
        }

        return Task.CompletedTask;
    }
}