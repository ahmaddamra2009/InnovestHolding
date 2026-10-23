using InnovestHolding.Infrastructure.Localization;

namespace InnovestHolding.Infrastructure.Content;

/// <summary>How a key's text is turned into markup on the public page.</summary>
public enum ContentFormat
{
    /// <summary>Plain text, HTML-encoded.</summary>
    Plain,
    /// <summary>One optional highlighted part between { and } → wrapped in the gold underline span.</summary>
    Highlight,
    /// <summary>Line breaks in the value become &lt;br&gt; (each line encoded).</summary>
    Lines,
    /// <summary>"{year}" is replaced by the current UTC year.</summary>
    Year,
}

/// <summary>Extra validation an admin value must pass.</summary>
public enum ContentRule
{
    None,
    /// <summary>Absolute https:// URL, or empty.</summary>
    HttpsUrl,
    /// <summary>The site base URL: https://host[:port] only (Infrastructure/Seo/SiteUrl).</summary>
    BaseUrl,
    /// <summary>A site-relative path starting with "/" (not "//"), or empty.</summary>
    SitePath,
    /// <summary>A whole number from 1 to <see cref="ContentKeys.MaxCount"/> (ASCII digits).</summary>
    Count,
    /// <summary>A key of the route table (Infrastructure/Site/SiteRoutes), e.g. network.join.</summary>
    PageKey,
    /// <summary>An international phone number as digits only (8–15), e.g. 201141410808 — used in https://wa.me/….</summary>
    Digits,
    /// <summary>A UTC offset written ±hh:mm (−14:00 … +14:00), e.g. +02:00.</summary>
    UtcOffset,
    /// <summary>
    /// Where a button goes (C5): a key of the route table (rendered as that page's URL in the page language), or an
    /// absolute https:// URL that also passes SafeHref (rendered with rel="noopener noreferrer"; needs a label).
    /// </summary>
    Link,
    /// <summary>A key of the icon whitelist (Infrastructure/Content/Icons) — never markup.</summary>
    IconKey,
    /// <summary>Comma-separated route-table keys, 1–<see cref="ContentKeys.MaxPageList"/>, no duplicates.</summary>
    PageKeyList,
    /// <summary>A phone as typed: optional leading «+», digits, spaces and dashes, 7–15 digits (<see cref="ContactFormats.IsPhone"/>).</summary>
    Phone,
    /// <summary>An ISO 3166-1 alpha-2 country code, stored upper-case (<see cref="ContactFormats.CountryCode"/>).</summary>
    CountryCode,
}

/// <param name="Key">SiteContents.SectionKey.</param>
/// <param name="Group">Admin card (one of <see cref="ContentKeys.Groups"/>).</param>
/// <param name="Label">Arabic admin label.</param>
/// <param name="Neutral">One value for both languages (stored in ContentAr, ContentEn stays NULL).</param>
/// <param name="Optional">No fallback: the English page shows nothing when English is empty.</param>
/// <param name="AllowEmpty">The Arabic value may be empty (otherwise it is required).</param>
/// <param name="CountMax">Upper bound of a <see cref="ContentRule.Count"/> value.</param>
/// <param name="Default">The value used when the stored one is missing or invalid (a page key, a count, an icon key).</param>
/// <param name="LabelKey">A <see cref="ContentRule.Link"/> key's text key (an external link needs its Arabic text).</param>
public sealed record ContentKey(
    string Key, string Group, string Label, int MaxLength = ContentKeys.DefaultMax,
    ContentFormat Format = ContentFormat.Plain, ContentRule Rule = ContentRule.None,
    bool Neutral = false, bool Optional = false, bool AllowEmpty = false, bool Multiline = false, string? HintKey = null,
    int CountMax = ContentKeys.MaxCount, string? Default = null, string? LabelKey = null);

/// <summary>
/// Registry of every SiteContents key the site reads. The admin editor is generated from it (only these keys can be
/// saved — no over-posting); the migrations AddBilingualContent and ApplyCompanyProfile insert every one of them.
/// Rows that are no longer read (seo_title, seo_description — the home page's SEO is in Pages since the multi-page
/// site; footer_platforms_title; about_years and about_years_label — the old "years of experience" badge, which no page
/// shows since the company-profile redesign) stay in the database untouched but are not listed here.
/// </summary>
public static class ContentKeys
{
    public const int DefaultMax = 4000;

