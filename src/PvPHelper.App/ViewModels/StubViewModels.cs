namespace PvPHelper.App.ViewModels;

// Lightweight placeholders for sections whose full UI arrives in later phases.
// Each still routes through the sidebar + ViewLocator and renders the themed PlaceholderView.

public sealed class HotkeysViewModel : PageViewModelBase
{
    public override string Title => "Hotkeys";
    public override string Subtitle => "Every binding in one place · conflicts flagged";
    public override string Glyph => "⌨";
    public override string Blurb =>
        "A single table of all bindings plus the master arm and panic/stop keys, with conflict detection.";
}

public sealed class ProfilesViewModel : PageViewModelBase
{
    public override string Title => "Profiles";
    public override string Subtitle => "Presets you can hot-swap mid-fight";
    public override string Glyph => "⛃";
    public override string Blurb =>
        "Save, load, duplicate, import and export presets — plus one-click import of legacy console configs.";
}
