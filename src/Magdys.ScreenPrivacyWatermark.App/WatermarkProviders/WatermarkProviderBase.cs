using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magdys.ScreenPrivacyWatermark.App.WatermarkProviders;

internal abstract class WatermarkProviderBase(ILogger<WatermarkProviderBase> logger) : IWatermarkProvider
{
    public Dictionary<string, string> Data { get; private set; } = [];

    public abstract bool IsLoaded { get; protected set; }

    public abstract Task<bool> IsOnline();

    public abstract Task LoadDataAsync(params string[] requestedAttributes);

    protected virtual void AddData(string key, string value)
    {
        if (!Data.TryAdd(key, value))
        {
            logger.LogDebug("Failed to add {key} because it might be exists.", key);
        }
    }
}
