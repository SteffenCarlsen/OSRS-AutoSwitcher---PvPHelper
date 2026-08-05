namespace PvPHelper.App.ViewModels;

// Lightweight placeholders for sections whose full UI arrives in later phases.
// Each still routes through the sidebar + ViewLocator and renders the themed PlaceholderView.

public sealed class ProfilesViewModel : PageViewModelBase
{
    public override string Title => "Profiles";
    public override string Subtitle => "Presets you can hot-swap mid-fight";
    public override string Glyph => "⛃";
    public override string Blurb =>
        "Save, load, duplicate, import and export presets — plus one-click import of legacy console configs.";
}
