using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PvPHelper.Core.Models;
using PvPHelper.Core.Persistence;

namespace PvPHelper.App.Services;

/// <summary>
/// Shared, observable runtime state bridged between the engine and the UI
/// (armed/disarmed, run timer, active profile). Singleton.
/// </summary>
public partial class SessionState : ObservableObject
{
    private readonly DispatcherTimer _timer;
    private DateTime _armedAt;

    [ObservableProperty]
    private bool _isArmed;

    [ObservableProperty]
    private string _runningTime = "00:00:00";

    [ObservableProperty]
    private string _armLabel = "Disarmed";

    private readonly ProfileStore _store;

    public Profile Profile { get; }

    public SessionState(ProfileStore store)
    {
        _store = store;

        // Load the first saved profile, or seed a populated default on first run.
        var names = store.ListNames();
        Profile = (names.Count > 0 ? store.Load(names[0]) : null) ?? SampleData.CreateDefaultProfile();
        if (names.Count == 0) store.Save(Profile);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => RunningTime = (DateTime.Now - _armedAt).ToString(@"hh\:mm\:ss");
    }

    /// <summary>Persist the active profile to disk.</summary>
    public void Save() => _store.Save(Profile);

    partial void OnIsArmedChanged(bool value)
    {
        ArmLabel = value ? "Armed" : "Disarmed";
        if (value)
        {
            _armedAt = DateTime.Now;
            RunningTime = "00:00:00";
            _timer.Start();
        }
        else
        {
            _timer.Stop();
            RunningTime = "00:00:00";
        }
    }

    [RelayCommand]
    private void ToggleArm() => IsArmed = !IsArmed;
}
