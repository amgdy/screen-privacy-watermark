namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;

internal class EntraIdGroupsAccessPolicyOptions : IAccessPolicyOptions
{
    public string AllowedGroupsIds { get; set; }

    public string[] AllowedGroupsIdsList => AllowedGroupsIds.SplitConfiguration();
}
