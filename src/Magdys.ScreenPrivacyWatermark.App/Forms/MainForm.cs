using DnsClient.Internal;
using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Core;
using Magdys.ScreenPrivacyWatermark.App.WatermarkProviders;
using System.ComponentModel;
using static Magdys.ScreenPrivacyWatermark.App.NativeMethods;

namespace Magdys.ScreenPrivacyWatermark.App.Forms;

public partial class MainForm : Form, IMainForm
{
    private readonly ILogger<MainForm> _logger;

    private readonly IServiceProvider _serviceProvider;

    private readonly WatermarkContext _watermarkContext;

    private bool _isDisplaySettingsChanged = true;

    public MainForm(ILogger<MainForm> logger, IServiceProvider serviceProvider, WatermarkContext watermarkContext)
    {
        InitializeComponent();
        _logger = logger;
        _serviceProvider = serviceProvider;
        _watermarkContext = watermarkContext;
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
    }

    private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
    {
        _logger.LogTrace("Executing {e}.", nameof(SystemEvents_DisplaySettingsChanged));
        _isDisplaySettingsChanged = true;
        _logger.LogTrace("Executed  {e}.", nameof(SystemEvents_DisplaySettingsChanged));
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;

            // Makes the form transparent to pass through the clicks
            cp.ExStyle
                |= (int)ExtendedWindowStyles.WS_EX_LAYERED
                | (int)ExtendedWindowStyles.WS_EX_TRANSPARENT
                | (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    private async void MainForm_Load(object sender, EventArgs e)
    {
        _logger.LogTrace("Executing {e}.", nameof(MainForm_Load));

        BackgroundWorkerDispatcher.RunWorkerAsync();

        _logger.LogTrace("Executed  {e}.", nameof(MainForm_Load));
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        _logger.LogTrace("Executing {e}.", nameof(MainForm_FormClosing));

        switch (e.CloseReason)
        {
            case CloseReason.None:
            case CloseReason.MdiFormClosing:
            case CloseReason.UserClosing:
            case CloseReason.TaskManagerClosing:
            case CloseReason.ApplicationExitCall:
                e.Cancel = true;
                _logger.LogDebug("Form closing CANCELLED");
                break;
            default:
                _logger.LogDebug("Form closing CANCELLED");
                e.Cancel = true;
                break;
        }

        _logger.LogTrace("Executed  {e}.", nameof(MainForm_FormClosing));
    }

    private async void BackgroundWorkerDispatcher_DoWork(object sender, DoWorkEventArgs e)
    {
        _logger.LogTrace("Executing {e}.", nameof(BackgroundWorkerDispatcher_DoWork));

        bool isFirstIteration = true;

        while (!e.Cancel)
        {
            if (_isDisplaySettingsChanged)
            {
                BeginInvoke(new Action(async () => await ShowWatermarkFormsAsync()));

                _isDisplaySettingsChanged = false;
            }

            if (isFirstIteration && !await _watermarkContext.Provider.IsOnline())
            {
                // Enable your timers here
                TimerOnlineStatus.Enabled = true;

                isFirstIteration = false;
            }

            await Task.Delay(1000);
        }

        _logger.LogTrace("Executed  {e}.", nameof(BackgroundWorkerDispatcher_DoWork));
    }

    private async Task ShowWatermarkFormsAsync()
    {
        _logger.LogTrace("Executing {e}.", nameof(ShowWatermarkFormsAsync));

        var openedForms = Application.OpenForms.OfType<WatermarkForm>().ToArray();

        foreach (var openedForm in openedForms)
        {
            openedForm.ForceClose();
        }

        var watermarkText = await _watermarkContext.GetWatermarkText();

        foreach (var screen in Screen.AllScreens)
        {
            var options = new WatermarkFormOptions(screen, watermarkText);
            _serviceProvider
                .GetRequiredService<WatermarkForm>()
                .InitializeForm(options)
                .Show();
        }

        _logger.LogTrace("Executed {e}.", nameof(ShowWatermarkFormsAsync));
    }

    private async void TimerOnlineStatus_Tick(object sender, EventArgs e)
    {
        _logger.LogTrace("Executing {e}.", nameof(TimerOnlineStatus_Tick));
        while (!await _watermarkContext.Provider.IsOnline())
        {
            await Task.Delay(5000);
        }

        await ShowWatermarkFormsAsync();
        _logger.LogTrace("Executed {e}.", nameof(TimerOnlineStatus_Tick));
    }
}
