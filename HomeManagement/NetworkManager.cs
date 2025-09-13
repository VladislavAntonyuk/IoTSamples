using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace HomeManagement;

public class NetworkManager
{
    public static async Task<List<Device>> ScanNetworkAsync(string baseIP, int timeout, CancellationToken token)
    {
        var tasks = new List<Task<Device?>>();

        for (int i = 1; i <= 254; i++)
        {
            string ip = baseIP + i;
            tasks.Add(PingAndResolveAsync(ip, timeout, token));
        }

        var results = await Task.WhenAll(tasks);
        return results.OfType<Device>().ToList();
    }

    static async Task<Device?> PingAndResolveAsync(string ip, int timeout, CancellationToken token)
    {
        using Ping ping = new();
        try
        {
            PingReply reply = await ping.SendPingAsync(ip, TimeSpan.FromMilliseconds(timeout), cancellationToken: token);
            if (reply.Status == IPStatus.Success)
            {
                return await GetDeviceInfoAsync(ip, token);
            }
        }
        catch
        {
            // Ignore failures
        }

        return null;
    }

    public static string? GetLocalSubnet()
    {
        var subnetBytes = GetSubnets();
        if (subnetBytes is null)
        {
            return null;
        }

        return $"{subnetBytes[0]}.{subnetBytes[1]}.{subnetBytes[2]}.";
    }


    public static byte[]? GetSubnets()
    {
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue; // skip loopback
            }

            var ipProps = ni.GetIPProperties();
            foreach (var ipInfo in ipProps.UnicastAddresses)
            {
                if (ipInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    var ipBytes = ipInfo.Address.GetAddressBytes();
                    return ipBytes;
                }
            }
        }

        return null;
    }

    private static async Task<Device?> GetDeviceInfoAsync(string ip, CancellationToken token)
    {
        using var httpClient = new HttpClient();
        return await httpClient.GetFromJsonAsync<Device>($"http://{ip}/info", token);
    }
}
