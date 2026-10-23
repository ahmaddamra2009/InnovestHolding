namespace InnovestHolding.Infrastructure.Content;

/// <summary>
/// The contact phone and the organisation's country code (SEO-C5 §7, CTO decision C5 final): checked when the admin
/// saves them, and turned into clean structured data at render. The visible page text of the phone stays exactly as
/// stored; only the JSON-LD uses <see cref="ToE164"/>.
/// </summary>
public static class ContactFormats
{
    /// <summary>
    /// A phone as an admin types it: an optional leading «+», then digits, spaces and dashes only, with 7–15 digits
    /// (ASCII digits; no letters, no brackets).
    /// </summary>
    public static bool IsPhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var v = value.Trim();
        if (v.StartsWith('+')) v = v[1..];
        if (v.Length == 0 || !char.IsAsciiDigit(v[0])) return false;
        var digits = 0;
        foreach (var ch in v)
        {
            if (char.IsAsciiDigit(ch)) digits++;
            else if (ch is not (' ' or '-')) return false;
        }
        return digits is >= 7 and <= 15;
    }

    // ITU-T E.164 country calling codes are prefix-free: 1 and 7 are the one-digit codes, these are the two-digit
    // ones, every other code has three digits.
    private static readonly HashSet<string> TwoDigitCodes =
    [
        "20", "27", "30", "31", "32", "33", "34", "36", "39", "40", "41", "43", "44", "45", "46", "47", "48", "49",
        "51", "52", "53", "54", "55", "56", "57", "58", "60", "61", "62", "63", "64", "65", "66", "81", "82", "84",
        "86", "90", "91", "92", "93", "94", "95", "98",
    ];

    private static int CountryCodeLength(string digits) =>
        digits[0] is '1' or '7' ? 1 : TwoDigitCodes.Contains(digits[..2]) ? 2 : 3;

    /// <summary>
    /// The stored phone as E.164 (<c>+</c> and 8–15 digits) for schema.org <c>telephone</c>, or null when it cannot be
    /// normalised cleanly (then the JSON-LD leaves <c>telephone</c> out). It needs the country code: a leading «+» or
    /// «00» (→ «+»). A national trunk «0» right after the country code is dropped («+20 0114…» → «+20114…»), except for
    /// Italy (+39), whose numbers keep it.
    /// </summary>
    public static string? ToE164(string? value)
    {
        if (!IsPhone(value)) return null;
        var v = value!.Trim();
        var digits = new string(v.Where(char.IsAsciiDigit).ToArray());
        if (v.StartsWith('+')) { }
        else if (digits.StartsWith("00", StringComparison.Ordinal)) digits = digits[2..];
        else return null; // no country code: not normalisable
        if (digits.Length < 4 || digits[0] == '0') return null;

        var cc = CountryCodeLength(digits);
        var italy = digits[..cc] == "39";
        if (digits[cc] == '0' && !italy) digits = digits[..cc] + digits[(cc + 1)..];
        return digits.Length is >= 8 and <= 15 && (italy || digits[cc] != '0') ? "+" + digits : null;
    }

    // ISO 3166-1 alpha-2, officially assigned codes.
    private static readonly HashSet<string> Countries = new(
        ("AD AE AF AG AI AL AM AO AQ AR AS AT AU AW AX AZ BA BB BD BE BF BG BH BI BJ BL BM BN BO BQ BR BS BT BV BW BY BZ " +
         "CA CC CD CF CG CH CI CK CL CM CN CO CR CU CV CW CX CY CZ DE DJ DK DM DO DZ EC EE EG EH ER ES ET FI FJ FK FM FO FR " +
         "GA GB GD GE GF GG GH GI GL GM GN GP GQ GR GS GT GU GW GY HK HM HN HR HT HU ID IE IL IM IN IO IQ IR IS IT JE JM JO " +
         "JP KE KG KH KI KM KN KP KR KW KY KZ LA LB LC LI LK LR LS LT LU LV LY MA MC MD ME MF MG MH MK ML MM MN MO MP MQ MR " +
         "MS MT MU MV MW MX MY MZ NA NC NE NF NG NI NL NO NP NR NU NZ OM PA PE PF PG PH PK PL PM PN PR PS PT PW PY QA RE RO " +
         "RS RU RW SA SB SC SD SE SG SH SI SJ SK SL SM SN SO SR SS ST SV SX SY SZ TC TD TF TG TH TJ TK TL TM TN TO TR TT TV " +
         "TW TZ UA UG UM US UY UZ VA VC VE VG VI VN VU WF WS YE YT ZA ZM ZW").Split(' '),
        StringComparer.Ordinal);

    /// <summary>The value upper-cased when it is an ISO 3166-1 alpha-2 code (<c>eg</c> → <c>EG</c>), else null.</summary>
    public static string? CountryCode(string? value)
    {
        var v = value?.Trim().ToUpperInvariant();
        return v is { Length: 2 } && Countries.Contains(v) ? v : null;
    }
}
