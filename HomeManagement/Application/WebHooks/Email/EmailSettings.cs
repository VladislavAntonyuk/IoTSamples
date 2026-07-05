namespace HomeManagement.Application.WebHooks.Email;

public class EmailSettings
{
    public required string ApiKey { get; set; }
    public required string ApiSecret { get; set; }
    public required string FromEmail { get; set; }
    public required string[] To { get; set; }
}