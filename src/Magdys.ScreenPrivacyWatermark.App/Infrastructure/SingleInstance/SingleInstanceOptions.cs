using Microsoft.Extensions.Logging;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.SingleInstance;

internal class SingleInstanceOptions
{
    public bool Enabled { get; set; } = true;

    public string MutexId { get; set; } = Application.ExecutablePath;

    public Action<ILogger>? OnAlreadyRunning { get; set; }
}
