using Magdys.ScreenPrivacyWatermark.App.Forms;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.Core;

internal class CoreHostedService(ILogger<CoreHostedService> logger, IServiceProvider serviceProvider, CoreOptions winFormsOptions) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogTrace("Executing {method}.", nameof(StartAsync));
        if (cancellationToken.IsCancellationRequested)
        {
            logger.LogTrace("Cancellation requested. Exiting {method}.", nameof(StartAsync));
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

        logger.LogTrace("Executed {method}.", nameof(StartAsync));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogTrace("Executing {method}.", nameof(StopAsync));
        var openForms = Application.OpenForms.OfType<Form>().ToArray();
        logger.LogTrace("Found {count} open forms.", openForms.Length);

        if (winFormsOptions.CloseMainFormOnly)
        {
            openForms = openForms.Where(f => f is IMainForm).ToArray();
            logger.LogTrace("Filtered to main forms only. {count} forms remaining.", openForms.Length);
        }

        foreach (var form in openForms)
        {
            try
            {
                if (form is WatermarkForm watermarkForm)
                {
                    logger.LogTrace("Force closing the form {text}.", watermarkForm.Text);
                    watermarkForm.ForceClose();
                }
                else
                {
                    logger.LogTrace("Closing the form {text}.", form.Text);
                    form.Close();
                }
                logger.LogTrace("Disposing the form {text}.", form.Text);
                form.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to close the form {text}.", form.Text);
            }
        }

        logger.LogTrace("Executed {method}.", nameof(StopAsync));
        Application.ExitThread();
        logger.LogTrace("Exited application thread.");

        return Task.CompletedTask;
    }
}
