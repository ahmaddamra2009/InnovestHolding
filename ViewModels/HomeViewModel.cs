using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Site;

namespace InnovestHolding.ViewModels;

/// <summary>The home page: the shell plus teasers of the other pages. Section ids of the one-page site are kept.</summary>
public sealed class HomeViewModel
{
    public required bool IsEnglish { get; init; }
    public string Lang => IsEnglish ? Cultures.EnglishName : Cultures.ArabicName;
    public required ContentVm Content { get; init; }
    public required ShellVm Shell { get; init; }
    public required SeoVm Seo { get; init; }

    public required IReadOnlyList<CardVm> Units { get; init; }
    public required IReadOnlyList<ServiceVm> Services { get; init; }
    public required IReadOnlyList<StatVm> Stats { get; init; }
    public required IReadOnlyList<NewsCardVm> News { get; init; }
    public required IReadOnlyList<EventCardVm> Events { get; init; }
    public required IReadOnlyList<LText> Partners { get; init; }
    public required IReadOnlyList<FaqVm> Faqs { get; init; }
    public required RegisterFormVm Register { get; init; }
    public required ContactFormVm Contact { get; init; }
    public required CtaBlock CtaBand { get; init; }

    /// <summary>Links of the teasers to their pages (from the route table).</summary>
    public required IReadOnlyDictionary<string, LinkVm> Pages { get; init; }

    /// <summary>Section ids rendered on the page (a list section with no items is not rendered).</summary>
    public required IReadOnlySet<string> VisibleSections { get; init; }

    /// <summary>Reveal delay class: the literal page's pattern, clamped to the 6 classes the CSS defines.</summary>
    public static string Delay(int n) => n <= 0 ? "" : $" reveal-delay-{Math.Min(n, 6)}";

    public static HomeViewModel Create(SiteViews v, ShellVm shell, SeoVm seo)
    {
        var services = v.Services();
        var stats = v.Stats();
        var news = v.NewsCards(v.Snapshot.News);
        var events = v.EventCards(v.Snapshot.Events.Take(3));
        var partners = v.Partners();
        var faqs = v.Faqs();
        var units = v.Cards(PageKeys.Ecosystem, true);

        var visible = new HashSet<string>(StringComparer.Ordinal) { "hero", "about", "register", "contact" };
        if (services.Count > 0) visible.Add("platforms");
        if (stats.Count > 0) visible.Add("stats");
        if (news.Count > 0) visible.Add("news");
        if (events.Count > 0) visible.Add("events");
        if (partners.Count > 0) visible.Add("partners");
        if (faqs.Count > 0) visible.Add("faq");
        if (units.Count > 0) visible.Add("ecosystem");

        string[] linked = [PageKeys.About, PageKeys.Ecosystem, PageKeys.News, PageKeys.ForumEvents, PageKeys.ForumPartners,
            PageKeys.Contact, PageKeys.ForumRegistration, PageKeys.NetworkJoin];

        return new HomeViewModel
        {
            IsEnglish = v.English,
            Content = v.Content,
            Shell = shell,
            Seo = seo,
            Units = units,
            Services = services,
            Stats = stats,
            News = news,
            Events = events,
            Partners = partners,
            Faqs = faqs,
            Register = v.RegisterForm("reg"),
            Contact = v.ContactForm("contact"),
            CtaBand = v.CtaBand(),
            Pages = linked.ToDictionary(k => k, k => new LinkVm(v.Url(k), v.Title(k))),
            VisibleSections = visible,
        };
    }
}
