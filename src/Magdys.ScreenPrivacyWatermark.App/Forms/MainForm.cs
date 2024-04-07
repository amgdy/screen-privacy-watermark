using DnsClient.Internal;
using Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;
using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Caching;
using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Core;
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
    private readonly WatermarkContext _watermarkContext;
    private readonly ConnectivityService _connectivityService;
    private bool _isDisplaySettingsChanged = true;

    private readonly HashSet<string> _allowedProcessNames;
    private readonly Regex[] _allowedProcessRegexes;


    public MainForm(ILogger<MainForm> logger,
        IServiceProvider serviceProvider,
        ProcessAccessPolicyOptions processAccessPolicyOptions,
        WatermarkContext watermarkContext,
        ConnectivityService connectivityService)
    {
        InitializeComponent();
        _logger = logger;
        _serviceProvider = serviceProvider;
        _processAccessPolicyOptions = processAccessPolicyOptions;
        _watermarkContext = watermarkContext;
        _connectivityService = connectivityService;
        _allowedProcessNames = new HashSet<string>(_processAccessPolicyOptions.AllowedProcessesList, StringComparer.OrdinalIgnoreCase);
        _allowedProcessRegexes = _processAccessPolicyOptions.EnableWildcardNames
            ? _processAccessPolicyOptions.AllowedProcessesList.Select(p => new Regex(p.Replace(".", @"\.").Replace("*", ".*"), RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(0.5))).ToArray()
            : null!;

        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
    }

    private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
    {
        _logger.LogTrace("Executing {Method}.", nameof(SystemEvents_DisplaySettingsChanged));
        _isDisplaySettingsChanged = true;
        _logger.LogTrace("Executed  {Method}.", nameof(SystemEvents_DisplaySettingsChanged));
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
        _logger.LogTrace("Executing {Method}.", nameof(MainForm_Load));

        if (_processAccessPolicyOptions.AllowedProcessesList.Length > 0)
        {
            _logger.LogTrace("Process access policy check enabled.");

            _logger.LogTrace("Wildcard names is {EnableWildcardNames}.", _processAccessPolicyOptions.EnableWildcardNames ? "enabled" : "disabled");

            TimeProcessAccessPolicyCheck.Enabled = true;
        }

        BackgroundWorkerDispatcher.RunWorkerAsync();

        _logger.LogTrace("Executed  {Method}.", nameof(MainForm_Load));
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        _logger.LogTrace("Executing {Method}.", nameof(MainForm_FormClosing));

        _logger.LogDebug("Form closing reason: {Reason}", e.CloseReason);

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

        _logger.LogTrace("Executed  {Method}.", nameof(MainForm_FormClosing));
    }

    private async void BackgroundWorkerDispatcher_DoWork(object sender, DoWorkEventArgs e)
    {
        _logger.LogTrace("Executing {Method}.", nameof(BackgroundWorkerDispatcher_DoWork));

        bool isFirstIteration = true;

        while (!e.Cancel)
        {
            if (_isDisplaySettingsChanged)
            {
                BeginInvoke(new Action(async () => await ShowWatermarkFormsAsync()));

                _isDisplaySettingsChanged = false;
            }

            if (isFirstIteration && !await _connectivityService.IsConnectedAsync())
            {
                // Enable your timers here
                BeginInvoke(new Action(() => { TimerOnlineStatus.Enabled = true; }));
            }

            isFirstIteration = false;

            await Task.Delay(1000);
        }

        _logger.LogTrace("Executed  {Method}.", nameof(BackgroundWorkerDispatcher_DoWork));
    }

    private async ValueTask ShowWatermarkFormsAsync()
    {
        _logger.LogTrace("Executing {Method}.", nameof(ShowWatermarkFormsAsync));

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

        _logger.LogTrace("Executed {Method}.", nameof(ShowWatermarkFormsAsync));
    }

    private async void TimerOnlineStatus_Tick(object sender, EventArgs e)
    {
        _logger.LogTrace("Executing {Method}.", nameof(TimerOnlineStatus_Tick));
        if (await _connectivityService.IsConnectedAsync())
        {
            await ShowWatermarkFormsAsync();
            TimerOnlineStatus.Enabled = false;
        }

        _logger.LogTrace("Executed {Method}.", nameof(TimerOnlineStatus_Tick));
    }

    private int _timeProcessAccessPolicyCheckFailures = 0;

    private void TimeProcessAccessPolicyCheck_Tick(object sender, EventArgs e)
    {

#if !(DEBUG)
        _logger.LogTrace("Executing {Method}.", nameof(TimeProcessAccessPolicyCheck_Tick));
#endif
        if (_processAccessPolicyOptions.AllowedProcessesList.Length == 0)
        {
            return;
        }

        try
        {
            // this feature is an expensive operation, so we need to measure the time it takes to execute
            var timestamp = Stopwatch.GetTimestamp();

            var allProcessesWithWindows = Process
                .GetProcesses()
                .Where(p => p.MainWindowHandle != IntPtr.Zero)
                .ToArray();

            var filteredProcesses = new List<Process>();

            if (_processAccessPolicyOptions.EnableWildcardNames && _allowedProcessRegexes is not null)
            {
                foreach (var processWithWindow in allProcessesWithWindows)
                {
                    foreach (var regex in _allowedProcessRegexes)
                    {
                        if (regex.IsMatch(processWithWindow.ProcessName))
                        {
                            filteredProcesses.Add(processWithWindow);
                        }
                    }
                }
            }
            else
            {
                foreach (var processWithWindow in allProcessesWithWindows)
                {
                    if (_allowedProcessNames.Contains(processWithWindow.ProcessName))
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

            _logger.LogDebug("Process access policy check took {Elapsed} ms", elapsed.Milliseconds);
            _timeProcessAccessPolicyCheckFailures = 0; // reset the failure count if the operation was successful

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during the process access policy check.");
            _timeProcessAccessPolicyCheckFailures++;

            if (_timeProcessAccessPolicyCheckFailures >= 3)
            {
                _logger.LogWarning("Disabling the timer due to repeated failures.");
                TimeProcessAccessPolicyCheck.Enabled = false;

                // enforce the watermark to be shown in case of repeated failures
                ProcessAccessPolicyState.HideWatermark = false;
            }
        }
        finally
        {
#if !(DEBUG)
        _logger.LogTrace("Executed {Method}.", nameof(TimeProcessAccessPolicyCheck_Tick));
#endif
        }
    }


}
