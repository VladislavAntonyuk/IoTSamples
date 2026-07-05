namespace HomeManagement.Shared;

public class NetworkDevice : Device
{
    public int UptimeSeconds { get; init; }
    public double Temperature { get; init; }
}