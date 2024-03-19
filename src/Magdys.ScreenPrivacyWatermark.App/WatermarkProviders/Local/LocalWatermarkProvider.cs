using Magdys.ScreenPrivacyWatermark.App.Settings;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;

namespace Magdys.ScreenPrivacyWatermark.App.WatermarkProviders.Local;

public class LocalWatermarkProvider(ILogger<LocalWatermarkProvider> logger, IOptions<WatermarkProviderSettings> watermarkOptions) : IWatermarkProvider
{
    public Dictionary<string, string> Data { get; private set; } = [];

    public bool IsLoaded { get; private set; }

    public Task<bool> IsOnline()
    {
        return Task.FromResult(true);
    }

    public async Task LoadDataAsync(params string[] requestedAttibutes)
    {
        var localData = await GetLocalDataAsync(watermarkOptions.Value.DataDateCultures.ToArray());
        foreach (var item in localData)
        {
            AddData(item.Key, item.Value);
        }

        IsLoaded = true;
    }

    private void AddData(string key, string value)
    {
        if (!Data.TryAdd(key, value))
        {
            logger.LogDebug("Failed to add {key} because it might be exists.", key);
        }
    }

    public static async Task<Dictionary<string, string>> GetLocalDataAsync(params string[] cultures)
    {
        var now = DateTimeOffset.Now;

        var data = new Dictionary<string, string>
        {
            { nameof(Environment.UserName), Environment.UserName },
            { nameof(Environment.MachineName), Environment.MachineName },
            { nameof(Environment.UserDomainName), Environment.UserDomainName },
            { nameof(Environment.ProcessId), Environment.ProcessId.ToString() },
            { "Date", now.Date.ToString("D", new CultureInfo("en-US")) },
            { "Time", now.TimeOfDay.ToString() },
        };

        foreach (var culture in cultures)
        {
            var cultureInfo = new CultureInfo(culture);
            data.Add($"Date{cultureInfo.TwoLetterISOLanguageName}", now.Date.ToString("D", cultureInfo));
        }

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

        return data;
    }
}
