using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;
using Microsoft.Extensions.Options;

namespace HomeManagement.Application.WebHooks.Email;

public class EmailSender(IOptions<EmailSettings> options) : ISender
{
    public async Task<HasErrorResult> Send(SenderRequest request, CancellationToken token)
    {
        if (request.Content is not EmailSenderContent emailContent)
        {
            return new HasErrorResult { Error = $"{GetType().Name} received unsupported content type {request.Content.GetType().Name}." };
        }

        MailjetClient client = new MailjetClient(options.Value.ApiKey, options.Value.ApiSecret);

        var email = new TransactionalEmailBuilder()
            .WithFrom(new SendContact(options.Value.FromEmail))
            .WithSubject("Home Management notification")
            .WithTextPart(emailContent.Message)
            .WithTo(options.Value.To.Select(recipient => new SendContact(recipient)))
            .Build();

        var response = await client.SendTransactionalEmailAsync(email);
        return response.Messages.Length > 0 ? new HasErrorResult() : new HasErrorResult { Error = "Email is not sent" };
    }
}