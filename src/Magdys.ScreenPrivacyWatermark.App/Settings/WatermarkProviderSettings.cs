using System.ComponentModel.DataAnnotations;

namespace Magdys.ScreenPrivacyWatermark.App.Settings;

public class WatermarkProviderSettings
{
    public enum WatermarkProvider
    {
        Local,
        ActiveDirectory,
        EntraID
    }

    public const string SectionName = "Provider";

    public WatermarkProvider Name { get; set; } = WatermarkProvider.Local;

    [Required]
    public string WatermarkOnlinePattern { get; set; } = "{UserPrincipalName} ({JobTitle}) - {Date}";

    [Required]
    public string WatermarkOfflinePattern { get; set; } = "{UserName}";

    public bool EnableCache { get; set; } = true;

    public List<string> DataDateCultures { get; set; } = ["en-us", "ar-sa"];
}


