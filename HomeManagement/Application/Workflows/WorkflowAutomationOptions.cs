namespace HomeManagement.Application.Workflows;

public class WorkflowAutomationOptions
{
    public bool Enabled { get; set; } = true;

    public int CheckIntervalSeconds { get; set; } = 30;

    public double Latitude { get; set; } = 48.4647;

    public double Longitude { get; set; } = 35.0462;

    public string TimeZoneId { get; set; } = "Europe/Kyiv";

    public int SunriseAdjustmentMinutes { get; set; }

    public int SunsetAdjustmentMinutes { get; set; }
}