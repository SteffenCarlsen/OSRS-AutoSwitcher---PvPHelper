using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PvPHelper.App.ViewModels;

/// <summary>A single sidebar navigation entry.</summary>
public partial class NavItem : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    public string Label { get; }
    public string Glyph { get; }
    public string Group { get; }
    public PageViewModelBase Page { get; }
    public ICommand SelectCommand { get; }

    public NavItem(string label, string glyph, string group, PageViewModelBase page, Action<NavItem> onSelect)
    {
        Label = label;
        Glyph = glyph;
        Group = group;
        Page = page;
        SelectCommand = new RelayCommand(() => onSelect(this));
    }
}
