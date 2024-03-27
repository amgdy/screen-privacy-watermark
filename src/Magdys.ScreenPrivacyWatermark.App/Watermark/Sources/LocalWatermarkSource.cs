using System.Globalization;

namespace Magdys.ScreenPrivacyWatermark.App.Watermark.Sources;

public class LocalWatermarkSource(ILogger<LocalWatermarkSource> logger, LocalWatermarkSourceOptions options) : IWatermarkSource
{
    public bool Enabled => options.Enabled;

    public ValueTask<bool> IsConnectedAsync()
    {
        return ValueTask.FromResult(true);
    }

    public ValueTask<Dictionary<string, string>> LoadAsync(bool reload = false)
    {
        logger.LogTrace("Loading local watermark data");

        var now = DateTimeOffset.Now;

        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { nameof(Environment.UserName), Environment.UserName },
            { nameof(Environment.MachineName), Environment.MachineName },
            { nameof(Environment.UserDomainName), Environment.UserDomainName },
            { nameof(Environment.ProcessId), Environment.ProcessId.ToString() },
            { "Date", now.Date.ToString("D", new CultureInfo("en-US")) },
            { "Time", now.TimeOfDay.ToString() },
        };

        foreach (var culture in options.DateCultures)
        {
            data.Add($"Date{culture.TwoLetterISOLanguageName}", now.Date.ToString("D", culture));
        }

        logger.LogTrace("Local watermark data loaded");
        return ValueTask.FromResult(data);
    }
}
