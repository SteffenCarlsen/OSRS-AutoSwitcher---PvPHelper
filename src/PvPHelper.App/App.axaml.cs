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

        // Automation engine — foreground (SendInput) and background (PostMessage) senders,
        // dispatched by the active profile's input mode.
        services.AddSingleton<Win32InputSender>();
        services.AddSingleton<GameWindowLocator>();
        services.AddSingleton<IGameWindowProvider>(sp => sp.GetRequiredService<GameWindowLocator>());
        services.AddSingleton<PostMessageInputSender>();
        services.AddSingleton<IInputModeProvider, AppInputModeProvider>();
        services.AddSingleton<IInputSender>(sp => new InputRouter(
            sp.GetRequiredService<IInputModeProvider>(),
            sp.GetRequiredService<Win32InputSender>(),
            sp.GetRequiredService<PostMessageInputSender>()));
        services.AddSingleton<IHotkeyHook, LowLevelHotkeyHook>();
        services.AddSingleton<ActionEngine>();
        services.AddSingleton<ArmController>();

        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<AutoSwitchViewModel>();
        services.AddSingleton<PrayersViewModel>();
        services.AddSingleton<SpecViewModel>();
        services.AddSingleton<HotkeysViewModel>();
        services.AddSingleton<ProfilesViewModel>();
        services.AddSingleton<CalibrationViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        var provider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = provider.GetRequiredService<MainWindowViewModel>()
            };

            // Point the background-input locator at the profile's configured window (if any).
            var session = provider.GetRequiredService<SessionState>();
            provider.GetRequiredService<GameWindowLocator>().UseTitle(session.Profile.TargetWindowTitle);

            // Global hook runs for the app's lifetime so the arm/panic keys always work.
            var arm = provider.GetRequiredService<ArmController>();
            arm.Start();
            desktop.ShutdownRequested += (_, _) => arm.Stop();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
