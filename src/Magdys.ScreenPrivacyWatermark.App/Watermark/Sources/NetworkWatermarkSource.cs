using System.Net;
using System.Net.NetworkInformation;

namespace Magdys.ScreenPrivacyWatermark.App.Watermark.Sources;

public class NetworkWatermarkSource(ILogger<NetworkWatermarkSource> logger) : IWatermarkSource
{
    public bool Enabled { get; set; } = true;

    public bool RequiresConnectivity => false;

    public async ValueTask<Dictionary<string, string>> LoadAsync(bool reload = false)
    {
        logger.LogTrace("Loading network watermark data");
        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var ipAddress = await Dns.GetHostAddressesAsync(Dns.GetHostName());

        for (int i = 0; i < ipAddress.Length; i++)
        {
            var ip = ipAddress[i];
            if (i == 0)
            {
                data.Add($"IPAddress", ip.ToString());
            }

            data.Add($"IPAddress{i}", ip.ToString());
        }

        var nics = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up)
            .Select(n => n.GetPhysicalAddress().ToString())
            .ToArray();

        for (int i = 0; i < nics.Length; i++)
        {
            if (i == 0)
            {
                data.Add("MacAddress", nics[i]);
            }

            data.Add($"MacAddress{i}", nics[i]);
        }


        logger.LogTrace("Network watermark data loaded");
        return data;
    }
}
