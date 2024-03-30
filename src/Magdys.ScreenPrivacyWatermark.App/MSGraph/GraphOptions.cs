namespace Magdys.ScreenPrivacyWatermark.App.MSGraph;

public class GraphOptions
{
    public const string SectionName = "EntraID";

    public Guid ClientId { get; set; }

    public string ClientSecret { get; set; }

    public Guid TenantId { get; set; }

    public string[] Scopes { get; set; } = ["User.Read"];

    public static Uri AuthorityBase => new("https://login.microsoftonline.com");

    public Uri Authority => new($"{AuthorityBase}{TenantId}/v2.0");

    public bool UsePublicClient { get; set; } = false;
}
