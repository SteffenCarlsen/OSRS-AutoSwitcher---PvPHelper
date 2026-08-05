using System.Threading.Tasks;
using Avalonia.Threading;
using PvPHelper.Core.Automation;
using PvPHelper.Core.Input;
using PvPHelper.Core.Models;

namespace PvPHelper.App.Services;

/// <summary>
/// Bridges the global hotkey hook to the action engine. The hook runs for the app's
/// lifetime so the arm/panic keys always work; combat actions only fire while armed.
/// A single-flight guard prevents a second hotkey from interleaving an in-progress sequence.
/// </summary>
public sealed class ArmController
{
    private readonly SessionState _session;
    private readonly IHotkeyHook _hook;
    private readonly ActionEngine _engine;
    private volatile bool _busy;

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

        if (!_session.IsArmed || _busy) return;

        _busy = true;
        Task.Run(() =>
        {
            try { _engine.Handle(profile, ev.Token); }
            finally { _busy = false; }
        });
    }
}
