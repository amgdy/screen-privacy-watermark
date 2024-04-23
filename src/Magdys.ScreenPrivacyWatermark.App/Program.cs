using Magdys.ScreenPrivacyWatermark.App.Forms;
using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Caching;
using Magdys.ScreenPrivacyWatermark.App.Watermark;
using NLog.Extensions.Logging;
using System.Reflection;

namespace Magdys.ScreenPrivacyWatermark.App;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static async Task Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        NLog.LogManager.Configuration = LoggingExtensions.GetNLogDefaultConfig(NLog.LogLevel.Trace);

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddNLog(NLog.LogManager.Configuration);
        });

        var logger = loggerFactory.CreateLogger(nameof(Program));

        WriteStartupLogs(logger, args);

        try
        {
            var hostApplicationBuilderSettings = new HostApplicationBuilderSettings
            {
                Args = args,
                ApplicationName = Metadata.ApplicationNameLong,
                DisableDefaults = true
            };

            var hostApplicationBuilder = Host.CreateEmptyApplicationBuilder(hostApplicationBuilderSettings);

            hostApplicationBuilder
                .ConfigureCachingAndConnectivity(options =>
                {
                    options.EnableEncryption = true;
                    options.Logger = loggerFactory.CreateLogger("CachingService");
                })
                .ConfigureAppConfiguration(options =>
                {
                    options.Logger = loggerFactory.CreateLogger("AppConfiguration");
                })
                .ConfigureLogging(options =>
                {
#if DEBUG
                    options.FallbackLogLevel = NLog.LogLevel.Trace;
#endif
                })
                .ConfigureSingleInstance(options =>
                {
                    options.Enabled = true;
                    options.MutexId = Metadata.ApplicationId.ToString();
                    options.OnAlreadyRunning = logger => logger.LogWarning("The application is designed to run only one instance at a time.");
                })
                .ConfigureProcessProtection(options =>
                {
                    options.Enabled = true;
#if DEBUG
                    // disable the protection when debugging
                    options.Enabled = false;
#endif
                })
                .ConfigureAccessPolicies(logger)
                .ConfigureWatermark(logger)
                .ConfigureWinForms<MainForm, WatermarkForm>(options =>
                {
                    options.CloseMainFormOnly = false;
                    options.Args = args;
                });

            using var host = hostApplicationBuilder.Build();

            await host.StartAsync();
        }
        catch (UiException uiex)
        {
            logger.LogCritical(uiex, "An internal application error has occurred.");
            MessageBox.Show(uiex.Message, "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "The application has terminated unexpectedly.");
            MessageBox.Show("An unexpected error has occurred in the application. Please contact your IT department for assistance.", "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            NLog.LogManager.Shutdown();
        }
    }

    private static void WriteStartupLogs(ILogger logger, string[] args)
    {
        logger.LogInformation("──────────────────────────────────────────────────────");
        logger.LogInformation("Initiating {AppName} by {Company}...", Metadata.ApplicationNameLong, Metadata.CompanyNameLong);
        logger.LogInformation("Application initiated by user: {Domain}\\{User}.", Environment.UserDomainName, Environment.UserName);
        logger.LogInformation(
             "Running application version {Version} on {@FrameworkDescription}.",
             Assembly.GetEntryAssembly()?.GetName().Version,
             System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);

        logger.LogInformation("Application initiated with arguments: {@Args}.", args);
    }
}
