using System.Text.Json;
using System.Text.Json.Serialization;
using PvPHelper.Core.Models;

namespace PvPHelper.Core.Persistence;

/// <summary>Reads and writes <see cref="Profile"/> presets as JSON files under %AppData%.</summary>
public sealed class ProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public ProfileStore(string? directory = null)
    {
        Directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OSRS-PvPHelper", "profiles");
        System.IO.Directory.CreateDirectory(Directory);
    }

    /// <summary>Folder where profile JSON files live.</summary>
    public string Directory { get; }

    public IReadOnlyList<string> ListNames() =>
        System.IO.Directory.EnumerateFiles(Directory, "*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Where(n => !string.IsNullOrEmpty(n))
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList()!;

    public bool Exists(string name) => File.Exists(PathFor(name));

    public Profile? Load(string name)
    {
        var path = PathFor(name);
        if (!File.Exists(path)) return null;

        using var stream = File.OpenRead(path);
        var profile = JsonSerializer.Deserialize<Profile>(stream, JsonOptions);
        if (profile is not null && string.IsNullOrWhiteSpace(profile.Name))
            profile.Name = name;
        return profile;
    }

    public void Save(Profile profile)
    {
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        File.WriteAllText(PathFor(profile.Name), json);
    }

    public void Delete(string name)
    {
        var path = PathFor(name);
        if (File.Exists(path)) File.Delete(path);
    }

    private string PathFor(string name) => Path.Combine(Directory, Sanitize(name) + ".json");

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        return string.IsNullOrEmpty(cleaned) ? "profile" : cleaned;
    }
}
