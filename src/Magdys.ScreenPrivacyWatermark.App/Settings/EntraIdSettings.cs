namespace Magdys.ScreenPrivacyWatermark.App.Settings;

public class EntraIdSettings
{
    public const string SectionName = "EntraID";

    public Guid ClientId { get; set; }

    public Guid TenantId { get; set; }

    public string[] Scopes { get; set; } = ["User.Read"];

    public Uri AuthorityBase => new("https://login.microsoftonline.com");

    public Uri Authority => new($"{AuthorityBase}{TenantId}/v2.0");

    public bool IncludeLocalData { get; set; } = true;
}
