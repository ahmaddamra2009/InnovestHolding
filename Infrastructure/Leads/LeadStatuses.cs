using System.Globalization;
using System.Text;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;

namespace InnovestHolding.Infrastructure.Leads;

/// <summary>The lead workflow states: URL/form keys and resx label keys (the enum itself is in Models/Leads.cs).</summary>
public static class LeadStatuses
{
    public static readonly IReadOnlyList<LeadStatus> All = [LeadStatus.New, LeadStatus.Contacted, LeadStatus.Closed];

    /// <summary>The key used in query strings and forms: new / contacted / closed.</summary>
    public static string Key(LeadStatus status) => status switch
    {
        LeadStatus.Contacted => "contacted",
        LeadStatus.Closed => "closed",
        _ => "new",
    };

    /// <summary>The status of a key, or null for anything else (numbers included: no enum parsing of request values).</summary>
    public static LeadStatus? Parse(string? key) => key switch
    {
        "new" => LeadStatus.New,
        "contacted" => LeadStatus.Contacted,
        "closed" => LeadStatus.Closed,
        _ => null,
    };

    public static string LabelKey(LeadStatus status) => status switch
    {
        LeadStatus.Contacted => MessageKeys.Admin.LeadStatusContacted,
        LeadStatus.Closed => MessageKeys.Admin.LeadStatusClosed,
        _ => MessageKeys.Admin.LeadStatusNew,
    };
}

/// <summary>CSV for Excel: every cell quoted, quotes doubled, formula-like values neutralised, UTF-8 with a BOM.</summary>
public static class LeadCsv
{
    /// <summary>
    /// A value a spreadsheet could evaluate gets a leading apostrophe, so Excel shows it as text (CSV / formula
    /// injection): it starts with a tab or a carriage return, or its first non-whitespace character is = + - @ |
    /// or one of their fullwidth forms (＝ ＋ － ＠), which some spreadsheet locales normalise.
    /// </summary>
    public static string Cell(string? value)
    {
        var v = value ?? "";
        if (IsFormulaLike(v)) v = "'" + v;
        return "\"" + v.Replace("\"", "\"\"") + "\"";
    }

    private static bool IsFormulaLike(string v)
    {
        if (v.Length == 0) return false;
        if (v[0] is '\t' or '\r') return true;
        var t = v.AsSpan().TrimStart();
        return t.Length > 0 && t[0] is '=' or '+' or '-' or '@' or '|' or '＝' or '＋' or '－' or '＠';
    }

    public static string Row(IEnumerable<string?> cells) => string.Join(",", cells.Select(Cell)) + "\r\n";

    /// <summary>The header row's resx keys, in column order.</summary>
    public static readonly IReadOnlyList<string> HeaderKeys =
    [
        MessageKeys.Admin.CsvType, MessageKeys.Admin.CsvId, MessageKeys.Admin.CsvDate, MessageKeys.Admin.CsvName,
        MessageKeys.Admin.CsvEmail, MessageKeys.Admin.CsvPhone, MessageKeys.Admin.CsvDetails, MessageKeys.Admin.CsvStatus,
        MessageKeys.Admin.CsvRead, MessageKeys.Admin.CsvCulture, MessageKeys.Admin.CsvNote,
        MessageKeys.Admin.CsvReference, MessageKeys.Admin.CsvReferenceId,
    ];

    /// <summary>The header row in the language of <paramref name="text"/> (a resx lookup for the current culture).</summary>
    public static string Header(Func<string, string> text) => Row(HeaderKeys.Select(text));

    /// <summary>One lead as a CSV row; words (status, yes/no, language) come from <paramref name="text"/>.</summary>
    public static string Row(LeadRow r, Func<string, string> text) => Row(
    [
        LeadInbox.Label(r.Type), r.Id.ToString(CultureInfo.InvariantCulture),
        r.CreatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), r.Name, r.Email, r.Phone, r.Summary,
        text(LeadStatuses.LabelKey(r.Status)), text(r.IsRead ? MessageKeys.Admin.CsvYes : MessageKeys.Admin.CsvNo),
        text(r.Culture == "en" ? MessageKeys.Admin.CsvEnglish : MessageKeys.Admin.CsvArabic), r.Note,
        r.Reference, r.ReferenceId?.ToString(CultureInfo.InvariantCulture),
    ]);

    /// <summary>
    /// Writes the BOM, the header and every row to <paramref name="body"/> as the rows arrive from the database (never
    /// the whole file in memory: C4 review F5).
    /// </summary>
    public static async Task WriteAsync(Stream body, IAsyncEnumerable<LeadRow> rows, Func<string, string> text, CancellationToken ct)
    {
        await body.WriteAsync(Encoding.UTF8.GetPreamble(), ct);
        await using var writer = new StreamWriter(body, new UTF8Encoding(false), 16 * 1024, leaveOpen: true);
        await writer.WriteAsync(Header(text));
        await foreach (var r in rows.WithCancellation(ct)) await writer.WriteAsync(Row(r, text));
        await writer.FlushAsync(ct);
    }
}
