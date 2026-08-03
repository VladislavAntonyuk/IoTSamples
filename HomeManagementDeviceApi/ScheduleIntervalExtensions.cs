using Coravel.Scheduling.Schedule.Interfaces;
using Innovative.SolarCalculator;

namespace HomeManagementDeviceApi;

public static class ScheduleIntervalExtensions
{
    extension(IScheduleInterval scheduleInterval)
    {
        public IScheduledEventConfiguration AtSunrise(TimeZoneInfo timeZoneInfo, double latitude, double longitude, int adjustmentMinutes = 0)
        {
            var lastRunDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo);

            return scheduleInterval
                .EveryMinute()
                .Zoned(timeZoneInfo)
                .When(() =>
                {
                    var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo);

                    var solarTimes = new SolarTimes(localNow.Date, latitude, longitude);
                    var localSunrise = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.Sunrise.ToUniversalTime(), timeZoneInfo);
                    var adjustment = TimeSpan.FromMinutes(adjustmentMinutes);
                    var enableAt = localSunrise + adjustment;

                    if (lastRunDate < enableAt)
                    {
                        lastRunDate = enableAt;
                        return Task.FromResult(true);
                    }

                    return Task.FromResult(false);
                });
        }

        public IScheduledEventConfiguration AtSunset(TimeZoneInfo timeZoneInfo, double latitude, double longitude, int adjustmentMinutes = 0)
        {
            var lastRunDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo);

            return scheduleInterval
                .EveryMinute()
                .Zoned(timeZoneInfo)
                .When(() =>
                {
                    var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo);

                    var solarTimes = new SolarTimes(localNow.Date, latitude, longitude);
                    var localSunset = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.Sunset.ToUniversalTime(), timeZoneInfo);
                    var adjustment = TimeSpan.FromMinutes(adjustmentMinutes);
                    var enableAt = localSunset + adjustment;

                    if (lastRunDate > enableAt)
                    {
                        lastRunDate = enableAt;
                        return Task.FromResult(true);
                    }

                    return Task.FromResult(false);
                });
        }
    }
}
