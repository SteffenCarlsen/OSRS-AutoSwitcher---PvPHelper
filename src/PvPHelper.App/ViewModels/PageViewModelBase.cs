using CommunityToolkit.Mvvm.ComponentModel;

namespace PvPHelper.App.ViewModels;

/// <summary>Base for every navigable page shown in the main content area.</summary>
public abstract class PageViewModelBase : ObservableObject
{
    public abstract string Title { get; }
    public virtual string Subtitle => string.Empty;
    public virtual string Glyph => "✦";

    /// <summary>Placeholder copy for sections whose full UI lands in a later phase.</summary>
    public virtual string Blurb => "This section is being built out on this branch.";

    /// <summary>Called each time the page becomes the active content — refresh from the profile here.</summary>
    public virtual void OnNavigatedTo() { }
}
