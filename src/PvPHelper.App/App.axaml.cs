using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PvPHelper.App.Services;
using PvPHelper.App.ViewModels;
using PvPHelper.App.Views;
using PvPHelper.Core.Automation;
using PvPHelper.Core.Input;
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

        // Automation engine
        services.AddSingleton<IInputSender, Win32InputSender>();
        services.AddSingleton<IHotkeyHook, LowLevelHotkeyHook>();
        services.AddSingleton<ActionEngine>();
        services.AddSingleton<ArmController>();

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

            // Global hook runs for the app's lifetime so the arm/panic keys always work.
            var arm = provider.GetRequiredService<ArmController>();
            arm.Start();
            desktop.ShutdownRequested += (_, _) => arm.Stop();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