    /// <summary>Upper bound of a <see cref="ContentRule.Count"/> value (keeps the home page bounded).</summary>
    public const int MaxCount = 50;

    /// <summary>How many news cards the home page shows, newest first (admin-editable; the migration writes 6).</summary>
    public const string HomeNewsLimit = "home_news_limit";

    /// <summary>Used only when the stored limit is missing or not a valid count.</summary>
    public const int HomeNewsLimitFallback = 6;

    /// <summary>The stored value when it is a valid count (1…<paramref name="max"/>), else <paramref name="fallback"/>.</summary>
    public static int ParseCount(string? value, int fallback, int max = MaxCount) =>
        int.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var n)
        && n >= 1 && n <= max ? n : fallback;

    // ── C5: counts, links and icons of the sections (AddAdminControl inserts every key with today's value) ──

    /// <summary>How many figures the home hero shows (default 4), events on the home page and on the Forum page (3).</summary>
    public const string HeroStatsLimit = "hero_stats_limit";
    public const string HomeEventsLimit = "home_events_limit";
    public const string ForumEventsLimit = "forum_events_limit";

    /// <summary>Upper bound of the section counts above (Dana US-03).</summary>
    public const int SectionCountMax = 12;

    /// <summary>The pages the 404 page suggests (rule <see cref="ContentRule.PageKeyList"/>).</summary>
    public const string NotFoundPages = "notfound_pages";
    public const int MaxPageList = 6;

    /// <summary>The value of the link picker's «رابط خارجي…» option: the URL comes from its own text box.</summary>
    public const string ExternalLinkChoice = "external";

    /// <summary>A registry count (its own default and maximum) from the stored content.</summary>
    public static int Count(IReadOnlyDictionary<string, TextPair> content, string key)
    {
        var k = Find(key) ?? throw new ArgumentException($"Unknown content key {key}.", nameof(key));
        var fallback = int.Parse(k.Default!, System.Globalization.CultureInfo.InvariantCulture);
        return ParseCount(content.GetValueOrDefault(key)?.Ar?.Trim(), fallback, k.CountMax);
    }

    /// <summary>An external button target: an absolute https URL with a host, no whitespace, accepted by SafeHref.</summary>
    public static bool IsExternalLink(string? value) =>
        Seo.SiteUrl.IsHttpsUrl(value) && !value!.Trim().Any(char.IsWhiteSpace) && SafeHrefAttribute.IsSafe(value);

    /// <summary>A valid <see cref="ContentRule.Link"/> value: a route-table key or an external https link.</summary>
    public static bool IsLink(string? value) =>
        !string.IsNullOrWhiteSpace(value) && (Site.SiteRoutes.IsKey(value.Trim()) || IsExternalLink(value));

    /// <summary>The keys of a <see cref="ContentRule.PageKeyList"/> value, in order (whitespace trimmed, empty parts dropped).</summary>
    public static IReadOnlyList<string> PageList(string? value) =>
        (value ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Items per page of the public list pages (news, events…).</summary>
    public const string ListPageSize = "list_page_size";

    public const int ListPageSizeFallback = 12;

    /// <summary>The UTC offset a new event's date form starts with (events keep their own offset).</summary>
    public const string DefaultUtcOffset = "default_utc_offset";

    public const string DefaultUtcOffsetFallback = "+02:00";

    /// <summary>The founder block's photo: the FileKey of an uploaded image, written only by the Content editor's
    /// photo form (not a text field of the registry).</summary>
    public const string FounderPhoto = "founder_photo";

    /// <summary>The consent sentence next to the required checkbox of every lead form.</summary>
    public const string FormsConsent = "forms_consent";

    /// <summary>Internal marker of the one-time timestamp conversion — never shown in the admin.</summary>
    public const string TimestampsUtcMarker = "sys_timestamps_utc";

    /// <summary>Internal: a random id of THIS database (created by the first orphan sweep), written into the uploads
    /// folder's marker so the sweep can tell its own folder from another database's. Never shown in the admin.</summary>
    public const string MediaInstance = "sys_media_instance";

    public static readonly IReadOnlyList<(string Id, string Title)> Groups =
    [
        ("header", "الهيدر والأزرار"),
        ("brand", "الهوية"),
        ("hero", "قسم Hero - الصفحة الرئيسية"),
        ("about", "قسم من نحن"),
        ("links", "الأزرار والروابط"),
        ("sections", "عناوين أقسام الصفحة الرئيسية"),
        ("icons", "الأيقونات"),
        ("pages", "عناوين أقسام الصفحات"),
        ("founder", "كلمة المؤسس (تُخفى ما دام نص الكلمة فارغاً)"),
        ("register", "قسم التسجيل"),
        ("forms", "نماذج الطلبات (الانضمام، الرعاية، الاهتمام بالفرص)"),
        ("contact", "معلومات التواصل"),
        ("footer", "الفوتر"),
        ("seo", "SEO وبيانات الموقع"),
    ];

    public static readonly IReadOnlyList<ContentKey> All =
    [
        new("cta_register", "header", "نص زر «سجّل الآن»", 100),
        new("header_cta_label", "header", "نص زر الهيدر", 100),
        new("header_cta_page", "header", "الصفحة التي يفتحها زر الهيدر", 500, Rule: ContentRule.Link, Neutral: true,
            Default: Site.PageKeys.NetworkJoin, LabelKey: "header_cta_label"),

        new("brand_positioning", "brand", "الشعار الرئيسي (Positioning)", 500, Multiline: true),

        new("hero_badge", "hero", "شارة الترحيب", 200),
        new("hero_kicker", "hero", "السطر العلوي (اختياري — لا يظهر في الصفحة الإنجليزية إن تُرك فارغاً)", 100, Optional: true, AllowEmpty: true),
        new("hero_title", "hero", "العنوان الرئيسي", 200),
        new("hero_subtitle", "hero", "العنوان الفرعي", 1000, Multiline: true),
        new("hero_cta_primary", "hero", "نص الزر الثانوي", 100),

        new("about_badge", "about", "شارة القسم", 100),
        new("about_title", "about", "العنوان (الجزء المميز بين { })", 200, ContentFormat.Highlight),
        new("about_text_1", "about", "النص الأول", Multiline: true),
        new("about_text_2", "about", "النص الثاني", Multiline: true),
        new("tagline", "about", "الشعار تحت الشعار في الصورة", 200),
        new("vision_title", "about", "عنوان الرؤية", 100),
        new("vision_text", "about", "نص الرؤية", 1000, Multiline: true),
        new("mission_title", "about", "عنوان الرسالة", 100),
        new("mission_text", "about", "نص الرسالة", 1000, Multiline: true),
        new("values_title", "about", "عنوان القيم", 100),
        new("values_text", "about", "نص القيم", 1000, Multiline: true, HintKey: MessageKeys.Admin.ListHint),

        new("platforms_badge", "sections", "المنصات — الشارة", 100),
        new("platforms_title", "sections", "المنصات — العنوان (المميز بين { })", 200, ContentFormat.Highlight),
        new("platforms_subtitle", "sections", "المنصات — الوصف", 1000, Multiline: true),
        new("platforms_read_more", "sections", "المنصات — نص «اعرف المزيد»", 100),
        new("news_badge", "sections", "الأخبار — الشارة", 100),
        new("news_title", "sections", "الأخبار — العنوان (المميز بين { })", 200, ContentFormat.Highlight),
        new("news_subtitle", "sections", "الأخبار — الوصف", 1000, Multiline: true),
        new("news_read_more", "sections", "الأخبار — نص «اقرأ المزيد»", 100),
        new(HomeNewsLimit, "sections", "الأخبار — عدد الأخبار المعروضة في الصفحة الرئيسية (1–50)", 2, Rule: ContentRule.Count, Neutral: true),
        new("events_badge", "sections", "الفعاليات — الشارة", 100),
        new("events_title", "sections", "الفعاليات — العنوان (المميز بين { })", 200, ContentFormat.Highlight),
        new("events_subtitle", "sections", "الفعاليات — الوصف", 1000, Multiline: true),
        new("partners_badge", "sections", "الشركاء — الشارة", 100),
        new("partners_title", "sections", "الشركاء — العنوان (المميز بين { })", 200, ContentFormat.Highlight),
        new("partners_subtitle", "sections", "الشركاء — الوصف", 1000, Multiline: true),
        new("faq_badge", "sections", "الأسئلة الشائعة — الشارة", 100),
        new("faq_title", "sections", "الأسئلة الشائعة — العنوان (المميز بين { })", 200, ContentFormat.Highlight),
        new("faq_subtitle", "sections", "الأسئلة الشائعة — الوصف", 1000, Multiline: true),
        new("ecosystem_badge", "sections", "المنظومة — الشارة", 100),
        new("ecosystem_title", "sections", "المنظومة — العنوان (المميز بين { })", 200, ContentFormat.Highlight),
        new("ecosystem_subtitle", "sections", "المنظومة — الوصف", 1000, Multiline: true),
        new("cta_band_title", "sections", "شريط الدعوة في أسفل الصفحات — العنوان (المميز بين { })", 200, ContentFormat.Highlight),
        new("cta_band_text", "sections", "شريط الدعوة — النص", 500, Multiline: true),
        new(ListPageSize, "sections", "عدد العناصر في كل صفحة من القوائم (الأخبار، الفعاليات…) (1–50)", 2, Rule: ContentRule.Count, Neutral: true),
        new(HeroStatsLimit, "sections", "عدد الأرقام في واجهة الصفحة الرئيسية (1–12)", 2, Rule: ContentRule.Count, Neutral: true, CountMax: SectionCountMax, Default: "4"),
        new(HomeEventsLimit, "sections", "الفعاليات — عدد الفعاليات في الصفحة الرئيسية (1–12)", 2, Rule: ContentRule.Count, Neutral: true, CountMax: SectionCountMax, Default: "3"),
        new(ForumEventsLimit, "sections", "عدد الفعاليات في صفحة المنتدى (1–12)", 2, Rule: ContentRule.Count, Neutral: true, CountMax: SectionCountMax, Default: "3"),

        // C5 buttons: a text (empty = the target page's title in the page language) and a target (a page, or an https link).
        new("hero_cta_main_label", "links", "زر الواجهة الرئيسي — النص (فارغ = نص زر الهيدر، أو عنوان الصفحة المختارة)", 100, AllowEmpty: true),
        new("hero_cta_main_link", "links", "زر الواجهة الرئيسي — الوجهة", 500, Rule: ContentRule.Link, Neutral: true, AllowEmpty: true, LabelKey: "hero_cta_main_label"),
        new("hero_cta_secondary_link", "links", "زر الواجهة الثانوي — الوجهة (نصه في قسم Hero)", 500, Rule: ContentRule.Link, Neutral: true,
            Default: Site.PageKeys.Ecosystem, LabelKey: "hero_cta_primary"),
        new("about_more_label", "links", "زر «من نحن» في الرئيسية — النص (فارغ = عنوان الصفحة)", 100, AllowEmpty: true),
        new("about_more_link", "links", "زر «من نحن» في الرئيسية — الوجهة", 500, Rule: ContentRule.Link, Neutral: true,
            Default: Site.PageKeys.About, LabelKey: "about_more_label"),
        new("cta_band_button1_label", "links", "شريط الدعوة — نص الزر الأول (فارغ = عنوان الصفحة)", 100, AllowEmpty: true),
        new("cta_band_button1_link", "links", "شريط الدعوة — وجهة الزر الأول", 500, Rule: ContentRule.Link, Neutral: true,
            Default: Site.PageKeys.NetworkJoin, LabelKey: "cta_band_button1_label"),
        new("cta_band_button2_label", "links", "شريط الدعوة — نص الزر الثاني (فارغ = عنوان الصفحة)", 100, AllowEmpty: true),
        new("cta_band_button2_link", "links", "شريط الدعوة — وجهة الزر الثاني (فارغ = بلا زر ثانٍ)", 500, Rule: ContentRule.Link, Neutral: true,
            AllowEmpty: true, Default: Site.PageKeys.ForumRegistration, LabelKey: "cta_band_button2_label"),
        new("events_action_label", "links", "زر الفعاليات (التسجيل) — النص (فارغ = عنوان الصفحة)", 100, AllowEmpty: true),
        new("events_action_link", "links", "زر الفعاليات — الوجهة (قوائم الفعاليات وصفحة الفعالية والمتحدث)", 500, Rule: ContentRule.Link, Neutral: true,
            Default: Site.PageKeys.ForumRegistration, LabelKey: "events_action_label"),
        new("tv_action_label", "links", "زر البرنامج التلفزيوني (الرعاية) — النص (فارغ = عنوان الصفحة)", 100, AllowEmpty: true),
        new("tv_action_link", "links", "زر البرنامج التلفزيوني — الوجهة (الحلقات والضيوف)", 500, Rule: ContentRule.Link, Neutral: true,
            Default: Site.PageKeys.TvSponsorship, LabelKey: "tv_action_label"),
        new(NotFoundPages, "links", "الصفحات المقترحة في صفحة «غير موجودة» (404) — حتى 6 صفحات", 500, Rule: ContentRule.PageKeyList, Neutral: true,
            Default: "home,ecosystem,contact"),

        // C5 section icons (a key of Icons; empty or unknown = the default).
        new("vision_icon", "icons", "أيقونة الرؤية", 40, Rule: ContentRule.IconKey, Neutral: true, AllowEmpty: true, Default: "eye"),
        new("mission_icon", "icons", "أيقونة الرسالة", 40, Rule: ContentRule.IconKey, Neutral: true, AllowEmpty: true, Default: "target"),
        new("values_icon", "icons", "أيقونة القيم", 40, Rule: ContentRule.IconKey, Neutral: true, AllowEmpty: true, Default: "handshake"),
        new("ecosystem_icon", "icons", "شارة قسم المنظومة", 40, Rule: ContentRule.IconKey, Neutral: true, AllowEmpty: true, Default: "layers"),
        new("platforms_icon", "icons", "شارة قسم الخدمات", 40, Rule: ContentRule.IconKey, Neutral: true, AllowEmpty: true, Default: "grid"),
        new("events_icon", "icons", "شارة قسم الفعاليات", 40, Rule: ContentRule.IconKey, Neutral: true, AllowEmpty: true, Default: "calendar"),
        new("news_icon", "icons", "شارة قسم الأخبار", 40, Rule: ContentRule.IconKey, Neutral: true, AllowEmpty: true, Default: "book"),
        new("partners_icon", "icons", "شارة قسم الشركاء", 40, Rule: ContentRule.IconKey, Neutral: true, AllowEmpty: true, Default: "handshake"),
        new("faq_icon", "icons", "شارة قسم الأسئلة الشائعة", 40, Rule: ContentRule.IconKey, Neutral: true, AllowEmpty: true, Default: "question"),
        new("contact_icon", "icons", "شارة قسم التواصل", 40, Rule: ContentRule.IconKey, Neutral: true, AllowEmpty: true, Default: "mic"),
        new("register_icon", "icons", "شارة قسم التسجيل السريع", 40, Rule: ContentRule.IconKey, Neutral: true, AllowEmpty: true, Default: "user_plus"),
        new(DefaultUtcOffset, "sections", "فرق التوقيت الافتراضي للفعاليات الجديدة عن UTC (مثال: ‎+02:00)", 6, Rule: ContentRule.UtcOffset, Neutral: true),

        new("impact_title", "pages", "الأثر المتوقع — العنوان (المميز بين { })", 200, ContentFormat.Highlight),
        new("impact_intro", "pages", "الأثر المتوقع — النص التمهيدي (اختياري)", 500, AllowEmpty: true),

        new("register_badge", "register", "الشارة", 100),
        new("register_title", "register", "العنوان (المميز بين { })", 200, ContentFormat.Highlight),
        new("register_text", "register", "النص", 1000, Multiline: true),

        new("contact_badge", "contact", "الشارة", 100),
        new("contact_title", "contact", "العنوان (المميز بين { })", 200, ContentFormat.Highlight),
        new("contact_subtitle", "contact", "الوصف", 1000, Multiline: true),
        new("contact_intro", "contact", "النص التعريفي", 1000, Multiline: true),
        new("contact_address", "contact", "العنوان", 300),
        new("contact_phone", "contact", "الهاتف (مثال: ‎+20 1141410808)", 50, Rule: ContentRule.Phone, Neutral: true),
        new("contact_email", "contact", "البريد الإلكتروني", 200, Neutral: true),
        new("contact_whatsapp", "contact", "رقم واتساب بالصيغة الدولية — أرقام فقط (اختياري)", 15, Rule: ContentRule.Digits, Neutral: true, AllowEmpty: true),
        new("contact_website", "contact", "الموقع الإلكتروني (https — اختياري)", 200, Rule: ContentRule.HttpsUrl, Neutral: true, AllowEmpty: true),
        new("contact_hours", "contact", "ساعات العمل", 200),

        new("footer_description", "footer", "وصف الفوتر", 1000, Multiline: true),
        new("footer_quicklinks_title", "footer", "عنوان «روابط سريعة»", 100),
        new("footer_contact_title", "footer", "عنوان «تواصل معنا»", 100),
        new("footer_copyright", "footer", "حقوق النشر ({year} = السنة الحالية)", 300, ContentFormat.Year),
        new("footer_credit_text", "footer", "نص «تصميم وتطوير بواسطة»", 200),
        new("footer_credit_name", "footer", "اسم الجهة المطوّرة", 200),
        new("footer_credit_url", "footer", "رابط الجهة المطوّرة (https)", 500, Rule: ContentRule.HttpsUrl, Neutral: true, AllowEmpty: true),

        new("site_name", "seo", "اسم الموقع (العنوان، نص الشعار البديل، og:site_name)", 100),
        new("seo_og_image", "seo", "صورة المشاركة (مسار داخل الموقع يبدأ بـ /)", 300, Rule: ContentRule.SitePath, Neutral: true, AllowEmpty: true),
        new("seo_og_image_alt", "seo", "النص البديل لصورة المشاركة", 200),
        new("site_base_url", "seo", "رابط الموقع الأساسي (https://النطاق — مطلوب للـ canonical وخريطة الموقع)", 200, Rule: ContentRule.BaseUrl, Neutral: true, AllowEmpty: true),
        new("org_address_locality", "seo", "المدينة (للبيانات المنظمة — اختياري)", 100, Neutral: true, AllowEmpty: true),
        new("org_address_country", "seo", "رمز الدولة (للبيانات المنظمة — اختياري، حرفان مثل EG)", 10, Rule: ContentRule.CountryCode, Neutral: true, AllowEmpty: true),

        new("founder_heading", "founder", "عنوان القسم (المميز بين { })", 200, ContentFormat.Highlight),
        new("founder_name", "founder", "الاسم", 200),
        new("founder_title", "founder", "الصفة", 200),
        new("founder_message", "founder", "نص الكلمة (فارغ = القسم مخفي)", Multiline: true, AllowEmpty: true),

        // Headings of the lists (ContentItems) on the pages: one title + optional lead sentence per group.
        .. ContentItemGroups.All.Where(g => !g.FormOptions && g.TitleKey != "values_title").Select(g =>
            new ContentKey(g.TitleKey, "pages", $"{g.Label} — العنوان (المميز بين {{ }})", 200, ContentFormat.Highlight)),
        .. ContentItemGroups.All.Where(g => !g.FormOptions).Select(g =>
            new ContentKey(g.IntroKey!, "pages", $"{g.Label} — النص التمهيدي (اختياري)", 500, AllowEmpty: true)),

        // The lead forms (Forum registration, Join the Network, sponsorship, opportunity interest): AddLeadForms inserts them.
        new(FormsConsent, "forms", "نص الموافقة بجانب مربع الاختيار (إلزامي في نماذج الطلبات)", 500, Multiline: true),
        new("join_kind_title", "forms", "نموذج الانضمام — سؤال نوع الطلب", 200),
        new("join_side_title", "forms", "نموذج الانضمام — عنوان العمود الجانبي (المميز بين { })", 200, ContentFormat.Highlight),
        new("join_side_text", "forms", "نموذج الانضمام — نص العمود الجانبي", 1000, Multiline: true),
        new("sponsorship_form_title", "forms", "نموذج الرعاية — العنوان (المميز بين { })", 200, ContentFormat.Highlight),
        new("sponsorship_form_text", "forms", "نموذج الرعاية — النص", 1000, Multiline: true),
        new("interest_form_title", "forms", "نموذج «أبدِ اهتمامك» (صفحة الفرص) — العنوان (المميز بين { })", 200, ContentFormat.Highlight),
        new("interest_form_text", "forms", "نموذج «أبدِ اهتمامك» — النص", 1000, Multiline: true),
    ];

    private static readonly Dictionary<string, ContentKey> ByKey = All.ToDictionary(k => k.Key);

    /// <summary>The stored list page size when valid, else <see cref="ListPageSizeFallback"/>.</summary>
    public static int PageSize(IReadOnlyDictionary<string, TextPair> content) =>
        ParseCount(content.GetValueOrDefault(ListPageSize)?.Ar, ListPageSizeFallback);

    public static ContentKey? Find(string key) => ByKey.GetValueOrDefault(key);
}
