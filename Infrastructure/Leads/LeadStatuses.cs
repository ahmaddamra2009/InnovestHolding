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
    /// A value a spreadsheet would evaluate (it starts with = + - @, a tab or a carriage return) gets a leading apostrophe,
    /// so Excel shows it as text (CSV / formula injection).
    /// </summary>
    public static string Cell(string? value)
    {
        var v = value ?? "";
        if (v.Length > 0 && v[0] is '=' or '+' or '-' or '@' or '\t' or '\r') v = "'" + v;
        return "\"" + v.Replace("\"", "\"\"") + "\"";
    }

    public static void AppendRow(StringBuilder csv, IEnumerable<string?> cells) =>
        csv.Append(string.Join(",", cells.Select(Cell))).Append("\r\n");

    public static byte[] Utf8WithBom(string text) => [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(text)];
}
