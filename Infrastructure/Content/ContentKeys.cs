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
}

/// <param name="Key">SiteContents.SectionKey.</param>
/// <param name="Group">Admin card (one of <see cref="ContentKeys.Groups"/>).</param>
/// <param name="Label">Arabic admin label.</param>
/// <param name="Neutral">One value for both languages (stored in ContentAr, ContentEn stays NULL).</param>
/// <param name="Optional">No fallback: the English page shows nothing when English is empty.</param>
/// <param name="AllowEmpty">The Arabic value may be empty (otherwise it is required).</param>
public sealed record ContentKey(
    string Key, string Group, string Label, int MaxLength = ContentKeys.DefaultMax,
    ContentFormat Format = ContentFormat.Plain, ContentRule Rule = ContentRule.None,
    bool Neutral = false, bool Optional = false, bool AllowEmpty = false, bool Multiline = false, string? HintKey = null);

/// <summary>
/// Registry of every SiteContents key the site reads. The admin editor is generated from it (only these keys can be
/// saved — no over-posting); the migrations AddBilingualContent and ApplyCompanyProfile insert every one of them.
/// Rows that are no longer read (seo_title, seo_description — the home page's SEO is in Pages since the multi-page
/// site; footer_platforms_title) stay in the database untouched but are not listed here.
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

    /// <summary>The stored value when it is a valid count, else <paramref name="fallback"/>.</summary>
    public static int ParseCount(string? value, int fallback) =>
        int.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var n)
        && n is >= 1 and <= MaxCount ? n : fallback;

    /// <summary>Items per page of the public list pages (news, events…).</summary>
    public const string ListPageSize = "list_page_size";

    public const int ListPageSizeFallback = 12;

    /// <summary>The UTC offset a new event's date form starts with (events keep their own offset).</summary>
    public const string DefaultUtcOffset = "default_utc_offset";

    public const string DefaultUtcOffsetFallback = "+02:00";

    /// <summary>The founder block's photo: the FileKey of an uploaded image, written only by the Content editor's
    /// photo form (not a text field of the registry).</summary>
    public const string FounderPhoto = "founder_photo";

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
        ("sections", "عناوين أقسام الصفحة الرئيسية"),
        ("pages", "عناوين أقسام الصفحات"),
        ("founder", "كلمة المؤسس (تُخفى ما دام نص الكلمة فارغاً)"),
        ("register", "قسم التسجيل"),
        ("contact", "معلومات التواصل"),
        ("footer", "الفوتر"),
        ("seo", "SEO وبيانات الموقع"),
    ];

    public static readonly IReadOnlyList<ContentKey> All =
    [
        new("cta_register", "header", "نص زر «سجّل الآن»", 100),
        new("header_cta_label", "header", "نص زر الهيدر", 100),
        new("header_cta_page", "header", "الصفحة التي يفتحها زر الهيدر", 60, Rule: ContentRule.PageKey, Neutral: true),

        new("brand_positioning", "brand", "الشعار الرئيسي (Positioning)", 500, Multiline: true),

        new("hero_badge", "hero", "شارة الترحيب", 200),
        new("hero_kicker", "hero", "السطر العلوي (اختياري — لا يظهر في الصفحة الإنجليزية إن تُرك فارغاً)", 100, Optional: true, AllowEmpty: true),
        new("hero_title", "hero", "العنوان الرئيسي", 200),
        new("hero_subtitle", "hero", "العنوان الفرعي", 1000, Multiline: true),
        new("hero_cta_primary", "hero", "نص الزر الأول", 100),

        new("about_badge", "about", "شارة القسم", 100),
        new("about_title", "about", "العنوان (الجزء المميز بين { })", 200, ContentFormat.Highlight),
        new("about_text_1", "about", "النص الأول", Multiline: true),
        new("about_text_2", "about", "النص الثاني", Multiline: true),
        new("tagline", "about", "الشعار تحت الشعار في الصورة", 200),
        new("about_years", "about", "سنوات الخبرة", 50, Neutral: true),
        new("about_years_label", "about", "نص شارة الخبرة (كل سطر في سطر)", 200, ContentFormat.Lines, Multiline: true),
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
        new("contact_phone", "contact", "الهاتف", 50, Neutral: true),
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
        new("org_address_country", "seo", "رمز الدولة (للبيانات المنظمة — اختياري، مثل EG)", 10, Neutral: true, AllowEmpty: true),

        new("founder_heading", "founder", "عنوان القسم (المميز بين { })", 200, ContentFormat.Highlight),
        new("founder_name", "founder", "الاسم", 200),
        new("founder_title", "founder", "الصفة", 200),
        new("founder_message", "founder", "نص الكلمة (فارغ = القسم مخفي)", Multiline: true, AllowEmpty: true),

        // Headings of the lists (ContentItems) on the pages: one title + optional lead sentence per group.
        .. ContentItemGroups.All.Where(g => g.TitleKey != "values_title").Select(g =>
            new ContentKey(g.TitleKey, "pages", $"{g.Label} — العنوان (المميز بين {{ }})", 200, ContentFormat.Highlight)),
        .. ContentItemGroups.All.Select(g =>
            new ContentKey(g.IntroKey!, "pages", $"{g.Label} — النص التمهيدي (اختياري)", 500, AllowEmpty: true)),
    ];

    private static readonly Dictionary<string, ContentKey> ByKey = All.ToDictionary(k => k.Key);

    /// <summary>The stored list page size when valid, else <see cref="ListPageSizeFallback"/>.</summary>
    public static int PageSize(IReadOnlyDictionary<string, TextPair> content) =>
        ParseCount(content.GetValueOrDefault(ListPageSize)?.Ar, ListPageSizeFallback);

    public static ContentKey? Find(string key) => ByKey.GetValueOrDefault(key);
}
