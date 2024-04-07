using Magdys.ScreenPrivacyWatermark.App.Forms;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.Core;

internal class CoreHostedService(ILogger<CoreHostedService> logger, IServiceProvider serviceProvider, CoreOptions winFormsOptions) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogTrace("Executing {Method}.", nameof(StartAsync));
        if (cancellationToken.IsCancellationRequested)
        {
            logger.LogTrace("Cancellation requested. Exiting {Method}.", nameof(StartAsync));
            return Task.CompletedTask;
        }

        logger.LogTrace("Getting MainForm from service provider.");
        if (serviceProvider.GetService<IMainForm>() is not Form mainForm)
        {
            logger.LogWarning("MainForm is not registered");
        }
        else
        {
            logger.LogTrace("MainForm found. Running application context.");
            Application.Run(new ApplicationContext(mainForm));
        }

        logger.LogTrace("Executed {Method}.", nameof(StartAsync));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogTrace("Executing {Method}.", nameof(StopAsync));
        var openForms = Application.OpenForms.OfType<Form>().ToArray();
        logger.LogTrace("Found {Count} open forms.", openForms.Length);

        if (winFormsOptions.CloseMainFormOnly)
        {
            openForms = openForms.Where(f => f is IMainForm).ToArray();
            logger.LogTrace("Filtered to main forms only. {Count} forms remaining.", openForms.Length);
        }

        foreach (var form in openForms)
        {
            try
            {
                if (form is WatermarkForm watermarkForm)
                {
                    logger.LogTrace("Force closing the form {Text}.", watermarkForm.Text);
                    watermarkForm.ForceClose();
                }
                else
                {
                    logger.LogTrace("Closing the form {Text}.", form.Text);
                    form.Close();
                }
                logger.LogTrace("Disposing the form {Text}.", form.Text);
                form.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to close the form {Text}.", form.Text);
            }
        }

        logger.LogTrace("Executed {Method}.", nameof(StopAsync));
        Application.ExitThread();
        logger.LogTrace("Exited application thread.");

        return Task.CompletedTask;
    }
}
