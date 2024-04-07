using System.Net.NetworkInformation;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;

internal class MacAddressAccessPolicy(ILogger<MacAddressAccessPolicy> logger, MacAddressAccessPolicyOptions options = null) : IAccessPolicy
{
    public bool Enabled => options.AllowedMacAddressesArray.Length > 0;

    public int Order => 002;

    public bool RequiresConnectivity => false;

    public Task<bool> CheckAccessAsync()
    {
        logger.LogTrace("Executing {Method}.", nameof(CheckAccessAsync));

        var allowedMacs = options.AllowedMacAddressesArray.Select(mac => mac.ToUpper()).ToArray();

        var physicalAddresses = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up)
            .Select(n => n.GetPhysicalAddress().ToString().ToUpper())
            .ToArray();


        var commonMacs = allowedMacs.Intersect(physicalAddresses);

        var hasAccess = commonMacs.Any();

        logger.LogDebug("User {HasAccess} access based on Policy {PolicyName}", hasAccess ? "granted" : "denied", nameof(MacAddressAccessPolicy));

        logger.LogTrace("Executed {Method}.", nameof(CheckAccessAsync));
        return Task.FromResult(hasAccess);
    }
}