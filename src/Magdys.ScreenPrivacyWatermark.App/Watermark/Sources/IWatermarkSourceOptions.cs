using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magdys.ScreenPrivacyWatermark.App.Watermark.Sources;

public interface IWatermarkSourceOptions
{
    bool Enabled { get; set; }
}
