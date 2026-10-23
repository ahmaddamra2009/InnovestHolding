namespace InnovestHolding.Infrastructure.Content;

/// <summary>
/// The icon whitelist of ContentItems.IconKey: the admin picks a key, the SVG body comes from here (code, not data),
/// so no admin value is ever rendered as markup. Stroke icons on a 24×24 grid.
/// </summary>
public static class Icons
{
    public static readonly IReadOnlyDictionary<string, (string Label, string Svg)> All = new Dictionary<string, (string, string)>(StringComparer.Ordinal)
    {
        ["idea"] = ("فكرة", "<path d=\"M9 18h6\"/><path d=\"M10 22h4\"/><path d=\"M12 2a7 7 0 0 0-4 12.7V17h8v-2.3A7 7 0 0 0 12 2z\"/>"),
        ["handshake"] = ("شراكة", "<path d=\"M11 17l2 2a1 1 0 0 0 1.4 0l4-4a1 1 0 0 0 0-1.4L13 8\"/><path d=\"M14 7 9.5 2.5 2 10l5 5\"/><path d=\"m22 10-5-5-3 3\"/><path d=\"m8 14 2 2\"/>"),
        ["eye"] = ("شفافية", "<path d=\"M1 12s4-8 11-8 11 8 11 8-4 8-11 8S1 12 1 12z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/>"),
        ["leaf"] = ("استدامة", "<path d=\"M11 20A7 7 0 0 1 9.8 6.1C15.5 5 17 4.5 19 2c1 2 2 4.2 2 8 0 5.5-4.8 10-10 10z\"/><path d=\"M2 21c0-3 1.9-5.4 5.1-6\"/>"),
        ["briefcase"] = ("احترافية", "<rect x=\"2\" y=\"7\" width=\"20\" height=\"14\" rx=\"2\"/><path d=\"M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16\"/>"),
        ["target"] = ("تأثير", "<circle cx=\"12\" cy=\"12\" r=\"10\"/><circle cx=\"12\" cy=\"12\" r=\"6\"/><circle cx=\"12\" cy=\"12\" r=\"2\"/>"),
        ["rocket"] = ("انطلاق", "<path d=\"M4.5 16.5c-1.5 1.3-2 5-2 5s3.7-.5 5-2c.7-.8.7-2.1-.1-2.9a2.2 2.2 0 0 0-2.9-.1z\"/><path d=\"m12 15-3-3a22 22 0 0 1 2-4A12.9 12.9 0 0 1 22 2c0 2.7-.8 7.5-6 11a22.4 22.4 0 0 1-4 2z\"/><path d=\"M9 12H4s.6-3 2-4c1.6-1.1 5 0 5 0\"/><path d=\"M12 15v5s3-.6 4-2c1.1-1.6 0-5 0-5\"/>"),
        ["chart"] = ("نمو", "<polyline points=\"22 7 13.5 15.5 8.5 10.5 2 17\"/><polyline points=\"16 7 22 7 22 13\"/>"),
        ["users"] = ("مجتمع", "<path d=\"M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M23 21v-2a4 4 0 0 0-3-3.87\"/><path d=\"M16 3.13a4 4 0 0 1 0 7.75\"/>"),
        ["building"] = ("مؤسسة", "<path d=\"M3 21h18\"/><path d=\"M5 21V7l7-4 7 4v14\"/><path d=\"M9 21v-6h6v6\"/><path d=\"M9 10h.01M15 10h.01\"/>"),
        ["graduation"] = ("جامعة", "<path d=\"M22 10 12 5 2 10l10 5 10-5z\"/><path d=\"M6 12v5c3 3 9 3 12 0v-5\"/>"),
        ["globe"] = ("عالمي", "<circle cx=\"12\" cy=\"12\" r=\"10\"/><line x1=\"2\" y1=\"12\" x2=\"22\" y2=\"12\"/><path d=\"M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z\"/>"),
        ["calendar"] = ("فعالية", "<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\"/><line x1=\"16\" y1=\"2\" x2=\"16\" y2=\"6\"/><line x1=\"8\" y1=\"2\" x2=\"8\" y2=\"6\"/><line x1=\"3\" y1=\"10\" x2=\"21\" y2=\"10\"/>"),
        ["mic"] = ("منصة حديث", "<rect x=\"9\" y=\"2\" width=\"6\" height=\"12\" rx=\"3\"/><path d=\"M19 10v1a7 7 0 0 1-14 0v-1\"/><line x1=\"12\" y1=\"18\" x2=\"12\" y2=\"22\"/>"),
        ["coins"] = ("تمويل", "<circle cx=\"8\" cy=\"8\" r=\"6\"/><path d=\"M18.1 10.4A6 6 0 1 1 10.3 18\"/><path d=\"M7 6h1v4\"/>"),
        ["cpu"] = ("تقنية", "<rect x=\"4\" y=\"4\" width=\"16\" height=\"16\" rx=\"2\"/><rect x=\"9\" y=\"9\" width=\"6\" height=\"6\"/><path d=\"M9 1v3M15 1v3M9 20v3M15 20v3M20 9h3M20 14h3M1 9h3M1 14h3\"/>"),
        ["link"] = ("ربط", "<path d=\"M10 13a5 5 0 0 0 7.5.5l3-3a5 5 0 0 0-7-7l-1.7 1.7\"/><path d=\"M14 11a5 5 0 0 0-7.5-.5l-3 3a5 5 0 0 0 7 7l1.7-1.7\"/>"),
        ["award"] = ("جائزة", "<circle cx=\"12\" cy=\"8\" r=\"6\"/><path d=\"M15.5 13 17 22l-5-3-5 3 1.5-9\"/>"),
        ["search"] = ("فحص", "<circle cx=\"11\" cy=\"11\" r=\"8\"/><line x1=\"21\" y1=\"21\" x2=\"16.65\" y2=\"16.65\"/>"),
        ["shield"] = ("ثقة", "<path d=\"M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z\"/>"),
        ["book"] = ("تعلم", "<path d=\"M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z\"/><path d=\"M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z\"/>"),
        ["layers"] = ("منظومة", "<polygon points=\"12 2 2 7 12 12 22 7 12 2\"/><polyline points=\"2 17 12 22 22 17\"/><polyline points=\"2 12 12 17 22 12\"/>"),
        ["tv"] = ("بث", "<rect x=\"2\" y=\"7\" width=\"20\" height=\"15\" rx=\"2\"/><polyline points=\"17 2 12 7 7 2\"/>"),
    };

    /// <summary>The SVG body of a known key, else null (the view then draws the brand triangle bullet).</summary>
    public static string? Svg(string? key) => key is not null && All.TryGetValue(key, out var icon) ? icon.Svg : null;

    public static bool IsKnown(string? key) => key is not null && All.ContainsKey(key);
}
