﻿using System.Net;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;

internal class NetworkAccessPolicy(ILogger<NetworkAccessPolicy> logger, NetworkAccessPolicyOptions options) : IAccessPolicy
{
    public bool Enabled => options.AllowedIPsList.Length > 0 || options.AllowedCidrsList.Length > 0;

    public int Order => 001;

    public bool RequiresConnectivity => false;

    public async Task<bool> CheckAccessAsync()
    {
        logger.LogTrace("Executing {Method}.", nameof(CheckAccessAsync));

        // Validate IP addresses in AllowedIPsList
        var allowedIPs = new HashSet<IPAddress>();
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

        var ipAddresses = await Dns.GetHostAddressesAsync(Dns.GetHostName());

        if (allowedIPs.Count > 0)
        {
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

        logger.LogTrace("Executed {Method}.", nameof(CheckAccessAsync));

        return false;
    }
}
