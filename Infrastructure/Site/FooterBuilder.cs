using InnovestHolding.Infrastructure.Content;
using InnovestHolding.ViewModels;

namespace InnovestHolding.Infrastructure.Site;

/// <summary>
/// A footer column before it is resolved for a language: its title (a Content key, or a page's title) and its links
/// (page keys or extra-link ids). The automatic footer is a list of these; «تخصيص الأعمدة» copies the same list into
/// FooterColumns/FooterLinks, so a customised footer starts identical.
/// </summary>
/// <param name="TitleContentKey">The «روابط سريعة» column takes its title from this Content key.</param>
/// <param name="TitlePageKey">A unit column takes its title from this page (and its first link is that page).</param>
public sealed record FooterPlanColumn(string? TitleContentKey, string? TitlePageKey, IReadOnlyList<FooterLinkRow> Links);

/// <summary>What a social link shows (footer and contact block): a known network's brand name and glyph (code), or a
/// custom network's admin label and whitelisted icon. Null when a custom row lacks either (never an empty icon).</summary>
public static class SocialDisplay
{
    public static (string Name, string Svg)? Of(SocialVm social)
    {
        if (SocialIcons.All.TryGetValue(social.Network, out var known)) return known;
        var svg = Icons.Svg(social.IconKey);
        return string.IsNullOrWhiteSpace(social.Label) || svg is null ? null : (social.Label.Trim(), svg);
    }
}

/// <summary>
/// The footer's link columns: automatic (today's derivation, filtered by Pages.ShowInFooter and IsPublished) while
/// FooterColumns is empty, else the admin's columns; plus the privacy link of the bottom bar.
/// </summary>
public static class FooterBuilder
{
    public const int MaxColumns = 4;
    public const int MaxLinks = 8;
    public const string QuickLinksTitleKey = "footer_quicklinks_title";

    public static FooterVm Build(SiteViews v) =>
        new(v.Snapshot.Footer.Count == 0 ? Automatic(v) : Customised(v), Privacy(v));

    /// <summary>
    /// The automatic footer exactly as C1–C4 derived it from the header menu: the company column (the home page and the
    /// top-level pages, the ecosystem hub inserted at position 2, then every active extra link), then one column per
    /// unit (the unit, then its sub-pages). The page tree is filtered by ShowInFooter (initialised = ShowInMenu by the
    /// migration) and IsPublished instead of ShowInMenu. The position-2 rule is kept on purpose: on a database whose
    /// admin reordered the top-level pages it is what the footer shows today (NC-0); an explicit order is what the
    /// customised columns are for.
    /// </summary>
    public static IReadOnlyList<FooterPlanColumn> Plan(SiteSnapshot s)
    {
        var top = Children(s, PageKeys.Home);
        var company = new List<string> { PageKeys.Home };
        company.AddRange(top.Where(k => Children(s, k).Count == 0));
        var hub = top.FirstOrDefault(k => Children(s, k).Count > 0);
        if (hub is not null) company.Insert(Math.Min(2, company.Count), hub);

        var quick = company.Select(k => new FooterLinkRow(k, null))
            .Concat(s.Nav.Select(n => new FooterLinkRow(null, n.Id))).ToList();
        var columns = new List<FooterPlanColumn> { new(QuickLinksTitleKey, null, quick) };
        if (hub is not null)
        {
            columns.AddRange(Children(s, hub).Select(unit => new FooterPlanColumn(null, unit,
                new[] { unit }.Concat(Children(s, unit)).Select(k => new FooterLinkRow(k, null)).ToList())));
        }
        return columns;
    }

    /// <summary>In the automatic footer: the page and every ancestor are published and shown in the footer (home always is).</summary>
    public static bool InFooter(SiteSnapshot s, string key) =>
        s.IsPublished(key) && SiteRoutes.Chain(key).All(p => p.Key == PageKeys.Home || (s.Page(p.Key)?.ShowInFooter ?? true));

    /// <summary>The footer children of a page, in the menus' order (SortOrder, then route order).</summary>
    private static List<string> Children(SiteSnapshot s, string key) =>
        SiteRoutes.Children(key).Where(p => InFooter(s, p.Key)).OrderBy(p => s.Page(p.Key)?.SortOrder ?? 0).Select(p => p.Key).ToList();

    private static IReadOnlyList<FooterColumnVm> Automatic(SiteViews v) =>
        Plan(v.Snapshot).Select(c => new FooterColumnVm(
                c.TitlePageKey is not null ? v.Title(c.TitlePageKey) : v.Content.Text(c.TitleContentKey!),
                c.Links.Select(l => Resolve(v, l)).OfType<LinkVm>().ToList()))
            .ToList();

    /// <summary>The admin's columns. A link to a page that is not published, or to an extra link that is deleted or
    /// inactive, is skipped; a column left without a link is skipped.</summary>
    private static IReadOnlyList<FooterColumnVm> Customised(SiteViews v) =>
        v.Snapshot.Footer
            .Select(c => new FooterColumnVm(v.P(c.TitleAr, c.TitleEn), c.Links.Select(l => Resolve(v, l)).OfType<LinkVm>().ToList()))
            .Where(c => c.Links.Count > 0)
            .ToList();

    private static LinkVm? Resolve(SiteViews v, FooterLinkRow link)
    {
        if (link.PageKey is { } key)
            return SiteRoutes.IsKey(key) && v.Snapshot.IsPublished(key) ? new LinkVm(v.Url(key), v.Title(key)) : null;
        // The snapshot's Nav holds the ACTIVE extra links only (ShowInHeader is the header's switch, not the footer's).
        return v.Snapshot.Nav.FirstOrDefault(n => n.Id == link.NavItemId) is { } nav
            ? new LinkVm(nav.Href, v.P(nav.TitleAr, nav.TitleEn))
            : null;
    }

    /// <summary>
    /// A column whose first link carries the column's own title (a unit column: «المنتدى» over the Forum's sub-pages)
    /// shows that link as its heading, as the footer always did; the link is then not repeated in the list.
    /// </summary>
    public static LinkVm? HeadingLink(FooterColumnVm column) =>
        column.Links.Count > 0 && column.Links[0].Title.Value == column.Title.Value ? column.Links[0] : null;

    /// <summary>The privacy policy link: only while the policy has an Arabic body and the page is published (Rami Q6).</summary>
    public static LinkVm? Privacy(SiteViews v) => v.PrivacyLink();
}
