namespace HomeManagement.Application.WebHooks;

public interface ISender
{
    Task<HasErrorResult> Send(SenderRequest request, CancellationToken token);
}