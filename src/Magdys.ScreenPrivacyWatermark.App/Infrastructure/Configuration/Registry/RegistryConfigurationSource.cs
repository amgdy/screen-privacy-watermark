namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.Configuration.Registry;

internal class RegistryConfigurationSource(RegistryConfigurationOptions options, ILogger? logger) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        logger?.LogTrace($"Building Registry Configuration Provider");
        return new RegistryConfigurationProvider(options, logger);
    }
}
