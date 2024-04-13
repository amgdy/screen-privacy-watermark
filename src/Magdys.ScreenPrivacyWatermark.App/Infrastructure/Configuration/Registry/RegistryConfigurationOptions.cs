using Microsoft.Win32;
using System.ComponentModel.DataAnnotations;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.Configuration.Registry;

internal class RegistryConfigurationOptions
{
    [Required]
    public string? RootKey { get; set; }

    [Required]
    public RegistryHive RegistryHive { get; set; } = RegistryHive.LocalMachine;

    public bool Required { get; set; }

    [Range(1, 10)]
    public int Depth { get; set; } = 3;
}
