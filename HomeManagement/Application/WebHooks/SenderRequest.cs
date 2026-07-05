namespace HomeManagement.Application.WebHooks;

public record SenderRequest
{
    public required string? Recipient { get; init; }
    public required ISenderContent Content { get; init; }
}