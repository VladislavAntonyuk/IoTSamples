using System.Globalization;

namespace HomeManagement.Shared;

public static class DeviceManager
{
    public static int GetUptime()
    {
        try
        {
            var uptimeText = File.ReadAllText("/proc/uptime");
            var uptimeSeconds = double.Parse(uptimeText.Split(' ')[0], CultureInfo.InvariantCulture);

            return (int)uptimeSeconds;
        }
        catch
        {
            return 0;
        }
    }
}