namespace HomeManagement.Application.WebHooks;

public class WebHookModel
{
    public string? Recipient { get; init; }
    public required string Message { get; init; }
}