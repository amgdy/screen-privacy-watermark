using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Extensions;

namespace Magdys.ScreenPrivacyWatermark.App.Watermark.Sources;

internal class EntraIdWatermarkSourceOptions : IWatermarkSourceOptions
{
    public bool Enabled { get; set; } = true;

    public string Attributes { get; set; } = "Id,DisplayName,UserPrincipalName,Mail,GivenName,Surname";

    public string[] AttributesArray => Attributes.SplitConfiguration();
}
