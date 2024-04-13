using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Configuration.Registry;
using System.ComponentModel.DataAnnotations;

namespace Magdys.ScreenPrivacyWatermark.App;

internal static class RegistryConfigurationExtensions
{
    public static IConfigurationBuilder AddWindowsRegistry(this IConfigurationBuilder builder, Action<RegistryConfigurationOptions> configureAction, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(configureAction);

        var options = new RegistryConfigurationOptions();
        configureAction.Invoke(options);

        var validationErrors = new List<ValidationResult>();
        if (Validator.TryValidateObject(options, new ValidationContext(options), validationErrors))
        {
            builder.Add(new RegistryConfigurationSource(options, logger));
        }
        else
        {
            throw new ArgumentException($"Registry Configuration Options are not valid: {string.Join("; ", validationErrors.Select(v => v.ErrorMessage))}");
        }

        return builder;
    }
}
