namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.Caching;

public class CacheItem
{
    public DateTimeOffset Created { get; } = DateTimeOffset.Now;

    public string? WatermarkText { get; set; }

    public Dictionary<string, string>? Configurations { get; set; }
}
