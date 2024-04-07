namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;

internal interface IAccessPolicy
{
    bool Enabled { get; }

    int Order { get; }

    bool RequiresConnectivity { get; }

    Task<bool> CheckAccessAsync();
}
