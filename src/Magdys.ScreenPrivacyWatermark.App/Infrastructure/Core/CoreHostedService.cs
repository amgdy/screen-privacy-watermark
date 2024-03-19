using Magdys.ScreenPrivacyWatermark.App.Forms;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.Core;

internal class CoreHostedService(ILogger<CoreHostedService> logger, IServiceProvider serviceProvider, CoreOptions winFormsOptions) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogTrace("Executing {method}.", nameof(StartAsync));
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        if (serviceProvider.GetService<IMainForm>() is not Form mainForm)
        {
            logger.LogWarning("MainForm is not registered");
        }
        else
        {
            Application.Run(new ApplicationContext(mainForm));
        }

        logger.LogTrace("Executed {method}.", nameof(StartAsync));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogTrace("Executing {method}.", nameof(StopAsync));
        var openForms = Application.OpenForms.OfType<Form>().ToArray();
        if (winFormsOptions.CloseMainFormOnly)
        {
            openForms = openForms.Where(f => f is IMainForm).ToArray();
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
                    form.Close();
                }
                form.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to close the form {text}.", form.Text);
            }
        }

        logger.LogTrace("Executed {method}.", nameof(StopAsync));
        Application.ExitThread();

        return Task.CompletedTask;
    }
}
