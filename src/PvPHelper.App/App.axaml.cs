using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
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
            var session = provider.GetRequiredService<SessionState>();

            var mainWindow = new MainWindow
            {
                DataContext = provider.GetRequiredService<MainWindowViewModel>()
            };
            desktop.MainWindow = mainWindow;

            // Run in the tray: closing the window hides it; Quit shuts down explicitly.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            WindowIcon? icon = null;
            try { icon = new WindowIcon(AssetLoader.Open(new Uri("avares://OSRS-PvPHelper/Assets/app.ico"))); }
            catch { /* icon is optional */ }
            if (icon is not null) mainWindow.Icon = icon;

            // Always-on-top status HUD, shown while armed.
            var hud = new HudWindow { DataContext = session };
            session.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(SessionState.IsArmed)) return;
                if (session.IsArmed) hud.Show(); else hud.Hide();
            };

            // System tray.
            var tray = new TrayIcon { ToolTipText = "OSRS PvP Helper", IsVisible = true };
            if (icon is not null) tray.Icon = icon;
            tray.Clicked += (_, _) => { mainWindow.Show(); mainWindow.Activate(); };

            var showItem = new NativeMenuItem("Show window");
            showItem.Click += (_, _) => { mainWindow.Show(); mainWindow.Activate(); };
            var armItem = new NativeMenuItem("Arm / Disarm");
            armItem.Click += (_, _) => session.IsArmed = !session.IsArmed;
            var quitItem = new NativeMenuItem("Quit");
            quitItem.Click += (_, _) => { mainWindow.AllowClose(); desktop.Shutdown(); };

            var menu = new NativeMenu();
            menu.Items.Add(showItem);
            menu.Items.Add(armItem);
            menu.Items.Add(new NativeMenuItemSeparator());
            menu.Items.Add(quitItem);
            tray.Menu = menu;
            TrayIcon.SetIcons(this, new TrayIcons { tray });

            // Point the background-input locator at the profile's configured window (if any).
            provider.GetRequiredService<GameWindowLocator>().UseTitle(session.Profile.TargetWindowTitle);

            // Global hook runs for the app's lifetime so the arm/panic keys always work.
            var arm = provider.GetRequiredService<ArmController>();
            arm.Start();
            desktop.ShutdownRequested += (_, _) => arm.Stop();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
