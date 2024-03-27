using System.Globalization;

namespace Magdys.ScreenPrivacyWatermark.App.Watermark.Sources;

public class LocalWatermarkSourceOptions : IWatermarkSourceOptions
{
    public CultureInfo[] DateCultures { get; set; } = [new CultureInfo("en-US"), new CultureInfo("ar-SA")];

    public bool Enabled { get; set; } = true;
}
