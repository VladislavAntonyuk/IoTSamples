namespace HomeManagement.Application.WebHooks.Telegram;

public class TelegramSettings
{
    public required string Token { get; init; }
    public required long ChatId { get; init; }
}