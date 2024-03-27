using System.ComponentModel.DataAnnotations;

namespace Magdys.ScreenPrivacyWatermark.App.Watermark;

public class WatermarkOptions
{
    public const string SectionName = "Watermark";

    [Required]
    public string ConnectedPattern { get; set; } = "{UserPrincipalName}";

    [Required]
    public string DisconnectedPattern { get; set; } = "{UserName}";

    public bool EnableCache { get; set; } = true;
}
