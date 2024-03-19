namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.Core;

internal class CoreOptions
{
    public bool CloseMainFormOnly { get; set; } = false;

    public string[] Args { get; set; } = [];
}
