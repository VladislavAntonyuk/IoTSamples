using HomeManagement.Application.WebHooks.Email;
using HomeManagement.Application.WebHooks.Telegram;

namespace HomeManagement.Application.WebHooks;

public class SenderRequestFactory
{
    private static readonly Dictionary<Type, Func<WebHookModel, ISenderContent>> ContentFactoryBySenderType = new()
    {
        [typeof(TelegramSender)] = model => new TelegramSenderContent
        {
            Message = model.Message
        },
        [typeof(EmailSender)] = model => new EmailSenderContent
        {
            Message = model.Message
        }
    };

    public SenderRequest? Create(WebHookModel model, ISender service)
    {
        if (!ContentFactoryBySenderType.TryGetValue(service.GetType(), out var createContent))
        {
            return null;
        }

        return new SenderRequest
        {
            Recipient = model.Recipient,
            Content = createContent(model)
        };
    }
}