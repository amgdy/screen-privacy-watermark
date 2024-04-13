using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Extensions;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;

public class ProcessAccessPolicyOptions : IAccessPolicyOptions
{
    public string? AllowedProcesses { get; set; }

    public string[] AllowedProcessesList => AllowedProcesses!.SplitConfiguration();

    public bool EnableWildcardNames { get; set; }
}
