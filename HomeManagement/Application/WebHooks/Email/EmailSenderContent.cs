namespace HomeManagement.Application.WebHooks.Email;

public class EmailSenderContent : ISenderContent
{
    public required string Message { get; init; }

    public string SenderContentType => "Email";
}