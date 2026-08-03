namespace HomeManagement.Application.Router;

public class InternetWatchdogOptions
{
    public bool Enabled { get; set; }

    public string PingAddress { get; set; } = "1.1.1.1";

    public int PingTimeoutMs { get; set; } = 3000;

    public int CheckIntervalSeconds { get; set; } = 60;

    public int ConsecutiveFailuresBeforeReboot { get; set; } = 3;

    public int RebootCooldownMinutes { get; set; } = 15;
}
