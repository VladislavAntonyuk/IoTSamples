using HomeManagement.Components.Dialogs;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeManagement.Application.DeviceManagement;

public class NetworkManager
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<List<NetworkDevice>> ScanNetworkAsync(string baseIp, int timeout, CancellationToken token)
    {
        var tasks = new List<Task<NetworkDevice?>>();

        for (var i = 1; i <= 254; i++)
        {
            var ip = baseIp + i;
            tasks.Add(PingAndResolveAsync(ip, timeout, token));
        }

        var results = await Task.WhenAll(tasks);
        return results.OfType<NetworkDevice>().ToList();
    }

    static async Task<NetworkDevice?> PingAndResolveAsync(string ip, int timeout, CancellationToken token)
    {
        using Ping ping = new();
        try
        {
            var reply = await ping.SendPingAsync(ip, TimeSpan.FromMilliseconds(timeout), cancellationToken: token);
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

    public static string? GetLocalIp()
    {
        var subnetBytes = GetSubnets();
        if (subnetBytes is null)
        {
            return null;
        }

        return $"{subnetBytes[0]}.{subnetBytes[1]}.{subnetBytes[2]}.{subnetBytes[3]}";
    }

    public static byte[]? GetSubnets()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
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

    public static async Task<NetworkDevice?> GetDeviceInfoAsync(string address, CancellationToken token)
    {
        try
        {
            using var httpClient = new HttpClient();
            return await httpClient.GetFromJsonAsync<NetworkDevice>($"http://{address}/info", JsonSerializerOptions, token);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
