namespace Magdys.ScreenPrivacyWatermark.App;

internal static class Metadata
{
    public static readonly Guid ApplicationId = new("2742ce11-4343-4565-8326-23f7e440ffca");

    public const string ApplicationNameLong = "Screen Privacy Watermark";
    public const string ApplicationNameShort = "SPW";

    public const string CompanyNameLong = "Magdy's Software";
    public const string CompanyNameShort = "MAGDYS";

    internal static string ApplicationNameLongClean { get; } = ApplicationNameLong.Replace(" ", string.Empty);

    internal static string EnvironmentVariablePrefix { get; } = $"{CompanyNameShort.ToUpperInvariant()}_{ApplicationNameShort.ToUpperInvariant()}_";

    internal static string RegistryRootKey { get; } = @$"SOFTWARE\{CompanyNameShort}\{ApplicationNameLongClean}";

    internal static string ApplicationDataPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CompanyNameShort, ApplicationNameLongClean);

    internal static string ApplicationLogsPath { get; } = Path.Combine(ApplicationDataPath, "Logs");
}
