using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace InnovestHolding.Infrastructure.Content;

/// <summary>
/// The forum pages' stylesheet, /forum-theme.css: the static rules of wwwroot/css/forum.css followed by the admin's
/// identity values (<see cref="ForumIdentity.GeneratedCss"/>). One output per site snapshot (computed once per snapshot,
/// which the content-cache interceptor replaces on every save), the same for every visitor and language; its version
/// is a hash of the output, so the page links a new URL as soon as a value changes and the file can be cached forever.
/// </summary>
public sealed class ForumThemeStylesheet(IWebHostEnvironment environment)
{
    public const string Path = "/forum-theme.css";
    private const string StaticFile = "css/forum.css";

    public sealed record Output(string Css, string Version)
    {
        public string Href => Path + "?v=" + Version;
    }

    private readonly ConditionalWeakTable<SiteSnapshot, Output> _bySnapshot = new();
    private Output? _default;
    private string? _static;

    private string Static()
    {
        if (_static is not null) return _static;
        var file = environment.WebRootFileProvider.GetFileInfo(StaticFile);
        string text;
        if (!file.Exists) text = "";
        else
        {
            using var stream = file.CreateReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            text = reader.ReadToEnd();
        }
        return _static = text;
    }

    /// <summary>The stylesheet for this snapshot; null = the static rules only (the snapshot could not be loaded).</summary>
    public Output For(SiteSnapshot? snapshot) =>
        snapshot is null
            ? _default ??= Build(null)
            : _bySnapshot.GetValue(snapshot, Build);

    private Output Build(SiteSnapshot? snapshot)
    {
        var generated = ForumIdentity.GeneratedCss(snapshot);
        var css = generated.Length == 0
            ? Static()
            : Static().TrimEnd() + "\n\n/* Admin › هوية المنتدى */\n" + generated;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(css));
        return new Output(css, Convert.ToHexStringLower(hash)[..16]);
    }
}
