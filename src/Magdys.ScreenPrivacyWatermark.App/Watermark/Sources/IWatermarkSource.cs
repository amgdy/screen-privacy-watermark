namespace Magdys.ScreenPrivacyWatermark.App.Watermark.Sources;

public interface IWatermarkSource
{
    bool Enabled { get; }

    ValueTask<bool> IsConnectedAsync();

    ValueTask<Dictionary<string, string>> LoadAsync(bool reload = false);
}
