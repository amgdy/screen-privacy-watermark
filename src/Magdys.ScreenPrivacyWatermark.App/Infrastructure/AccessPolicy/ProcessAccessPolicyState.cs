namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;

internal static class ProcessAccessPolicyState
{
    /// <summary>
    /// Gets or sets a value indicating whether the watermark should be hidden.
    /// Null means is not configured or not enabled.
    /// </summary>
    public static bool? HideWatermark { get; set; }
}
