using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Seo;
using InnovestHolding.ViewModels;

namespace InnovestHolding.Infrastructure.Site;

/// <summary>
/// The links of the page bodies an admin chooses (ARCHITECTURE-C5 §3.2/§3.6): a page of the route table is linked
/// only while it is published (else no link at all — never a link to a 404); a stored link that is neither a page key
/// nor a safe https URL falls back to the code default. Labels are resolved by the caller (each pair keeps its own
/// fallback rule) and an empty label becomes the target page's title in the page language.
/// C5 integration: Dev 3's SiteViews.Link (header, CTA band) applies the same rules — when both exist, one delegates
/// to the other.
/// </summary>
public static class SiteLinks
{
    /// <summary>The published page as a link (its title), or null (not a page key, or unpublished).</summary>
    public static LinkVm? Page(SiteViews v, string? key) =>
        key is not null && SiteRoutes.IsKey(key) && v.Snapshot.IsPublished(key) ? new LinkVm(v.Url(key), v.Title(key)) : null;

    /// <summary>An absolute https URL that also passes the href rule (no control characters, no backslash).</summary>
    public static bool IsExternal(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        && SiteUrl.IsHttpsUrl(value) && SafeHrefAttribute.IsSafe(value);

    /// <summary>
    /// A link pair: the stored value of <paramref name="linkKey"/> (a page key or an https URL) or
    /// <paramref name="defaultKey"/>; labelled <paramref name="label"/> or, when that is empty, the target page's title.
    /// An external link without a label, or a link to an unpublished page, is null (not rendered).
    /// </summary>
    public static LinkVm? Resolve(SiteViews v, string linkKey, string? defaultKey, LText label)
    {
        var stored = v.Content.Neutral(linkKey).Trim();
        if (IsExternal(stored)) return label.IsEmpty ? null : new LinkVm(stored, label);
        var key = SiteRoutes.IsKey(stored) ? stored : defaultKey;
        if (Page(v, key) is not { } page) return null;
        return label.IsEmpty ? page : page with { Title = label };
    }

    /// <summary>The event action (rows, event and speaker pages, empty events lists); default Forum registration.</summary>
    public static LinkVm? EventsAction(SiteViews v) =>
        Resolve(v, "events_action_link", PageKeys.ForumRegistration, v.Content.Optional("events_action_label"));

    /// <summary>The TV action (empty TV lists, episode and guest pages); default TV sponsorship.</summary>
    public static LinkVm? TvAction(SiteViews v) =>
        Resolve(v, "tv_action_link", PageKeys.TvSponsorship, v.Content.Optional("tv_action_label"));

    /// <summary>
    /// The button under a page's "coming soon" text: the admin's Pages.EmptyActionPage when it names a page (null while
    /// that page is unpublished), else <paramref name="codeDefault"/>.
    /// </summary>
    public static LinkVm? EmptyAction(SiteViews v, string pageKey, LinkVm? codeDefault)
    {
        var chosen = v.Snapshot.Page(pageKey)?.EmptyActionPage;
        return !string.IsNullOrWhiteSpace(chosen) && SiteRoutes.IsKey(chosen.Trim()) ? Page(v, chosen.Trim()) : codeDefault;
    }

    /// <summary>The 404 page's links when the admin left none (or none is usable).</summary>
    public static readonly IReadOnlyList<string> NotFoundDefault = [PageKeys.Home, PageKeys.Ecosystem, PageKeys.Contact];

    /// <summary>At most this many 404 links.</summary>
    public const int NotFoundMax = 6;

    /// <summary>
    /// The 404 page's links: the page keys of notfound_pages (comma separated, in order), each once, published only,
    /// at most <see cref="NotFoundMax"/>; nothing usable → <see cref="NotFoundDefault"/> (published ones).
    /// </summary>
    public static IReadOnlyList<LinkVm> NotFound(SiteViews v)
    {
        var links = v.Content.Neutral("notfound_pages").Split([',', '،'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal).Select(k => Page(v, k)).OfType<LinkVm>().Take(NotFoundMax).ToList();
        return links.Count > 0 ? links : NotFoundDefault.Select(k => Page(v, k)).OfType<LinkVm>().ToList();
    }
}
