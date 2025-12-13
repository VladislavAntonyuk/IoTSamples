namespace HomeManagement.Application.WebHooks;

public class PowerIsBackEmailSettings
{
    public required string FromEmail { get; set; }
    public required string FromName { get; set; }
    public required string[] To { get; set; }
}