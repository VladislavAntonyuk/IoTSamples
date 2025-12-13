namespace HomeManagement.Application.WebHooks;

public interface IHandler
{
    Task<HasErrorResult> Handle();
}