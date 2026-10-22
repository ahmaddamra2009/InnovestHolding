using System.Text;
using System.Xml;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Seo;
using Microsoft.AspNetCore.Mvc;

namespace InnovestHolding.Controllers;

/// <summary>robots.txt and sitemap.xml, generated because both depend on the stored base URL.</summary>
public class SeoController(IHomeContentCache cache, IWebHostEnvironment environment) : Controller
{
    private async Task<string?> BaseUrlAsync()
    {
        var snapshot = await cache.GetAsync(HttpContext.RequestAborted);
        return SiteUrl.Resolve(snapshot.Content.GetValueOrDefault(SiteUrl.ContentKey)?.Ar, environment, Request);
    }

    [HttpGet("/robots.txt")]
    public async Task<IActionResult> Robots()
    {
        var baseUrl = await BaseUrlAsync();
        var text = new StringBuilder()
            .Append("User-agent: *\n")
            .Append("Disallow: /Admin\n")
            .Append("Disallow: /admin\n")
            .Append("Allow: /\n");
        if (baseUrl is not null) text.Append("Sitemap: ").Append(baseUrl).Append("/sitemap.xml\n");
        return Content(text.ToString(), "text/plain; charset=utf-8");
    }

    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Sitemap()
    {
        var baseUrl = await BaseUrlAsync();
        if (baseUrl is null) return NotFound();

        const string ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        const string xhtml = "http://www.w3.org/1999/xhtml";
        var alternates = new (string Lang, string Href)[]
        {
            ("ar", baseUrl + CultureUrls.ArabicHome), ("en", baseUrl + CultureUrls.EnglishHome), ("x-default", baseUrl + CultureUrls.ArabicHome)
        };

        var sb = new StringBuilder();
        using (var w = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 }))
        {
            w.WriteStartDocument();
            w.WriteStartElement("urlset", ns);
            w.WriteAttributeString("xmlns", "xhtml", null, xhtml);
            foreach (var path in CultureUrls.Indexable)
            {
                w.WriteStartElement("url", ns);
                w.WriteElementString("loc", ns, baseUrl + path);
                foreach (var (lang, href) in alternates)
                {
                    w.WriteStartElement("xhtml", "link", xhtml);
                    w.WriteAttributeString("rel", "alternate");
                    w.WriteAttributeString("hreflang", lang);
                    w.WriteAttributeString("href", href);
                    w.WriteEndElement();
                }
                w.WriteEndElement();
            }
            w.WriteEndElement();
            w.WriteEndDocument();
        }

        Response.Headers.CacheControl = "public, max-age=3600";
        // XmlWriter over a StringBuilder declares utf-16; the response is UTF-8.
        var xml = sb.ToString().Replace("encoding=\"utf-16\"", "encoding=\"utf-8\"");
        return Content(xml, "application/xml; charset=utf-8");
    }
}
