namespace Magdys.ScreenPrivacyWatermark.App.WatermarkProviders;

public interface IWatermarkProvider
{
    Dictionary<string, string> Data { get; }

    bool IsLoaded { get; }

    Task<bool> IsOnline();

    Task LoadDataAsync(params string[] requestedAttributes);
}

public enum ConnectionStates
{
    Online,
    Offline
}
