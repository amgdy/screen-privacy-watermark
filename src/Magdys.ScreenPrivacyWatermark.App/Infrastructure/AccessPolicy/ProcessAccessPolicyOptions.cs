using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;

public class ProcessAccessPolicyOptions : IAccessPolicyOptions
{
    public string AllowedProcesses { get; set; }

    public string[] AllowedProcessesList => AllowedProcesses.SplitConfiguration();
}
