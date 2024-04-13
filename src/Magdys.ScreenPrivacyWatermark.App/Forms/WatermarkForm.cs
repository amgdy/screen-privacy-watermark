using Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;
using Magdys.ScreenPrivacyWatermark.App.Watermark.Options;
using Microsoft.Extensions.Options;
using System.Drawing.Drawing2D;
using System.Text;
using static Magdys.ScreenPrivacyWatermark.App.NativeMethods;

namespace Magdys.ScreenPrivacyWatermark.App.Forms;

public partial class WatermarkForm : Form
{
    private readonly ILogger<WatermarkForm> _logger;
    private readonly IOptions<WatermarkFormatOptions> _watermarkFormatSetting;
    private bool _isFormClosingSubscribed = true;
    private bool _isInitialized = false;
    private WatermarkFormOptions _watermarkFormOptions = default!;
    private const string _loggerExecutingText = "Executing {Method} for '{text}'";
    private const string _loggerExecutedText = "Executed {Method} for '{text}'";

    public WatermarkForm(ILogger<WatermarkForm> logger, IOptions<WatermarkFormatOptions> watermarkFormatSetting)
    {
        InitializeComponent();
        _logger = logger;
        _watermarkFormatSetting = watermarkFormatSetting;
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
            FormClosing -= WatermarkForm_FormClosing!;
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

        _logger.LogDebug("Form closing reason: {Reason}", e.CloseReason);

        switch (e.CloseReason)
        {
            case CloseReason.None:
            case CloseReason.MdiFormClosing:
            case CloseReason.UserClosing:
            case CloseReason.TaskManagerClosing:
            //case CloseReason.ApplicationExitCall:
                e.Cancel = true;
                break;
        }

        _logger.LogTrace(_loggerExecutedText, nameof(WatermarkForm_FormClosing), Text);
    }

    private void WatermarkForm_Load(object sender, EventArgs e)
    {
        _logger.LogTrace(_loggerExecutingText, nameof(WatermarkForm_Load), Text);
        if (!_isInitialized)
        {
            throw new InvalidOperationException($"Form is not initialized. You need to call {nameof(InitializeForm)} method first.");
        }

        // Means the process access policy is enabled.
        if (ProcessAccessPolicyState.HideWatermark.HasValue)
        {
            _logger.LogDebug("Process Access Policy is enabled.");
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

        graphics.TextRenderingHint = _watermarkFormatSetting.Value.TextRender;
        graphics.SmoothingMode = SmoothingMode.Default;
        graphics.InterpolationMode = InterpolationMode.Default;
        var watermarkDrawingArea = _watermarkFormOptions.Screen.WorkingArea;

        double width = watermarkDrawingArea.Width;
        double height = watermarkDrawingArea.Height;
        double diagonalSize = Math.Sqrt(Math.Pow(width, 2) + Math.Pow(height, 2));

        string watermarkText = _watermarkFormOptions.WatermarkText;
        string watermarkTextSpacer = new(' ', _watermarkFormatSetting.Value.UseDynamicsSpacing ? new Random().Next(6, 15) : 10);

        string watermarkBaseString = $"{watermarkText} {watermarkTextSpacer}";

        using var font = new Font(_watermarkFormatSetting.Value.FontName, _watermarkFormatSetting.Value.FontSize, FontStyle.Regular, GraphicsUnit.Point);
        using var brush = new SolidBrush(Color.FromArgb(255, _watermarkFormatSetting.Value.Color));
        using var stringFormat = new StringFormat(StringFormatFlags.NoClip);

        var watermarkTextBuilder = new StringBuilder(watermarkBaseString);

        // generating the watermark text to fit the screen diagonal size
        while (graphics.MeasureString(watermarkTextBuilder.ToString(), font).Width <= (diagonalSize * 1.2))
        {
            watermarkTextBuilder.Append(watermarkBaseString);
        }

        var watermarkLineText = watermarkTextBuilder.ToString();

        // Calculate the spacing between lines
        var numberOfLines = _watermarkFormatSetting.Value.LinesCount + 2;
        double lineSpacing = (diagonalSize * 1.7) / numberOfLines;

        lineSpacing *= 1.0;

        for (int i = 0; i < numberOfLines; i++)
        {
            // Rotate the graphics to draw diagonally
            graphics.TranslateTransform(0, (float)(i * lineSpacing));
            graphics.RotateTransform(-45);
            var point = new PointF(0, 0);

            if (_watermarkFormatSetting.Value.OutlineColor == null)
            {
                graphics.DrawString(watermarkLineText, font, brush, point, stringFormat);
            }
            else
            {
                using var graphicPath = new GraphicsPath();
                using var pen = new Pen(_watermarkFormatSetting.Value.OutlineColor.Value, _watermarkFormatSetting.Value.OutlineWidth);
                graphicPath.AddString(watermarkLineText, font.FontFamily, (int)font.Style, font.Size * 1.5f, point, stringFormat);

                graphics.DrawPath(pen, graphicPath);
                graphics.FillPath(brush, graphicPath);
            }


            // Reset the transformation for the next line
            graphics.ResetTransform();
        }

        _logger.LogTrace(_loggerExecutedText, nameof(DrawWatermarkText), Text);
    }

    private void TimeProcessPolicyCheck_Tick(object sender, EventArgs e)
    {
#if !(DEBUG)
        _logger.LogTrace(_loggerExecutingText, nameof(TimeProcessPolicyCheck_Tick), Text);
#endif

        if (ProcessAccessPolicyState.HideWatermark == null)
        {
            // this should not happen since the timer should be disabled if the HideWatermark is null
            return;
        }

        if (ProcessAccessPolicyState.HideWatermark.Value)
        {
            Opacity = 0f;
        }
        else
        {
            Opacity = _watermarkFormatSetting.Value.OpacityF;
        }


#if !(DEBUG)
        _logger.LogTrace(_loggerExecutedText, nameof(TimeProcessPolicyCheck_Tick), Text);
#endif

    }
}
