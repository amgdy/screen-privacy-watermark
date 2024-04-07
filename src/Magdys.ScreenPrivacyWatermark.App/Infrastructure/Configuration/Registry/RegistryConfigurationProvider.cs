using Microsoft.Win32;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.Configuration.Registry;

internal class RegistryConfigurationProvider(RegistryConfigurationOptions options, ILogger? logger) : ConfigurationProvider
{
    private readonly RegistryConfigurationOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    public override void Load()
    {
        logger?.LogTrace("Loading registry configuration for {RootKey}", _options.RootKey);

        using var rootKey = _options.RegistryHive switch
        {
            RegistryHive.LocalMachine => Microsoft.Win32.Registry.LocalMachine.OpenSubKey(_options.RootKey, writable: false),
            RegistryHive.CurrentUser => Microsoft.Win32.Registry.CurrentUser.OpenSubKey(_options.RootKey, writable: false),
            _ => throw new NotSupportedException($"Unsupported registry hive: {_options.RegistryHive}"),
        };

        if (rootKey != null)
        {
            logger?.LogTrace("Processing root key: {RootKey}", _options.RootKey);
            ProcessKey(rootKey, string.Empty, 0);
        }
        else if (_options.Required)
        {
            throw new InvalidOperationException($"Registry key '{_options.RootKey}' was not found.");
        }

        logger?.LogTrace("Finished loading registry configuration for {RootKey}", _options.RootKey);
    }

    private void ProcessKey(RegistryKey key, string parentConfigKey, int depth)
    {
        try
        {
            logger?.LogTrace("Processing key: {Key}", parentConfigKey);

            string[] valueNames = key.GetValueNames();
            foreach (string valueName in valueNames)
            {
                var value = key.GetValue(valueName);
                var valueNameKey = GetConfigKey(parentConfigKey, valueName, logger);
                Data[valueNameKey] = Convert.ToString(value);
            }

            if (depth >= _options.Depth)
            {
                return;
            }

            string[] keyNames = key.GetSubKeyNames();
            foreach (string keyName in keyNames)
            {
                using var subKey = key.OpenSubKey(keyName) ?? throw new InternalException($"Failed to open subkey: {keyName}");
                var parentKey = GetConfigKey(parentConfigKey, keyName, logger);
                ProcessKey(subKey, parentKey, ++depth); // increment depth before the method call
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while processing the registry key: {Key}", parentConfigKey);
        }
    }

    private static string GetConfigKey(string parent, string child, ILogger? logger)
    {
        logger?.LogTrace("Getting config key for parent: {Parent}, child: {Child}", parent, child);

        if (string.IsNullOrWhiteSpace(child))
        {
            throw new ArgumentException($"'{nameof(child)}' cannot be null or whitespace.", nameof(child));
        }

        if (string.IsNullOrWhiteSpace(parent))
        {
            return child;
        }

        var configKey = $"{parent}{ConfigurationPath.KeyDelimiter}{child}";
        logger?.LogTrace("Config key: {ConfigKey}", configKey);

        return configKey;
    }
}
