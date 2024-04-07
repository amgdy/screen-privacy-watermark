using System.Text.RegularExpressions;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.Extensions;

internal static partial class GeneratedRegexes
{
    [GeneratedRegex("{(?<token>[A-Za-z0-9-_]{2,64})}", RegexOptions.IgnoreCase, 500, "en-US")]
    internal static partial Regex TokensExtraction();
}
