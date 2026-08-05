using PvPHelper.Core.Input;
using PvPHelper.Core.Models;

namespace PvPHelper.App.Services;

/// <summary>Reports the active profile's input mode to the Core input router.</summary>
public sealed class AppInputModeProvider : IInputModeProvider
{
    private readonly SessionState _session;

    public AppInputModeProvider(SessionState session) => _session = session;

    public InputMode Mode => _session.Profile.InputMode;
}
