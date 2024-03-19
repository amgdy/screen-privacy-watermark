using System.ComponentModel;
using System.Security.AccessControl;
using System.Security.Principal;
using static Magdys.ScreenPrivacyWatermark.App.NativeMethods;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.ProcessProtection;

internal class ProcessProtectionHostedService(ILogger<ProcessProtectionHostedService> logger, ProcessProtectionOptions processProtectionOptions) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogTrace("Executing {method}.", nameof(StartAsync));
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        if (processProtectionOptions.Enabled)
        {
            logger.LogInformation("Enabling process protection");

            Protect();

            logger.LogInformation("Process protection is enabled");
        }
        else
        {
            logger.LogInformation("Process protection is skipped");
        }

        logger.LogTrace("Executed {method}.", nameof(StartAsync));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogTrace("Executing {method}.", nameof(StopAsync));
        return Task.CompletedTask;
    }

    private void Protect()
    {
        logger.LogTrace("Executing {method}.", nameof(Protect));

        // Get the current process handle
        IntPtr hProcess = GetCurrentProcess();

        const int DACL_SECURITY_INFORMATION = 0x00000004;
        byte[] psd = [];

        // Call with 0 size to obtain the actual size needed in bufSizeNeeded
        GetKernelObjectSecurity(hProcess, DACL_SECURITY_INFORMATION, psd, 0, out uint bufSizeNeeded);
        if (bufSizeNeeded > short.MaxValue)
        {
            throw new Win32Exception();
        }

        // Allocate the required bytes and obtain the DACL
        if (!GetKernelObjectSecurity(hProcess, DACL_SECURITY_INFORMATION, psd = new byte[bufSizeNeeded], bufSizeNeeded, out _))
        {
            throw new Win32Exception();
        }
        // Use the RawSecurityDescriptor class from System.Security.AccessControl to parse the bytes:
        var dacl = new RawSecurityDescriptor(psd, 0);

        // Insert the new ACE

        var ace = new CommonAce(AceFlags.None, AceQualifier.AccessDenied, (int)ProcessAccessRights.PROCESS_ALL_ACCESS, new SecurityIdentifier(WellKnownSidType.WorldSid, null), false, null);
        dacl.DiscretionaryAcl?.InsertAce(0, ace);

        byte[] rawsd = new byte[dacl.BinaryLength];
        dacl.GetBinaryForm(rawsd, 0);
        if (!SetKernelObjectSecurity(hProcess, DACL_SECURITY_INFORMATION, rawsd))
        {
            throw new Win32Exception();
        }

        logger.LogTrace("Executed {method}.", nameof(Protect));
    }
}
