namespace HomeManagement.Application.Router;

public interface IRouterController
{
    public Task<RouterResponse> Reboot();
    public Task<RouterResponse> RebootModem();
    public Task<RouterResponse> SetLeds(bool enabled);
}