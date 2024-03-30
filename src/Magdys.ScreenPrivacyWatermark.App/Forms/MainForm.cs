using DnsClient.Internal;
using Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;
using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Core;
using Magdys.ScreenPrivacyWatermark.App.Settings;
using Magdys.ScreenPrivacyWatermark.App.Watermark;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static Magdys.ScreenPrivacyWatermark.App.NativeMethods;

namespace Magdys.ScreenPrivacyWatermark.App.Forms;

public partial class MainForm : Form, IMainForm
{
    private readonly ILogger<MainForm> _logger;

    private readonly IServiceProvider _serviceProvider;
    private readonly ProcessAccessPolicyOptions _processAccessPolicyOptions;
    private readonly WatermarkManager _watermarkManager;
    private bool _isDisplaySettingsChanged = true;

    public MainForm(ILogger<MainForm> logger,
        IServiceProvider serviceProvider,
        ProcessAccessPolicyOptions processAccessPolicyOptions,
        WatermarkManager watermarkManager)
    {
        InitializeComponent();
        _logger = logger;
        _serviceProvider = serviceProvider;
        _processAccessPolicyOptions = processAccessPolicyOptions;
        _watermarkManager = watermarkManager;
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

        if (_processAccessPolicyOptions.AllowedProcessesList.Length > 0)
        {
            _logger.LogTrace("Process access policy check enabled.");

            _logger.LogTrace("Wildcard names is {EnableWildcardNames}.", _processAccessPolicyOptions.EnableWildcardNames ? "enabled" : "disabled");

            TimeProcessAccessPolicyCheck.Enabled = true;
        }

        BackgroundWorkerDispatcher.RunWorkerAsync();

        _logger.LogTrace("Executed  {e}.", nameof(MainForm_Load));
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        _logger.LogTrace("Executing {e}.", nameof(MainForm_FormClosing));

        _logger.LogDebug("Form closing reason: {reason}", e.CloseReason);

        switch (e.CloseReason)
        {
            case CloseReason.None:
            case CloseReason.MdiFormClosing:
            case CloseReason.UserClosing:
            case CloseReason.TaskManagerClosing:
            //case CloseReason.ApplicationExitCall:
                e.Cancel = true;
                _logger.LogDebug("Form closing CANCELLED");
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

            if (isFirstIteration && !await _watermarkManager.IsConnectedAsync())
            {
                // Enable your timers here
                TimerOnlineStatus.Enabled = true;
            }

            isFirstIteration = false;

            await Task.Delay(1000);
        }

        _logger.LogTrace("Executed  {e}.", nameof(BackgroundWorkerDispatcher_DoWork));
    }

    private async ValueTask ShowWatermarkFormsAsync()
    {
        _logger.LogTrace("Executing {e}.", nameof(ShowWatermarkFormsAsync));

        var openedForms = Application.OpenForms.OfType<WatermarkForm>().ToArray();

        foreach (var openedForm in openedForms)
        {
            openedForm.ForceClose();
        }

        var watermarkText = await _watermarkManager.GetWatermarkText();

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
        while (!await _watermarkManager.IsConnectedAsync())
        {
            await Task.Delay(5000);
        }

        await ShowWatermarkFormsAsync();
        _logger.LogTrace("Executed {e}.", nameof(TimerOnlineStatus_Tick));
    }

    private void TimeProcessAccessPolicyCheck_Tick(object sender, EventArgs e)
    {

#if !(DEBUG)
        _logger.LogTrace("Executing {e}.", nameof(TimeProcessAccessPolicyCheck_Tick));
#endif
        if (_processAccessPolicyOptions.AllowedProcessesList.Length == 0)
        {
            return;
        }

        // this feature is an expensive operation, so we need to measure the time it takes to execute
        var timestamp = Stopwatch.GetTimestamp();

        var allowedProcessList = _processAccessPolicyOptions.EnableWildcardNames
            ? _processAccessPolicyOptions.AllowedProcessesList.Select(p => p.Replace(".", @"\.").Replace("*", ".*")).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(_processAccessPolicyOptions.AllowedProcessesList, StringComparer.OrdinalIgnoreCase);


        var allProcessesWithWindows = Process
            .GetProcesses()
            .Where(p => p.MainWindowHandle != IntPtr.Zero)
            .ToArray();

        var filteredProcesses = new List<Process>();
        foreach (var processWithWindow in allProcessesWithWindows)
        {
            if (_processAccessPolicyOptions.EnableWildcardNames)
            {
                foreach (var allowedProcess in allowedProcessList)
                {

                    if (Regex.IsMatch(processWithWindow.ProcessName, allowedProcess, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(0.5)))
                    {
                        filteredProcesses.Add(processWithWindow);
                    }
                }
            }
            else
            {
                if (allowedProcessList.Contains(processWithWindow.ProcessName))
                {
                    filteredProcesses.Add(processWithWindow);
                }
            }
        }

        foreach (var process in filteredProcesses)
        {
            var isMinimized = IsIconic(process.MainWindowHandle);
            if (!isMinimized)
            {
                ProcessAccessPolicyState.HideWatermark = false;
                return;
            }

        }

        ProcessAccessPolicyState.HideWatermark = true;


        var elapsed = Stopwatch.GetElapsedTime(timestamp);

        _logger.LogDebug("Process access policy check took {elapsed} ms", elapsed.Milliseconds);


#if !(DEBUG)
        _logger.LogTrace("Executed {e}.", nameof(TimeProcessAccessPolicyCheck_Tick));
#endif

    }
}
