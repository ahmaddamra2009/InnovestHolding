using System.Text;
using System.Xml;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Seo;
using InnovestHolding.Infrastructure.Site;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers;

/// <summary>robots.txt and sitemap.xml, generated because both depend on the stored base URL and on the content.</summary>
public class SeoController(ISiteContentCache cache, IWebHostEnvironment environment, Data.AppDbContext db) : Controller
{
    [HttpGet("/robots.txt")]
    public async Task<IActionResult> Robots()
    {
        var snapshot = await cache.GetAsync(HttpContext.RequestAborted);
        var baseUrl = SiteUrl.Resolve(snapshot.Content.GetValueOrDefault(SiteUrl.ContentKey)?.Ar, environment, Request);
        var text = new StringBuilder()
            .Append("User-agent: *\n")
            .Append("Disallow: /Admin\n")
            .Append("Disallow: /admin\n")
            .Append("Disallow: /forms\n")
            .Append("Disallow: /en/forms\n")
            .Append("Allow: /\n");
        if (baseUrl is not null) text.Append("Sitemap: ").Append(baseUrl).Append("/sitemap.xml\n");
        return Content(text.ToString(), "text/plain; charset=utf-8");
    }

    /// <summary>
    /// Every page URL whose robots decision is Index (<see cref="SeoPolicy.Decide"/> — the same function the pages use),
    /// each with its hreflang pair only when both languages are indexable; then every published news item and event
    /// (<see cref="SeoPolicy.DecideDetail"/>): the Arabic URL always, the English URL and the pair only when the item
    /// has its English text.
    /// </summary>
    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Sitemap()
    {
        var snapshot = await cache.GetAsync(HttpContext.RequestAborted);
        var baseUrl = SiteUrl.Resolve(snapshot.Content.GetValueOrDefault(SiteUrl.ContentKey)?.Ar, environment, Request);
        if (baseUrl is null) return NotFound();

        // Built once per snapshot: every content save drops the snapshot (ContentCacheInvalidationInterceptor), so the
        // next request builds a new one and with it a new sitemap; between saves the sitemap costs no query.
        var built = Built.GetOrCreateValue(snapshot);
        if (!built.TryGetValue(baseUrl, out var xml))
        {
            xml = await BuildAsync(snapshot, baseUrl, HttpContext.RequestAborted);
            built[baseUrl] = xml;
        }
        Response.Headers.CacheControl = "public, max-age=3600";
        return Content(xml, "application/xml; charset=utf-8");
    }

    /// <summary>The sitemap of a snapshot, per base URL; collected with the snapshot once it leaves the cache.</summary>
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<SiteSnapshot,
        System.Collections.Concurrent.ConcurrentDictionary<string, string>> Built = new();

