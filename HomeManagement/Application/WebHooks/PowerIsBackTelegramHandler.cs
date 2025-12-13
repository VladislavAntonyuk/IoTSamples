using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace HomeManagement.Application.WebHooks;

public class PowerIsBackTelegramHandler(IOptions<PowerIsBackTelegramSettings> options) : IHandler
{
    public async Task<HasErrorResult> Handle()
    {
        try
        {
            var client = new TelegramBotClient(options.Value.Token);
            await client.SendMessage(new ChatId(options.Value.ChatId), "Power is back");
            return new HasErrorResult();
        }
        catch (Exception e)
        {
            return new HasErrorResult() { Error = e.Message };
        }
    }
}