using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PvPHelper.App.Services;

namespace PvPHelper.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public SessionState Session { get; }

    public ObservableCollection<NavItem> CombatNav { get; } = new();
    public ObservableCollection<NavItem> ConfigNav { get; } = new();

    [ObservableProperty]
    private PageViewModelBase? _currentPage;

    public MainWindowViewModel(
        SessionState session,
        DashboardViewModel dashboard,
        AutoSwitchViewModel autoSwitch,
        PrayersViewModel prayers,
        SpecViewModel spec,
        HotkeysViewModel hotkeys,
        ProfilesViewModel profiles,
        CalibrationViewModel calibration,
        SettingsViewModel settings)
    {
        Session = session;

        CombatNav.Add(new NavItem("Dashboard", "◈", "Combat", dashboard, Select));
        CombatNav.Add(new NavItem("Auto-Switch", "⚔", "Combat", autoSwitch, Select));
        CombatNav.Add(new NavItem("Prayers", "✦", "Combat", prayers, Select));
        CombatNav.Add(new NavItem("Spec Helper", "☄", "Combat", spec, Select));

        ConfigNav.Add(new NavItem("Calibration", "◎", "Config", calibration, Select));
        ConfigNav.Add(new NavItem("Hotkeys", "⌨", "Config", hotkeys, Select));
        ConfigNav.Add(new NavItem("Profiles", "⛃", "Config", profiles, Select));
        ConfigNav.Add(new NavItem("Settings", "⚙", "Config", settings, Select));

        Select(CombatNav[0]);
    }

    private void Select(NavItem item)
    {
        foreach (var n in CombatNav) n.IsSelected = ReferenceEquals(n, item);
        foreach (var n in ConfigNav) n.IsSelected = ReferenceEquals(n, item);
        CurrentPage = item.Page;
        item.Page.OnNavigatedTo();
    }
}
