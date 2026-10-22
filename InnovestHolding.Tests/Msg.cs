using System.Globalization;
using System.Resources;

namespace InnovestHolding.Tests;

/// <summary>Reads the app's resx (Resources/SharedResource[.en].resx) the way the runtime does.</summary>
public static class Msg
{
    public static readonly ResourceManager Resources =
        new("InnovestHolding.Resources.SharedResource", typeof(SharedResource).Assembly);

    private static readonly CultureInfo Arabic = new("ar");
    private static readonly CultureInfo English = new("en");

    /// <summary>The Arabic (neutral) text of a key; throws on a missing key so a typo cannot pass silently.</summary>
    public static string Ar(string key) =>
        Resources.GetString(key, Arabic) ?? throw new KeyNotFoundException(key);

    public static string En(string key) =>
        Resources.GetString(key, English) ?? throw new KeyNotFoundException(key);

    public static string Ar(string key, params object[] args) => string.Format(Ar(key), args);
    public static string En(string key, params object[] args) => string.Format(En(key), args);
}
