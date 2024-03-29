using System.ComponentModel.DataAnnotations;

namespace Magdys.ScreenPrivacyWatermark.App.Watermark.Options;

public class WatermarkLayoutOptions
{
    public const string SectionName = "Watermark";

    [Required]
    public string ConnectedPattern { get; set; } = "{UserPrincipalName}";

    [Required]
    public string DisconnectedPattern { get; set; } = "{UserName}";

    public bool EnableWatermarkTextCache { get; set; } = true;
}
