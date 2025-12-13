namespace HomeManagement.Application.WebHooks;

public class PowerIsBackTelegramSettings
{
    public required string Token { get; init; }
    public required long ChatId { get; init; }
}