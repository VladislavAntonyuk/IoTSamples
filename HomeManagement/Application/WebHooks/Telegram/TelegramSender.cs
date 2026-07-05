using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace HomeManagement.Application.WebHooks.Telegram;

public class TelegramSender(IOptions<TelegramSettings> options) : ISender
{
    public async Task<HasErrorResult> Send(SenderRequest request, CancellationToken token)
    {
        if (request.Content is not TelegramSenderContent telegramContent)
        {
            return new HasErrorResult { Error = $"{GetType().Name} received unsupported content type {request.Content.GetType().Name}." };
        }

        try
        {
            var client = new TelegramBotClient(options.Value.Token);
            await client.SendMessage(new ChatId(options.Value.ChatId), telegramContent.Message, cancellationToken: token);
            return new HasErrorResult();
        }
        catch (Exception e)
        {
            return new HasErrorResult() { Error = e.Message };
        }
    }
}