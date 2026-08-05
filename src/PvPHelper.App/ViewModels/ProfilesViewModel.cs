using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PvPHelper.App.Services;

namespace PvPHelper.App.ViewModels;

public sealed partial class ProfilesViewModel : PageViewModelBase
{
    private readonly SessionState _session;

    public override string Title => "Profiles";
    public override string Subtitle => "Presets you can hot-swap";
    public override string Glyph => "⛃";

    public ObservableCollection<ProfileItemVm> Profiles { get; } = new();

    public ProfilesViewModel(SessionState session)
    {
        _session = session;
        Reload();
    }

    public override void OnNavigatedTo() => Reload();

    private void Reload()
    {
        Profiles.Clear();
        var active = _session.Profile.Name;
        foreach (var name in _session.ProfileNames())
            Profiles.Add(new ProfileItemVm(
                name,
                string.Equals(name, active, StringComparison.OrdinalIgnoreCase),
                LoadItem, DuplicateItem, DeleteItem));
    }

    private void LoadItem(ProfileItemVm item)
    {
        _session.SwitchTo(item.Name);
        Reload();
    }

    private void DuplicateItem(ProfileItemVm item)
    {
        _session.Duplicate(item.Name, UniqueName(item.Name + " copy"));
        Reload();
    }

    private void DeleteItem(ProfileItemVm item)
    {
        if (item.IsActive) return; // can't delete the active profile
        _session.Delete(item.Name);
        Reload();
    }

    [RelayCommand]
    private void NewProfile()
    {
        _session.CreateNew(UniqueName("New profile"));
        Reload();
    }

    [RelayCommand]
    private void OpenFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = _session.StoreDirectory, UseShellExecute = true });
        }
        catch
        {
            // best-effort; ignore if the shell can't open the folder
        }
    }

    private string UniqueName(string basis)
    {
        var existing = _session.ProfileNames().ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!existing.Contains(basis)) return basis;
        for (var i = 2; i < 1000; i++)
        {
            var candidate = $"{basis} {i}";
            if (!existing.Contains(candidate)) return candidate;
        }

        return basis + " new";
    }
}

public sealed class ProfileItemVm
{
    public string Name { get; }
    public bool IsActive { get; }
    public ICommand LoadCommand { get; }
    public ICommand DuplicateCommand { get; }
    public ICommand DeleteCommand { get; }

    public ProfileItemVm(string name, bool isActive,
        Action<ProfileItemVm> load, Action<ProfileItemVm> duplicate, Action<ProfileItemVm> delete)
    {
        Name = name;
        IsActive = isActive;
        LoadCommand = new RelayCommand(() => load(this));
        DuplicateCommand = new RelayCommand(() => duplicate(this));
        DeleteCommand = new RelayCommand(() => delete(this));
    }
}
