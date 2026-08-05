using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using PvPHelper.Core.Automation;
using PvPHelper.Core.Input;
using PvPHelper.Core.Models;

namespace PvPHelper.App.Services;

/// <summary>
/// Bridges the global hotkey hook to the action engine. The hook runs for the app's
/// lifetime so the arm/panic keys always work; combat actions only fire while armed.
/// An atomic single-flight guard prevents a second hotkey from interleaving an in-progress
/// sequence, and executed actions are pushed to the activity feed.
/// </summary>
public sealed class ArmController
{
    private readonly SessionState _session;
    private readonly IHotkeyHook _hook;
    private readonly ActionEngine _engine;
    private int _busy; // 0 = idle, 1 = a sequence is running

    public ArmController(SessionState session, IHotkeyHook hook, ActionEngine engine)
    {
        _session = session;
        _hook = hook;
        _engine = engine;
    }

    public void Start()
    {
        _hook.HotkeyReleased += OnHotkey;
        _hook.Start();
    }

    public void Stop()
    {
        _hook.HotkeyReleased -= OnHotkey;
        _hook.Stop();
    }

    private void OnHotkey(HotkeyEvent ev)
    {
        // While calibrating, the CalibrationViewModel consumes the keypress to sample the cursor.
        if (_session.IsCalibrating) return;

        var profile = _session.Profile;

        if (ev.Token == profile.ArmHotkey)
        {
            Dispatcher.UIThread.Post(() => _session.IsArmed = !_session.IsArmed);
            return;
        }

        if (ev.Token == profile.PanicKey)
        {
            Dispatcher.UIThread.Post(() => _session.IsArmed = false);
            return;
        }

        if (!_session.IsArmed) return;

        // Atomic single-flight guard: bail if a sequence is already running.
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return;

        Task.Run(() =>
        {
            try
            {
                var actions = _engine.Handle(profile, ev.Token);
                if (actions.Count > 0)
                    Dispatcher.UIThread.Post(() =>
                    {
                        foreach (var action in actions) _session.Log(action);
                    });
            }
            finally
            {
                Interlocked.Exchange(ref _busy, 0);
            }
        });
    }
}
