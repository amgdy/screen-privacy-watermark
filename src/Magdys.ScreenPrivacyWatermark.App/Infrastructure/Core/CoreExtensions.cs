using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Caching;
using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Core;

namespace Magdys.ScreenPrivacyWatermark.App;

internal static class CoreExtensions
{
    public static HostApplicationBuilder ConfigureWinForms<TForm>(this HostApplicationBuilder hostApplicationBuilder, Action<CoreOptions>? configureOptions) where TForm : Form, IMainForm
    {
        var options = new CoreOptions();
        configureOptions?.Invoke(options);
        hostApplicationBuilder.Services.AddSingleton<ConnectivityService>();
        hostApplicationBuilder.Services.AddSingleton(options);
        hostApplicationBuilder.Services.AddSingleton<TForm>();
        hostApplicationBuilder.Services.AddSingleton<IMainForm, TForm>();
        hostApplicationBuilder.Services.AddHostedService<CoreHostedService>();
        return hostApplicationBuilder;
    }

    public static HostApplicationBuilder ConfigureWinForms<TMainForm, TOtherForm1>(this HostApplicationBuilder hostApplicationBuilder, Action<CoreOptions>? configureOptions) where TMainForm : Form, IMainForm where TOtherForm1 : Form
    {
        hostApplicationBuilder.ConfigureWinForms<TMainForm>(configureOptions);
        hostApplicationBuilder.Services.AddTransient<TOtherForm1>();
        return hostApplicationBuilder;
    }

    public static HostApplicationBuilder ConfigureWinForms<TMainForm, TOtherForm1, TOtherForm2>(this HostApplicationBuilder hostApplicationBuilder, Action<CoreOptions>? configureOptions) where TMainForm : Form, IMainForm where TOtherForm1 : Form where TOtherForm2 : Form
    {
        hostApplicationBuilder.ConfigureWinForms<TMainForm, TOtherForm1>(configureOptions);
        hostApplicationBuilder.Services.AddTransient<TOtherForm2>();
        return hostApplicationBuilder;
    }

    public static HostApplicationBuilder ConfigureWinForms<TMainForm, TOtherForm1, TOtherForm2, TOtherForm3>(this HostApplicationBuilder hostApplicationBuilder, Action<CoreOptions>? configureOptions) where TMainForm : Form, IMainForm where TOtherForm1 : Form where TOtherForm2 : Form where TOtherForm3 : Form
    {
        hostApplicationBuilder.ConfigureWinForms<TMainForm, TOtherForm1, TOtherForm2>(configureOptions);
        hostApplicationBuilder.Services.AddTransient<TOtherForm3>();
        return hostApplicationBuilder;
    }

    public static HostApplicationBuilder ConfigureWinForms<TMainForm, TOtherForm1, TOtherForm2, TOtherForm3, TOtherForm4>(this HostApplicationBuilder hostApplicationBuilder, Action<CoreOptions>? configureOptions) where TMainForm : Form, IMainForm where TOtherForm1 : Form where TOtherForm2 : Form where TOtherForm3 : Form where TOtherForm4 : Form
    {
        hostApplicationBuilder.ConfigureWinForms<TMainForm, TOtherForm1, TOtherForm2, TOtherForm3>(configureOptions);
        hostApplicationBuilder.Services.AddTransient<TOtherForm4>();
        return hostApplicationBuilder;
    }
}
