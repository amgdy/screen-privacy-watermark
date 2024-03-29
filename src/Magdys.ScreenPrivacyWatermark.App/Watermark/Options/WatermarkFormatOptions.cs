using System.ComponentModel.DataAnnotations;
using System.Drawing.Text;

namespace Magdys.ScreenPrivacyWatermark.App.Watermark.Options;

public class WatermarkFormatOptions
{
    public const string SectionName = "Format";

    [Range(1, 120)]
    public float FontSize { get; set; } = 16f;

    [Required]
    [MinLength(2)]
    public string FontName { get; set; } = "Segoe UI";

    [Range(0, 100)]
    public int Opacity { get; set; } = 40;

    public float OpacityF => (100 - Opacity) / 100f;

    public Color Color { get; set; } = Color.Gray;

    public Color? OutlineColor { get; set; }

    [Range(0f, 20f)]
    public float OutlineWidth { get; set; } = 0.5f;

    public bool UseDynamicsSpacing { get; set; } = false;

    [Range(1, 100)]
    public int LinesCount { get; set; } = 8;

    public TextRenderingHint TextRender
    {
        get;
#if DEBUG
        set; // setter only while debugging
#endif
    } = TextRenderingHint.SingleBitPerPixel;

    public bool UseDiagonalLines
    {
        get;
#if DEBUG
        set; // setter only while debugging
#endif
    } = true;

}

