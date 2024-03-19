// Ignore Spelling: Entra

using Magdys.ScreenPrivacyWatermark.App.Settings;
using Magdys.ScreenPrivacyWatermark.App.WatermarkProviders.Local;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;

namespace Magdys.ScreenPrivacyWatermark.App.WatermarkProviders.EntraId;

internal class EntraIDWatermarkProvider(ILogger<EntraIDWatermarkProvider> logger, IOptions<EntraIdSettings> options, IOptions<WatermarkProviderSettings> watermarkOptions, MicrosoftGraphService microsoftGraphService) : IWatermarkProvider
{
    private readonly ILogger<EntraIDWatermarkProvider> _logger = logger;
    private readonly IOptions<EntraIdSettings> _options = options;
    private readonly IOptions<WatermarkProviderSettings> _watermarkOptions = watermarkOptions;

    public Dictionary<string, string> Data { get; private set; } = [];

    public bool IsLoaded { get; private set; }

    public async Task<bool> IsOnline()
    {
        _logger.LogTrace("Checking if online...");

        using var httpClient = new HttpClient();

        try
        {
            int retryCount = 3;

            for (int i = 0; i < retryCount; i++)
            {
                _logger.LogDebug("Attempt {attempt} to check online status.", i + 1);

                var response = await httpClient.GetAsync(_options.Value.AuthorityBase);

                // If the status code is OK, the website is available
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    _logger.LogDebug("Online status check succeeded on attempt {attempt}.", i + 1);
                    return true;
                }

                // Wait for a short period before retrying
                await Task.Delay(1000);
            }

            // If the status code is still not OK after 3 attempts, return false
            _logger.LogWarning("Online status check failed after {retryCount} attempts.", retryCount);
            return false;

        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "You're currently Offline and not connect to the Internet.");
            return false;
        }
    }

    public async Task LoadDataAsync(params string[] requestedAttributes)
    {
        _logger.LogTrace("Loading data...");
        try
        {
            var graphServiceClient = microsoftGraphService.Client;

            var user = await graphServiceClient.Me.GetAsync();

            if (user != null)
            {
                AddData(nameof(user.Id), user.Id);
                AddData(nameof(user.DisplayName), user.DisplayName);
                AddData(nameof(user.UserPrincipalName), user.UserPrincipalName);
                AddData(nameof(user.Mail), user.Mail);
                AddData(nameof(user.GivenName), user.GivenName);
                AddData(nameof(user.Surname), user.Surname);
                AddData(nameof(user.JobTitle), user.JobTitle);
                AddData(nameof(user.Department), user.Department);
                AddData(nameof(user.OfficeLocation), user.OfficeLocation);
                AddData(nameof(user.MobilePhone), user.MobilePhone);
                AddData(nameof(user.BusinessPhones), string.Join(", ", user.BusinessPhones ?? []));
                AddData(nameof(user.PreferredLanguage), user.PreferredLanguage);
                AddData(nameof(user.EmployeeId), user.EmployeeId);
                AddData(nameof(user.HireDate), user.HireDate?.ToString("D", new CultureInfo("en-US")));
                AddData(nameof(user.Country), user.Country);
                AddData(nameof(user.State), user.State);
                AddData(nameof(user.City), user.City);
                AddData(nameof(user.Birthday), user.Birthday);
                AddData(nameof(user.CompanyName), user.CompanyName);
                AddData(nameof(user.FaxNumber), user.FaxNumber);

                _logger.LogDebug("User data loaded successfully.");

            }
            else
            {
                _logger.LogError("Failed to load user data from Entra ID, please check the Entra ID Application Configuration.");
            }

            if (_options.Value.IncludeLocalData)
            {
                var localData = await LocalWatermarkProvider.GetLocalDataAsync(_watermarkOptions.Value.DataDateCultures.ToArray());

                foreach (var item in localData)
                {
                    AddData(item.Key, item.Value);
                }
            }
            else
            {
                _logger.LogDebug("Local data is not included.");
            }


            IsLoaded = true;
            _logger.LogDebug("Data loaded successfully.");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while loading data.");
            throw;
        }
    }

    private void AddData(string key, object? value)
    {
        if (!Data.TryAdd(key, value?.ToString()))
        {
            _logger.LogDebug("Failed to add {key} because it might be exists.", key);
        }
        else
        {
            _logger.LogDebug("Added {key} to data.", key);
        }
    }
}
