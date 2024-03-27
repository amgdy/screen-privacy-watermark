using Magdys.ScreenPrivacyWatermark.App.Infrastructure;

namespace Magdys.ScreenPrivacyWatermark.App.Watermark.Sources;

internal class EntraIdWatermarkSourceOptions : IWatermarkSourceOptions
{
    public bool Enabled { get; set; } = true;

    public string Attributes { get; set; } = "Id,DisplayName,UserPrincipalName,Mail,GivenName,Surname,JobTitle,Department,OfficeLocation,MobilePhone,BusinessPhones,PreferredLanguage,EmployeeId,HireDate,Country,State,City,CompanyName,FaxNumber";

    public string[] AttributesArray => Attributes.SplitConfiguration();
}
