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

    /// <summary>While true, hotkeys are consumed by calibration capture, not the engine.</summary>
    [ObservableProperty]
    private bool _isCalibrating;

    private readonly ProfileStore _store;

    [ObservableProperty]
    private Profile _profile = null!;

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

    public string StoreDirectory => _store.Directory;

    public System.Collections.Generic.IReadOnlyList<string> ProfileNames() => _store.ListNames();

    /// <summary>Persist the active profile to disk.</summary>
    public void Save() => _store.Save(Profile);

    /// <summary>Make a saved profile the active one (hot-swap).</summary>
    public void SwitchTo(string name)
    {
        var loaded = _store.Load(name);
        if (loaded is not null) Profile = loaded;
    }

    /// <summary>Create a new empty profile, save it, and make it active.</summary>
    public void CreateNew(string name)
    {
        var profile = new Profile { Name = name };
        _store.Save(profile);
        Profile = profile;
    }

    /// <summary>Duplicate a saved profile under a new name (does not switch to it).</summary>
    public void Duplicate(string sourceName, string newName)
    {
        var clone = _store.Load(sourceName);
        if (clone is null) return;
        clone.Name = newName;
        _store.Save(clone);
    }

    public void Delete(string name) => _store.Delete(name);

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
