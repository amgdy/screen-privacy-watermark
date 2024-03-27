using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;

internal class MacAddressAccessPolicyOptions : IAccessPolicyOptions
{
    public string AllowedMacAddresses { get; set; }

    public string[] AllowedMacAddressesArray => AllowedMacAddresses.SplitConfiguration();
}
