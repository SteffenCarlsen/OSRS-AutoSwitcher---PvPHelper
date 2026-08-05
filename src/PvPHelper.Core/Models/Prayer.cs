namespace PvPHelper.Core.Models;

/// <summary>
/// The 29 OSRS prayers, in prayer-book order. Integer values are kept identical to the
/// legacy console app so old configs migrate by value.
/// </summary>
public enum Prayer
{
    ThickSkin = 0,
    BurstOfStrength = 1,
    ClarityOfThought = 2,
    SharpEye = 3,
    MysticWill = 4,
    RockSkin = 5,
    SuperhumanStrength = 6,
    ImprovedReflexes = 7,
    RapidRestore = 8,
    RapidHeal = 9,
    ProtectItem = 10,
    HawkEye = 11,
    MysticLore = 12,
    SteelSkin = 13,
    UltimateStrength = 14,
    IncredibleReflexes = 15,
    ProtectFromMagic = 16,
    ProtectFromMissiles = 17,
    ProtectFromMelee = 18,
    EagleEye = 19,
    MysticMight = 20,
    Retribution = 21,
    Redemption = 22,
    Smite = 23,
    Preserve = 24,
    Chivalry = 25,
    Piety = 26,
    Rigour = 27,
    Augury = 28
}

public static class PrayerCatalog
{
    public const int Count = 29;
    public const int Columns = 5;

    public static IReadOnlyList<Prayer> InBookOrder { get; } =
        Enumerable.Range(0, Count).Select(i => (Prayer)i).ToArray();

    public static string DisplayName(Prayer prayer) => prayer switch
    {
        Prayer.ThickSkin => "Thick Skin",
        Prayer.BurstOfStrength => "Burst of Strength",
        Prayer.ClarityOfThought => "Clarity of Thought",
        Prayer.SharpEye => "Sharp Eye",
        Prayer.MysticWill => "Mystic Will",
        Prayer.RockSkin => "Rock Skin",
        Prayer.SuperhumanStrength => "Superhuman Strength",
        Prayer.ImprovedReflexes => "Improved Reflexes",
        Prayer.RapidRestore => "Rapid Restore",
        Prayer.RapidHeal => "Rapid Heal",
        Prayer.ProtectItem => "Protect Item",
        Prayer.HawkEye => "Hawk Eye",
        Prayer.MysticLore => "Mystic Lore",
        Prayer.SteelSkin => "Steel Skin",
        Prayer.UltimateStrength => "Ultimate Strength",
        Prayer.IncredibleReflexes => "Incredible Reflexes",
        Prayer.ProtectFromMagic => "Protect from Magic",
        Prayer.ProtectFromMissiles => "Protect from Missiles",
        Prayer.ProtectFromMelee => "Protect from Melee",
        Prayer.EagleEye => "Eagle Eye",
        Prayer.MysticMight => "Mystic Might",
        Prayer.Retribution => "Retribution",
        Prayer.Redemption => "Redemption",
        Prayer.Smite => "Smite",
        Prayer.Preserve => "Preserve",
        Prayer.Chivalry => "Chivalry",
        Prayer.Piety => "Piety",
        Prayer.Rigour => "Rigour",
        Prayer.Augury => "Augury",
        _ => prayer.ToString()
    };
}
