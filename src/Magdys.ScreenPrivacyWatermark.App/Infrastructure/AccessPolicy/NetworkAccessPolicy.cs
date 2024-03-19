using System.Net;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;

internal class NetworkAccessPolicy(ILogger<NetworkAccessPolicy> logger, NetworkAccessPolicyOptions options) : IAccessPolicy
{
    public bool Enabled => options.AllowedIPsList.Length > 0 || options.AllowedCidrsList.Length > 0;

    public int Order => 001;

    public async Task<bool> CheckAccessAsync()
    {
        logger.LogTrace("Executing {method}.", nameof(CheckAccessAsync));

        // Validate IP addresses in AllowedIPsList
        var allowedIPs = new List<IPAddress>();
        foreach (var allowedIP in options.AllowedIPsList)
        {
            if (IPAddress.TryParse(allowedIP, out var allowed))
            {
                allowedIPs.Add(allowed);
            }
            else
            {
                logger.LogWarning("Invalid IP address: {IPAddress}", allowedIP);
            }
        }

        if (allowedIPs.Count > 0)
        {
            var ipAddresses = await Dns.GetHostAddressesAsync(Dns.GetHostName());

            foreach (var ipAddress in ipAddresses)
            {
                logger.LogDebug("Checking IP address: {IPAddress}", ipAddress);

                if (allowedIPs.Contains(ipAddress))
                {
                    logger.LogDebug("IP address {IPAddress} is allowed.", ipAddress);
                    return true;
                }
            }
        }

        if (options.AllowedCidrsList.Length > 0)
        {
            // Validate CIDR blocks in AllowedCidrsList
            var allowedCidrs = new List<IPNetwork2>();
            foreach (var cidr in options.AllowedCidrsList)
            {
                if (IPNetwork2.TryParse(cidr, out var ipNetwork))
                {
                    allowedCidrs.Add(ipNetwork);
                }
                else
                {
                    logger.LogWarning("Invalid CIDR block: {CIDR}", cidr);
                }
            }

            if (allowedCidrs.Count > 0)
            {
                var ipAddresses = await Dns.GetHostAddressesAsync(Dns.GetHostName());
                foreach (var ipNetwork in allowedCidrs)
                {
                    logger.LogDebug("Checking CIDR block: {CIDR}", ipNetwork);
                    foreach (var ipAddress in ipAddresses)
                    {
                        logger.LogDebug("Checking IP address: {IPAddress}", ipAddress);
                        if (ipNetwork.Contains(ipAddress))
                        {
                            logger.LogDebug("IP address {IPAddress} is allowed.", ipAddress);
                            return true;
                        }
                    }
                }
            }
        }

        logger.LogTrace("Executed {method}.", nameof(CheckAccessAsync));

        return false;
    }
}
