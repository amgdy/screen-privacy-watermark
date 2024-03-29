using Magdys.ScreenPrivacyWatermark.App.Forms;
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

        IHost? host = null;

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
                .ConfigureAppConfiguration(args, loggerFactory.CreateLogger("Configuration"))
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
                    options.OnAlreadyRunning = logger => logger.LogWarning("Application supports running single instance only!");
                })
                .ConfigureProcessProtection(options =>
                {
                    options.Enabled = true;
#if DEBUG
                    // disable the protection when debugging
                    options.Enabled = false;
#endif
                })
                .ConfigureSettings()
                .ConfigureAccessPolicies(logger)
                .ConfigureWatermark(logger)
                .ConfigureWinForms<MainForm, WatermarkForm>(options =>
                {
                    options.CloseMainFormOnly = false;
                    options.Args = args;
                })

                ;


            host = hostApplicationBuilder.Build();

            await host.StartAsync();

        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            NLog.LogManager.Shutdown();
            if (host != null)
            {
                await host.StopAsync();

            }
        }
    }

    private static void WriteStartupLogs(ILogger logger, string[] args)
    {
        logger.LogInformation("──────────────────────────────────────────────────────");
        logger.LogInformation("Starting {AppName} by {Company}...", Metadata.ApplicationNameLong, Metadata.CompanyNameLong);
        logger.LogInformation("Application started by {Domain}\\{User}.", Environment.UserDomainName, Environment.UserName);
        logger.LogInformation(
             "Application version {version} is running on {@frameworkDescription}.",
             Assembly.GetEntryAssembly()?.GetName().Version,
             System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);

        logger.LogInformation("Application started with arguments: {@args}.", args);
    }
}