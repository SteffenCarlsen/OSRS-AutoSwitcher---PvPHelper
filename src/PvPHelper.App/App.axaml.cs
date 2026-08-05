using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PvPHelper.App.Services;
using PvPHelper.App.ViewModels;
using PvPHelper.App.Views;
using PvPHelper.Core.Persistence;

namespace PvPHelper.App;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ProfileStore>();
        services.AddSingleton<SessionState>();

        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<AutoSwitchViewModel>();
        services.AddSingleton<PrayersViewModel>();
        services.AddSingleton<SpecViewModel>();
        services.AddSingleton<HotkeysViewModel>();
        services.AddSingleton<ProfilesViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        var provider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = provider.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
