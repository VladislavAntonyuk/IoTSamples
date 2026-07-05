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

    public static double GetTemperature()
    {
        try
        {
            var tempText = File.ReadAllText("/sys/class/thermal/thermal_zone0/temp");
            var tempCelsius = double.Parse(tempText, CultureInfo.InvariantCulture) / 1000.0;

            return tempCelsius;
        }
        catch
        {
            return 0;
        }
    }
}