    private async Task<string> BuildAsync(SiteSnapshot snapshot, string baseUrl, CancellationToken ct)
    {
        var news = (await db.News.AsNoTracking().Where(n => n.IsActive).OrderBy(n => n.SortOrder).ThenBy(n => n.Id)
                .Select(n => new { n.Slug, n.UpdatedAt, n.TitleEn, n.SummaryEn }).ToListAsync(ct))
            .Select(n => (Key: DetailKeys.NewsItem, n.Slug, n.UpdatedAt, En: EventQueries.NewsHasEnglish(n.TitleEn, n.SummaryEn)));
        // F10: an event without an Arabic summary or description is thin (noindex,follow: EventQueries.HasText) and left
        // out — the page's own rule, applied in memory (the same function, so the two cannot disagree).
        var events = (await db.Events.AsNoTracking().Where(e => e.IsActive).OrderBy(e => e.SortOrder).ThenBy(e => e.Id)
                .Select(e => new { e.Slug, e.UpdatedAt, e.TitleEn, e.SummaryEn, e.DescriptionEn, e.SummaryAr, e.DescriptionAr }).ToListAsync(ct))
            .Where(e => EventQueries.HasText(e.SummaryAr, e.DescriptionAr))
            .Select(e => (Key: DetailKeys.ForumEvent, e.Slug, e.UpdatedAt, En: EventQueries.HasEnglish(e.TitleEn, e.SummaryEn, e.DescriptionEn)));
        // Profiles: only those with a description (EcosystemQueries.PersonHasDescription, the rule of the page itself).
        var speakers = (await db.Speakers.AsNoTracking().Where(p => p.IsActive && (p.BioAr != "" || p.MetaDescriptionAr != ""))
                .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
                .Select(p => new { p.Slug, p.UpdatedAt, p.NameEn, p.JobTitleAr, p.JobTitleEn, p.BioAr, p.BioEn, p.MetaDescriptionEn }).ToListAsync(ct))
            .Select(p => (Key: DetailKeys.ForumSpeaker, p.Slug, p.UpdatedAt,
                En: EcosystemQueries.PersonHasEnglish(p.NameEn, p.JobTitleAr, p.JobTitleEn, p.BioAr, p.BioEn)
                    && EcosystemQueries.PersonHasDescription(p.BioEn, p.MetaDescriptionEn)));
        var guests = (await db.TvGuests.AsNoTracking().Where(p => p.IsActive && (p.BioAr != "" || p.MetaDescriptionAr != ""))
                .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
                .Select(p => new { p.Slug, p.UpdatedAt, p.NameEn, p.JobTitleAr, p.JobTitleEn, p.BioAr, p.BioEn, p.MetaDescriptionEn }).ToListAsync(ct))
            .Select(p => (Key: DetailKeys.TvGuest, p.Slug, p.UpdatedAt,
                En: EcosystemQueries.PersonHasEnglish(p.NameEn, p.JobTitleAr, p.JobTitleEn, p.BioAr, p.BioEn)
                    && EcosystemQueries.PersonHasDescription(p.BioEn, p.MetaDescriptionEn)));
        var episodes = (await db.TvEpisodes.AsNoTracking().Where(e => e.IsActive).OrderBy(e => e.SortOrder).ThenBy(e => e.Id)
                .Select(e => new { e.Slug, e.UpdatedAt, e.TitleEn, e.SummaryEn }).ToListAsync(ct))
            .Select(e => (Key: DetailKeys.TvEpisode, e.Slug, e.UpdatedAt, En: EcosystemQueries.EpisodeHasEnglish(e.TitleEn, e.SummaryEn)));
        var opportunities = (await db.InvestmentOpportunities.AsNoTracking().Where(o => o.IsActive).OrderBy(o => o.SortOrder).ThenBy(o => o.Id)
                .Select(o => new { o.Slug, o.UpdatedAt, o.TitleEn, o.SummaryEn }).ToListAsync(ct))
            .Select(o => (Key: DetailKeys.NetworkOpportunity, o.Slug, o.UpdatedAt, En: EcosystemQueries.OpportunityHasEnglish(o.TitleEn, o.SummaryEn)));
        var details = news.Concat(events).Concat(speakers).Concat(guests).Concat(episodes).Concat(opportunities).ToList();

        const string ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        const string xhtml = "http://www.w3.org/1999/xhtml";

        var sb = new StringBuilder();
        using (var w = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 }))
        {
            w.WriteStartDocument();
            w.WriteStartElement("urlset", ns);
            w.WriteAttributeString("xmlns", "xhtml", null, xhtml);
            foreach (var english in new[] { false, true })
            {
                foreach (var page in SiteRoutes.All)
                {
                    if (SeoPolicy.Decide(page, english, snapshot) != RobotsDecision.Index) continue;
                    w.WriteStartElement("url", ns);
                    w.WriteElementString("loc", ns, baseUrl + CultureUrls.Page(page.Key, english));
                    // A list page's date would be its items'; only text pages carry their own date.
                    if (page.Kind != PageKind.List && snapshot.Page(page.Key) is { } row)
                        w.WriteElementString("lastmod", ns, row.UpdatedAt.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
                    if (SeoPolicy.HasAlternates(page, snapshot))
                    {
                        foreach (var (lang, href) in new[]
                                 {
                                     ("ar", CultureUrls.Page(page.Key, false)), ("en", CultureUrls.Page(page.Key, true)),
                                     ("x-default", CultureUrls.Page(page.Key, false)),
                                 })
                        {
                            w.WriteStartElement("xhtml", "link", xhtml);
                            w.WriteAttributeString("rel", "alternate");
                            w.WriteAttributeString("hreflang", lang);
                            w.WriteAttributeString("href", baseUrl + href);
                            w.WriteEndElement();
                        }
                    }
                    w.WriteEndElement();
                }
                foreach (var d in details)
                {
                    // The detail pages of an unpublished list page are 404 too.
                    if (!snapshot.IsPublished(SiteRoutes.Detail(d.Key).Parent)) continue;
                    if (SeoPolicy.DecideDetail(english, d.En) != RobotsDecision.Index) continue;
                    w.WriteStartElement("url", ns);
                    w.WriteElementString("loc", ns, baseUrl + CultureUrls.Detail(d.Key, d.Slug, english));
                    w.WriteElementString("lastmod", ns, d.UpdatedAt.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
                    if (d.En)
                    {
                        foreach (var (lang, href) in new[]
                                 {
                                     ("ar", CultureUrls.Detail(d.Key, d.Slug, false)), ("en", CultureUrls.Detail(d.Key, d.Slug, true)),
                                     ("x-default", CultureUrls.Detail(d.Key, d.Slug, false)),
                                 })
                        {
                            w.WriteStartElement("xhtml", "link", xhtml);
                            w.WriteAttributeString("rel", "alternate");
                            w.WriteAttributeString("hreflang", lang);
                            w.WriteAttributeString("href", baseUrl + href);
                            w.WriteEndElement();
                        }
                    }
                    w.WriteEndElement();
                }
            }
            w.WriteEndElement();
            w.WriteEndDocument();
        }

        // XmlWriter over a StringBuilder declares utf-16; the response is UTF-8.
        return sb.ToString().Replace("encoding=\"utf-16\"", "encoding=\"utf-8\"");
    }
}
