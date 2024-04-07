namespace Magdys.ScreenPrivacyWatermark.App.Watermark.Sources;

public interface IWatermarkSource
{
    bool Enabled { get; }

    bool RequiresConnectivity { get; }

    ValueTask<Dictionary<string, string>> LoadAsync(bool reload = false);
}
