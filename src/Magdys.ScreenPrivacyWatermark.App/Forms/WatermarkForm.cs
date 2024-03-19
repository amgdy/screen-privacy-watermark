using Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;
using Magdys.ScreenPrivacyWatermark.App.WatermarkProviders;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Text;
using static Magdys.ScreenPrivacyWatermark.App.NativeMethods;

namespace Magdys.ScreenPrivacyWatermark.App.Forms;

public partial class WatermarkForm : Form
{
    private readonly ILogger<WatermarkForm> _logger;

    private readonly WatermarkContext _context;
    private readonly ProcessAccessPolicyOptions _processAccessPolicyOptions;
    private WatermarkFormOptions _watermarkFormOptions;

    private bool _isFormClosingSubscribed = true;
    private bool _isInitialized = false;
    private const string _loggerExecutingText = "Executing {method} for '{text}'";

    private const string _loggerExecutedText = "Executed {method} for '{text}'";

    public WatermarkForm(ILogger<WatermarkForm> logger, WatermarkContext watermarkContext, ProcessAccessPolicyOptions _processAccessPolicyOptions)
    {
        InitializeComponent();
        _logger = logger;
        _context = watermarkContext;
        this._processAccessPolicyOptions = _processAccessPolicyOptions;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;

            // Makes the form transparent to pass through the clicks
            cp.ExStyle
                |= (int)ExtendedWindowStyles.WS_EX_LAYERED
                | (int)ExtendedWindowStyles.WS_EX_TRANSPARENT
                | (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    public void ForceClose()
    {
        if (_isFormClosingSubscribed)
        {
            FormClosing -= WatermarkForm_FormClosing;
            _isFormClosingSubscribed = false;
        }

        Close();
    }

    internal WatermarkForm InitializeForm(WatermarkFormOptions options)
    {
        _logger.LogTrace(_loggerExecutingText, nameof(InitializeForm), Text);
        ArgumentNullException.ThrowIfNull(options);

        _watermarkFormOptions = options;

        StartPosition = FormStartPosition.Manual;
        WindowState = FormWindowState.Normal;
        Text = $"{Metadata.ApplicationNameShort}-{options.Screen.DeviceName.Replace(@"\\.\", string.Empty)}";
        Location = options.Screen.WorkingArea.Location;
        Size = options.Screen.WorkingArea.Size;

        _isInitialized = true;

        _logger.LogTrace(_loggerExecutedText, nameof(InitializeForm), Text);
        return this;
    }

    private void WatermarkForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        _logger.LogTrace(_loggerExecutingText, nameof(WatermarkForm_FormClosing), Text);

        switch (e.CloseReason)
        {
            case CloseReason.None:
            case CloseReason.MdiFormClosing:
            case CloseReason.UserClosing:
            case CloseReason.TaskManagerClosing:
            case CloseReason.ApplicationExitCall:
                e.Cancel = true;
                break;
        }

        _logger.LogTrace(_loggerExecutedText, nameof(WatermarkForm_FormClosing), Text);
    }

    private async void WatermarkForm_Load(object sender, EventArgs e)
    {
        _logger.LogTrace(_loggerExecutingText, nameof(WatermarkForm_Load), Text);
        if (!_isInitialized)
        {
            throw new InvalidOperationException($"Form is not initialized. You need to call {nameof(InitializeForm)} method first.");
        }

        if (_processAccessPolicyOptions.AllowedProcessesList.Length != 0)
        {
            TimeProcessPolicyCheck.Enabled = true;
        }



        _logger.LogTrace(_loggerExecutedText, nameof(WatermarkForm_Load), Text);
    }

    private void WatermarkForm_Paint(object sender, PaintEventArgs e)
    {
        _logger.LogTrace(_loggerExecutingText, nameof(WatermarkForm_Paint), Text);

        DrawWatermarkText(e.Graphics);

        _logger.LogTrace(_loggerExecutedText, nameof(WatermarkForm_Paint), Text);
    }

    private void DrawWatermarkText(Graphics graphics)
    {
        _logger.LogTrace(_loggerExecutingText, nameof(DrawWatermarkText), Text);

        ArgumentNullException.ThrowIfNull(graphics);

        graphics.TextRenderingHint = _context.Format.TextRender;
        graphics.SmoothingMode = SmoothingMode.Default;
        graphics.InterpolationMode = InterpolationMode.Default;
        var watermarkDrawingArea = _watermarkFormOptions.Screen.WorkingArea;

        double width = watermarkDrawingArea.Width;
        double height = watermarkDrawingArea.Height;
        double diagonalSize = Math.Sqrt(Math.Pow(width, 2) + Math.Pow(height, 2));

        string watermarkText = _watermarkFormOptions.WatermarkText;
        string watermarkTextSpacer = new(' ', _context.Format.UseDynamicsSpacing ? new Random().Next(6, 15) : 10);

        string watermarkBaseString = $"{watermarkText} {watermarkTextSpacer}";

        using var font = new Font(_context.Format.FontName, _context.Format.FontSize, FontStyle.Regular, GraphicsUnit.Point);
        using var brush = new SolidBrush(Color.FromArgb(255, _context.Format.Color));
        using var stringFormat = new StringFormat(StringFormatFlags.NoClip);

        var watermarkTextBuilder = new StringBuilder(watermarkBaseString);

        // generating the watermark text to fit the screen diagonal size
        while (graphics.MeasureString(watermarkTextBuilder.ToString(), font).Width <= (diagonalSize * 1.2))
        {
            watermarkTextBuilder.Append(watermarkBaseString);
        }

        var watermarkLineText = watermarkTextBuilder.ToString();

        // Calculate the spacing between lines
        var numberOfLines = _context.Format.LinesCount + 2;
        double lineSpacing = (diagonalSize * 1.7) / numberOfLines;

        lineSpacing *= 1.0;

        for (int i = 0; i < numberOfLines; i++)
        {
            // Rotate the graphics to draw diagonally
            graphics.TranslateTransform(0, (float)(i * lineSpacing));
            graphics.RotateTransform(-45);
            var point = new PointF(0, 0);

            if (_context.Format.OutlineColor == null)
            {
                graphics.DrawString(watermarkLineText, font, brush, point, stringFormat);
            }
            else
            {
                using var graphicPath = new GraphicsPath();
                using var pen = new Pen(_context.Format.OutlineColor.Value, _context.Format.OutlineWidth);
                graphicPath.AddString(watermarkLineText, font.FontFamily, (int)font.Style, font.Size * 1.5f, point, stringFormat);

                graphics.DrawPath(pen, graphicPath);
                graphics.FillPath(brush, graphicPath);
            }


            // Reset the transformation for the next line
            graphics.ResetTransform();
        }

        _logger.LogTrace(_loggerExecutedText, nameof(DrawWatermarkText), Text);
    }

    //private string GetFormattedWatermarkText(Graphics graphics)
    //{
    //    _logger.LogTrace(_loggerExecutingText, nameof(GetFormattedWatermarkText), Text);

    //    string watermarkText = _watermarkFormOptions._watermarkText;

    //    string spacer = new string(' ', _context.Format.UseDynamicsSpacing ? new Random().Next(6, 15) : 10);

    //    string watermarkBaseString = $"{watermarkText} {spacer}";

    //    _logger.LogDebug("Watermark text: {_watermarkText}", watermarkBaseString);

    //    double width = _watermarkFormOptions.Screen.Bounds.Width;
    //    double height = _watermarkFormOptions.Screen.Bounds.Height;
    //    double diagonalSize = Math.Sqrt(Math.Pow(width, 2) + Math.Pow(height, 2));

    //    StringBuilder formatedText = new StringBuilder();
    //    for (int i = 0; i < 100; i++)
    //    {
    //        formatedText.Append(watermarkBaseString);
    //    }

    //    _logger.LogTrace(_loggerExecutedText, nameof(GetFormattedWatermarkText), Text);

    //    return formatedText.ToString();
    //}

    //private void DrawWatermarkTextOld(Graphics graphics, string text)
    //{
    //    _logger.LogTrace(_loggerExecutingText, nameof(DrawWatermarkText), Text);

    //    ArgumentNullException.ThrowIfNull(graphics);

    //    if (string.IsNullOrEmpty(text))
    //    {
    //        throw new ArgumentException("Watermark text cannot be NULL or empty.", nameof(text));
    //    }

    //    graphics.TextRenderingHint = _context.Format.TextRender;
    //    graphics.SmoothingMode = SmoothingMode.Default;
    //    graphics.InterpolationMode = InterpolationMode.Default;

    //    using var font = new Font(_context.Format.FontName, _context.Format.FontSize, FontStyle.Regular, GraphicsUnit.Point);
    //    using var brush = new SolidBrush(Color.FromArgb(255, _context.Format.Color));
    //    using var stringFormat = new StringFormat(StringFormatFlags.NoClip);


    //    graphics.ResetTransform();
    //    if (_context.Format.UseDiagonalLines)
    //    {
    //        graphics.RotateTransform(-45);
    //    }

    //    double screenDiagonal = Math.Round(Math.Sqrt(Math.Pow(_watermarkFormOptions.Screen.WorkingArea.Width, 2) + Math.Pow(_watermarkFormOptions.Screen.WorkingArea.Height, 2)));

    //    int repeatCounts = _context.Format.LinesCount;

    //    int x = _context.Format.UseDiagonalLines ? (int)screenDiagonal : Bounds.Height;

    //    int spacer = x / repeatCounts;

    //    int firstPoint = _context.Format.UseDynamicsSpacing ? new Random().Next(-2, 2) : 0;

    //    for (int i = firstPoint; i < repeatCounts + 2 + firstPoint; i++)
    //    {
    //        var point = new Point(-spacer * i, spacer * i);

    //        if (_context.Format.OutlineColor == null)
    //        {
    //            graphics.DrawString(text, font, brush, point, stringFormat);
    //        }
    //        else
    //        {
    //            using GraphicsPath graphicPath = new GraphicsPath();
    //            using Pen pen = new Pen(_context.Format.OutlineColor.Value, _context.Format.OutlineWidth);
    //            graphicPath.AddString(text, font.FontFamily, (int)font.Style, font.Size * 1.5f, point, stringFormat);

    //            graphics.DrawPath(pen, graphicPath);
    //            graphics.FillPath(brush, graphicPath);
    //        }
    //    }

    //    _logger.LogTrace(_loggerExecutedText, nameof(DrawWatermarkText), Text);
    //}

    private void TimeProcessPolicyCheck_Tick(object sender, EventArgs e)
    {
#if !(DEBUG)
        _logger.LogTrace(_loggerExecutingText, nameof(TimeProcessPolicyCheck_Tick), Text);
#endif

        var allowedProcessesList = _processAccessPolicyOptions.AllowedProcessesList.Select(p => p.ToUpperInvariant()).ToArray();

        if (allowedProcessesList.Length == 0)
        {
            Opacity = _context.Format.OpacityF;
            return;
        }

        var processes = Process
            .GetProcesses()
            .Where(p => allowedProcessesList.Contains(p.ProcessName.ToUpperInvariant()) && p.MainWindowHandle != IntPtr.Zero)
            .ToArray();

        foreach (var process in processes)
        {
            var isMinimized = IsIconic(process.MainWindowHandle);
            if (!isMinimized)
            {
                Opacity = _context.Format.OpacityF;
                return;
            }
        }

        Opacity = 0f;

#if !(DEBUG)
        _logger.LogTrace(_loggerExecutedText, nameof(TimeProcessPolicyCheck_Tick), Text);
#endif

    }
}
