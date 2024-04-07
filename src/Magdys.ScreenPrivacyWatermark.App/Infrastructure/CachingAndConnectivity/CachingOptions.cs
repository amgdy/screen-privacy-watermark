namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.Caching;

public class CachingOptions
{
    public bool EnableEncryption { get; set; }
    public ILogger? Logger { get; internal set; }
}
