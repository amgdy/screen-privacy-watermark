using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;

internal class MacAddressAccessPolicy(ILogger<MacAddressAccessPolicy> logger, MacAddressAccessPolicyOptions? options = null) : IAccessPolicy
{
    public bool Enabled => false;

    public int Order => 001;

    public async Task<bool> CheckAccessAsync()
    {
        return true;
    }
}