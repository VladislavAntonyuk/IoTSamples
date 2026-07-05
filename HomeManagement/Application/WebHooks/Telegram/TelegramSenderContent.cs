namespace HomeManagement.Application.WebHooks.Telegram;

public class TelegramSenderContent : ISenderContent
{
    public required string Message { get; init; }

    public string SenderContentType => "Telegram";
}