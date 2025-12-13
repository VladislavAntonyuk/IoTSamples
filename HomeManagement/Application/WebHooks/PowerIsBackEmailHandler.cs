using MailerSendNetCore.Common.Interfaces;
using MailerSendNetCore.Emails.Dtos;
using Microsoft.Extensions.Options;

namespace HomeManagement.Application.WebHooks;

public class PowerIsBackEmailHandler(IMailerSendEmailClient client, IOptions<PowerIsBackEmailSettings> options) : IHandler
{
    public async Task<HasErrorResult> Handle()
    {
        var parameters = new MailerSendEmailParameters()
        {
            Text = "Power is back"
        };
        parameters
            .WithSubject("Power is back")
            .WithFrom(options.Value.FromEmail, options.Value.FromName)
            .WithTo(options.Value.To);

        try
        {
            var result = await client.SendEmailAsync(parameters);
            return result.Errors is null ? new HasErrorResult() : new HasErrorResult() { Error = string.Join(", ", result.Errors.Values.SelectMany(x => x)) };
        }
        catch (MailerSendNetCore.Common.Exceptions.ApiException e)
        {
            return new HasErrorResult() { Error = e.Response };
        }
    }
}