namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;

public class AccessPolicyOptions
{
    public const string SectionName = "Policy";

    public EvaluationMode EvaluationMode { get; set; } = EvaluationMode.Any;
}

public enum EvaluationMode
{
    Any,
    All
}
