namespace PvPHelper.App.ViewModels;

// Lightweight placeholders for sections whose full UI arrives in later phases.
// Each still routes through the sidebar + ViewLocator and renders the themed PlaceholderView.

public sealed class PrayersViewModel : PageViewModelBase
{
    public override string Title => "Prayers";
    public override string Subtitle => "Bind a prayer to a key";
    public override string Glyph => "✦";
    public override string Blurb =>
        "The full 29-prayer book laid out as in-game, with search and one-click binding.";
}

public sealed class SpecViewModel : PageViewModelBase
{
    public override string Title => "Spec Helper";
    public override string Subtitle => "Equip a spec weapon, activate, click the target";
    public override string Glyph => "☄";
    public override string Blurb =>
        "Choose the spec weapon slots, wait-for-bar behaviour, and how many times to special-attack.";
}

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
