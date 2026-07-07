namespace HomeManagement.Application.WebHooks;

public record SenderRequest
{
    public required ISenderContent Content { get; init; }
}