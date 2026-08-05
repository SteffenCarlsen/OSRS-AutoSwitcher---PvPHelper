using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using PvPHelper.App.Services;
using PvPHelper.App.ViewModels;
using PvPHelper.App.Views;
using PvPHelper.Core.Input;
using PvPHelper.Core.Persistence;

namespace ScreenshotTool;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var outDir = args.Length > 0
            ? args[0]
            : Path.Combine(Directory.GetCurrentDirectory(), "docs", "screenshots");
        Directory.CreateDirectory(outDir);

        AppBuilder.Configure<ScreenshotApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .UseSkia()
            .WithInterFont()
            .SetupWithoutStarting();

        var tempDir = Path.Combine(Path.GetTempPath(), "pvph-shots-" + Guid.NewGuid().ToString("N"));
        var store = new ProfileStore(tempDir);
        var session = new SessionState(store);
        var hook = new LowLevelHotkeyHook();     // not started
        var cursor = new Win32InputSender();
        var locator = new GameWindowLocator();

        var main = new MainWindowViewModel(
            session,
            new DashboardViewModel(session),
            new AutoSwitchViewModel(session),
            new PrayersViewModel(session),
            new SpecViewModel(session),
            new HotkeysViewModel(session),
            new ProfilesViewModel(session),
            new CalibrationViewModel(session, hook, cursor),
            new SettingsViewModel(session, locator));

        var window = new MainWindow { DataContext = main, Width = 1240, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        void Capture(string name)
        {
            for (var i = 0; i < 3; i++)
            {
                Dispatcher.UIThread.RunJobs();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            }

            var frame = window.CaptureRenderedFrame();
            var path = Path.Combine(outDir, name + ".png");
            if (frame is null)
            {
                Console.WriteLine($"NULL frame for {name}");
                return;
            }

            frame.Save(path);
            Console.WriteLine($"saved {path}");
        }

        foreach (var nav in main.CombatNav.Concat(main.ConfigNav))
        {
            nav.SelectCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();
            Capture(Slug(nav.Label));
        }

        // Armed dashboard (glow state)
        main.CombatNav[0].SelectCommand.Execute(null);
        session.IsArmed = true;
        Dispatcher.UIThread.RunJobs();
        Capture("dashboard-armed");
        session.IsArmed = false;

        Console.WriteLine("done");
    }

    private static string Slug(string label)
    {
        var chars = label.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();
        return new string(chars).Trim('-');
    }
}
