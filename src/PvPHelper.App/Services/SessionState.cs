using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PvPHelper.Core.Models;

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

    public Profile Profile { get; }

    public SessionState()
    {
        Profile = SampleData.CreateDefaultProfile();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => RunningTime = (DateTime.Now - _armedAt).ToString(@"hh\:mm\:ss");
    }

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
