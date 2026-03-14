using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;
using Microsoft.Extensions.Options;

namespace HomeManagement.Application.WebHooks;

public class PowerIsBackEmailHandler(IOptions<PowerIsBackEmailSettings> options) : IHandler
{
    public async Task<HasErrorResult> Handle()
    {
        MailjetClient client = new MailjetClient(options.Value.ApiKey, options.Value.ApiSecret);

        var email = new TransactionalEmailBuilder()
            .WithFrom(new SendContact(options.Value.FromEmail))
            .WithSubject("Power is back")
            .WithTextPart("Power is back")
            .WithTo(options.Value.To.Select(recipient => new SendContact(recipient)))
            .Build();

        var response = await client.SendTransactionalEmailAsync(email);
        return response.Messages.Length > 0 ? new HasErrorResult() : new HasErrorResult() { Error = "Email is not sent" };
    }
}