using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovestHolding.Migrations
{
    /// <summary>
    /// The data steps of AddAdminControl. Every literal is FROZEN here (never read from application code — a test pins
    /// the section list to Infrastructure/Site/PageSectionRegistry). NC-0: every step only INSERTs a missing row, or
    /// initialises a column this migration created (Pages.ShowInFooter), or fills a page intro that is EMPTY; no value
    /// an admin stored is changed. Each step is guarded, so a second run does nothing. Down undoes a row only while it
    /// still equals what Up wrote (binary comparison).
    /// </summary>
    public partial class AddAdminControl
    {
        private const string Bin = "COLLATE Latin1_General_100_BIN2";

        /// <summary>
        /// N'…' with quotes doubled; NULL for null. A multi-line value is built with NCHAR(10) between its lines, as
        /// nvarchar(max): the SQL generator re-joins a statement's lines with the machine's line end, which would turn
        /// an LF inside a literal into CRLF (and a long literal would be cut at 4000 characters by a plain + chain).
        /// </summary>
        private static string L(string value)
        {
            if (value is null) return "NULL";
            var lines = value.Split('\n').Select(line => "N'" + line.Replace("'", "''") + "'");
            return value.Contains('\n')
                ? "(CAST(N'' AS nvarchar(max)) + " + string.Join(" + NCHAR(10) + ", lines) + ")"
                : "N'" + value.Replace("'", "''") + "'";
        }

        /// <summary>
        /// The sections of each page in the order the C4 code renders them (PageSections: SortOrder 10, 20, …, all
        /// visible). The home hero is not a section; its figures row is the hide-only "hero_stats".
        /// </summary>
        public static readonly (string Page, string[] Sections)[] SectionRows =
        [
            ("home", ["hero_stats", "about", "about_values", "ecosystem", "platforms", "stats", "events", "news", "partners", "faq", "register", "contact", "cta_band"]),
            ("about", ["vision", "values", "why", "stats", "sectors", "partners_in_success", "founder", "cta_band"]),
            ("ecosystem", ["units", "pillars", "services", "programs", "government", "universities", "cta_band"]),
            ("contact", ["contact", "faq"]),
            ("forum", ["units", "components", "events", "cta_band"]),
            ("network", ["units", "investor_services", "steps", "cta_band"]),
            ("network.investors", ["investor_services", "join_cta"]),
            ("network.startups", ["startup_services", "sectors", "join_cta"]),
            ("network.opportunities", ["list", "interest_form"]),
            ("forum.partners", ["partners", "packages", "partner_types"]),
            ("tv.sponsorship", ["packages", "form"]),
            ("forum.events", ["list"]),
            ("forum.speakers", ["list"]),
            ("news", ["list"]),
            ("tv.episodes", ["list"]),
            ("tv.guests", ["list"]),
            ("forum.registration", ["form"]),
            ("network.join", ["form"]),
            ("tv", ["units"]),
        ];

        /// <summary>
        /// New SiteContents structure keys: key, Arabic value, English value. The values reproduce the C4 rendering:
        /// link keys hold the page each button opens today, labels are empty (= the target page's title, as today),
        /// counts are today's fixed numbers, icon keys are today's icons, the 404 links today's three pages.
        /// </summary>
        public static readonly string[][] NewKeys =
        [
            // Buttons and links (rule Link: a page key or an https URL; "" = the default).
            ["hero_cta_main_label", "", null],
            ["hero_cta_main_link", "", null],
            ["hero_cta_secondary_link", "ecosystem", null],
            ["about_more_label", "", null],
            ["about_more_link", "about", null],
            ["cta_band_button1_label", "", null],
            ["cta_band_button1_link", "network.join", null],
            ["cta_band_button2_label", "", null],
            ["cta_band_button2_link", "forum.registration", null],
            ["events_action_label", "", null],
            ["events_action_link", "forum.registration", null],
            ["tv_action_label", "", null],
            ["tv_action_link", "tv.sponsorship", null],
            ["notfound_pages", "home,ecosystem,contact", null],
            // Counts.
            ["hero_stats_limit", "4", null],
            ["home_events_limit", "3", null],
            ["forum_events_limit", "3", null],
            // Section icons (keys of Infrastructure/Content/Icons).
            ["vision_icon", "eye", null],
            ["mission_icon", "target", null],
            ["values_icon", "handshake", null],
            ["ecosystem_icon", "layers", null],
            ["platforms_icon", "grid", null],
            ["events_icon", "calendar", null],
            ["news_icon", "book", null],
            ["partners_icon", "handshake", null],
            ["faq_icon", "question", null],
            ["contact_icon", "mic", null],
            ["register_icon", "user_plus", null],
        ];

        // ── Texts of CONTENT-C5.md (Mona; approved FINAL by Anas, 2026-10-23), copied word for word. Plain text: a blank
        //    line starts a new paragraph (LF line ends, as the admin editor stores them). ──

        public const string PrivacyBodyAr =
            "تشرح هذه السياسة، بلغة واضحة، ما الذي يجمعه موقع إنوفست هولدينغ (www.innovest-holding.com) من بياناتك، ولماذا، ومن يطّلع عليها، وما الذي لا نجمعه.\n" +
            "\n" +
            "1. متى نجمع بياناتك\n" +
            "\n" +
            "لا يطلب الموقع منك إنشاء حساب، ولا نجمع عنك شيئاً سوى ما تكتبه بنفسك في أحد النماذج التالية وترسله:\n" +
            "\n" +
            "- نموذج التسجيل السريع.\n" +
            "- نموذج التسجيل في المنتدى.\n" +
            "- نموذج الانضمام إلى الشبكة (بصفتك مستثمراً أو شركة ناشئة).\n" +
            "- نموذج طلب عرض الرعاية.\n" +
            "- نموذج إبداء الاهتمام بإحدى الفرص الاستثمارية.\n" +
            "- نموذج اتصل بنا.\n" +
            "\n" +
            "2. ما البيانات التي نجمعها\n" +
            "\n" +
            "في جميع النماذج: الاسم الكامل والبريد الإلكتروني. وفي نماذج التسجيل والانضمام والرعاية والاهتمام بالفرص: رقم الهاتف (اختياري)، وفي نموذج اتصل بنا: موضوع الرسالة ونصها.\n" +
            "\n" +
            "وبحسب النموذج، قد نطلب أيضاً: جهة العمل أو اسم الشركة، والمسمى الوظيفي، والدولة، ومجال الاهتمام، والفعالية أو الفرصة أو باقة الرعاية التي تخصّها رسالتك، ونوع المستثمر وحجم الاستثمار والقطاعات، واسم الشركة الناشئة ومجالها ومرحلتها والتمويل المطلوب ورابط موقعها أو حسابها المهني، ونبذة أو رسالة حرة تكتبها أنت.\n" +
            "\n" +
            "ما تكتبه في الحقول الحرة يصل إلينا كما هو، فلا تُدخل فيها بيانات لا ترغب بمشاركتها.\n" +
            "\n" +
            "نحفظ مع كل طلب أيضاً: وقت إرساله، ولغة الصفحة التي أرسلته منها (عربية أو إنجليزية)، ووقت موافقتك في النماذج التي تتطلب موافقة.\n" +
            "\n" +
            "3. موافقتك\n" +
            "\n" +
            "نماذج التسجيل في المنتدى والانضمام إلى الشبكة وطلب الرعاية والاهتمام بالفرص لا تُرسَل إلا بعد تأشير خانة الموافقة التي نصها: «أوافق على أن تتواصل معي إنوفست هولدينغ بخصوص طلبي، وأن تحتفظ ببياناتي لهذا الغرض فقط.»\n" +
            "\n" +
            "4. ما الذي لا نخزّنه\n" +
            "\n" +
            "لا يحفظ الموقع عنوان الـIP الخاص بك ولا نوع متصفحك مع طلبك. ولا نسجّل محتوى ما ترسله في سجلات التشغيل. يُستخدم عنوان الـIP مؤقتاً في ذاكرة الخادم فقط لتحديد عدد مرات الإرسال المتكررة ومنع إساءة استخدام النماذج، ولا يُربط برسالتك ويُمحى عند إعادة تشغيل الموقع.\n" +
            "\n" +
            "5. كيف نستخدم بياناتك\n" +
            "\n" +
            "نستخدمها للرد على طلبك والتواصل معك بشأنه فقط. لا نبيع بياناتك ولا نشاركها مع جهات تسويقية، ولا نستخدمها لإرسال نشرات لم تطلبها.\n" +
            "\n" +
            "6. من يطّلع عليها\n" +
            "\n" +
            "لا تظهر بياناتك على الموقع للزوار. يطّلع عليها مشرفو الموقع في إنوفست هولدينغ عبر لوحة الإدارة المحمية بتسجيل الدخول، وهم من يتولون متابعة طلبك والرد عليه. وقد يضيف المشرفون ملاحظات داخلية على الطلب لمتابعته.\n" +
            "\n" +
            "7. ملفات تعريف الارتباط (الكوكيز)\n" +
            "\n" +
            "يستخدم الموقع ملفات ضرورية فقط:\n" +
            "\n" +
            "- ملف حماية النماذج: يُنشأ عند فتح صفحة فيها نموذج، ويمنع إرسال النماذج من مواقع أخرى بغير علمك.\n" +
            "- ملف جلسة الإدارة: يُنشأ للمشرفين فقط عند تسجيل دخولهم، ولا يُنشأ للزوار.\n" +
            "\n" +
            "لا نستخدم كوكيز للإعلان أو التتبع، ولا أدوات تحليلات، ولا بكسلات أو مواقع تتبّع من أطراف ثالثة. لغة الصفحة تُحدَّد من عنوان الرابط (الصفحات العربية بلا بادئة، والإنجليزية تبدأ بـ /en)، ولا تُحفظ في كوكيز.\n" +
            "\n" +
            "8. خدمات من أطراف أخرى\n" +
            "\n" +
            "- الخطوط: يحمّل الموقع خطوطه من خدمة Google Fonts، لذلك يتلقى مزوّد الخدمة طلباً من متصفحك (يتضمن عنوان الـIP الخاص بك) عند فتح أي صفحة، وفق سياسة خصوصيته.\n" +
            "- الفيديو: تُعرض بعض الحلقات بمشغّل يوتيوب بنمط الخصوصية المعزَّز (youtube-nocookie.com). لا يتصل متصفحك بيوتيوب إلا عندما تضغط على زر التشغيل. وبعض الفيديوهات تظهر كرابط يفتح موقعاً آخر عند الضغط عليه، ولكل موقع سياسته الخاصة.\n" +
            "- الروابط الخارجية: عند الانتقال من الموقع إلى حساباتنا على مواقع التواصل أو أي موقع آخر، تنطبق سياسة ذلك الموقع.\n" +
            "\n" +
            "9. مدة الاحتفاظ\n" +
            "\n" +
            "نحتفظ ببياناتك طالما كان ذلك لازماً للتعامل مع طلبك. يمكنك في أي وقت أن تطلب حذف بياناتك أو تصحيحها أو الاطلاع على ما نحتفظ به عنك.\n" +
            "\n" +
            "10. حقوقك وكيف تتواصل معنا\n" +
            "\n" +
            "لأي طلب يتعلق بخصوصيتك (الاطلاع على بياناتك أو تصحيحها أو حذفها أو سحب موافقتك) تواصل معنا عبر بريد التواصل المذكور في صفحة اتصل بنا، واذكر الاسم والبريد الإلكتروني الذي استخدمته في النموذج حتى نتمكن من العثور على طلبك.\n" +
            "\n" +
            "11. حماية البيانات\n" +
            "\n" +
            "نحمي النماذج برمز تحقق من كل طلب، ونحدّ عدد مرات الإرسال، ونقصر الاطلاع على الطلبات على المشرفين بعد تسجيل دخولهم. ومع ذلك لا توجد وسيلة إرسال عبر الإنترنت آمنة بالكامل.\n" +
            "\n" +
            "12. التعديلات على هذه السياسة\n" +
            "\n" +
            "قد نعدّل هذه السياسة عند تغيّر ما يفعله الموقع. سيظهر النص المحدَّث على هذه الصفحة، وبه نعتمد في التعامل مع الطلبات المرسلة بعد نشره.";

        public const string PrivacyBodyEn =
            "This policy explains, in plain language, what the Innovest Holding website (www.innovest-holding.com) collects about you, why, who can see it, and what it does not collect.\n" +
            "\n" +
            "1. When we collect your data\n" +
            "\n" +
            "The site does not ask you to create an account. We collect nothing about you except what you type yourself into one of these forms and send:\n" +
            "\n" +
            "- The quick registration form.\n" +
            "- The forum registration form.\n" +
            "- The Join the Network form (as an investor or as a startup).\n" +
            "- The sponsorship offer request form.\n" +
            "- The opportunity interest form.\n" +
            "- The Contact Us form.\n" +
            "\n" +
            "2. What data we collect\n" +
            "\n" +
            "In every form: your full name and email address. In the registration, join, sponsorship and opportunity forms: a phone number (optional). In the Contact Us form: the subject and the text of your message.\n" +
            "\n" +
            "Depending on the form, we may also ask for: your organisation or company name, job title, country, area of interest, the event, opportunity or sponsorship package your message is about, investor type, ticket size and sectors, startup name, sector, stage, funding sought, the link to your website or professional profile, and a free-text summary or message that you write.\n" +
            "\n" +
            "What you type in free-text fields reaches us as written, so please do not enter details you do not want to share.\n" +
            "\n" +
            "With each request we also store: the time it was sent, the language of the page you sent it from (Arabic or English), and, in the forms that require consent, the time you gave it.\n" +
            "\n" +
            "3. Your consent\n" +
            "\n" +
            "The forum registration, Join the Network, sponsorship request and opportunity interest forms can only be sent after you tick the consent box, which reads: \"I agree that Innovest Holding may contact me about my request and keep my details for that purpose only.\"\n" +
            "\n" +
            "4. What we do not store\n" +
            "\n" +
            "The site does not store your IP address or your browser type with your request, and it does not write what you submit into its operating logs. Your IP address is used only temporarily in the server's memory to count repeated submissions and prevent abuse of the forms; it is not linked to your message and is cleared when the site restarts.\n" +
            "\n" +
            "5. How we use your data\n" +
            "\n" +
            "We use it only to respond to your request and to contact you about it. We do not sell your data, do not share it with marketing companies, and do not use it to send newsletters you did not ask for.\n" +
            "\n" +
            "6. Who can see it\n" +
            "\n" +
            "Your data is not shown to visitors on the site. It is visible to the Innovest Holding site administrators through the admin area, which requires signing in, and they follow up and reply to your request. Administrators may add internal notes to a request to track it.\n" +
            "\n" +
            "7. Cookies\n" +
            "\n" +
            "The site uses only the cookies it needs to work:\n" +
            "\n" +
            "- A form-protection cookie: created when you open a page that has a form; it prevents forms from being submitted from other sites without your knowledge.\n" +
            "- An admin session cookie: created only for administrators when they sign in; it is not created for visitors.\n" +
            "\n" +
            "We do not use advertising or tracking cookies, analytics tools, or third-party tracking pixels or sites. The page language is determined by the URL (Arabic pages have no prefix, English pages start with /en) and is not stored in a cookie.\n" +
            "\n" +
            "8. Third-party services\n" +
            "\n" +
            "- Fonts: the site loads its fonts from Google Fonts, so Google receives a request from your browser (which includes your IP address) when you open any page, under Google's own privacy policy.\n" +
            "- Video: some episodes are shown in a YouTube player in enhanced-privacy mode (youtube-nocookie.com). Your browser does not contact YouTube until you press the play button. Some videos appear as a link that opens another site when you click it, and each site has its own policy.\n" +
            "- External links: when you leave the site for our social media accounts or any other site, that site's policy applies.\n" +
            "\n" +
            "9. How long we keep it\n" +
            "\n" +
            "We keep your data for as long as it is needed to handle your request. You can ask us at any time to delete or correct your data, or to tell you what we hold about you.\n" +
            "\n" +
            "10. Your rights and how to contact us\n" +
            "\n" +
            "For any privacy request (to see, correct or delete your data, or to withdraw your consent), contact us through the contact email shown on the Contact Us page, and tell us the name and email address you used in the form so we can find your request.\n" +
            "\n" +
            "11. Protecting your data\n" +
            "\n" +
            "We protect each form with a verification token, limit how often forms can be submitted, and restrict access to requests to signed-in administrators. Even so, no method of sending data over the internet is completely secure.\n" +
            "\n" +
            "12. Changes to this policy\n" +
            "\n" +
            "We may update this policy when what the site does changes. The updated text will appear on this page and applies to requests sent after it is published.";

        public const string TvIntroAr =
            "البرنامج التلفزيوني لإنوفست هولدينغ: تعرّف هنا على حلقات البرنامج وضيوفه وفرص رعايته، ضمن رسالة الشركة في ربط رواد الأعمال بالمستثمرين ودعم الابتكار.";

        public const string TvIntroEn =
            "The Innovest Holding TV program: find its episodes, guests and sponsorship opportunities here, in line with the company's mission of connecting entrepreneurs with investors and supporting innovation.";

        /// <summary>
        /// The privacy page row (Anas, 2026-10-23: shipped with its text, CONTENT-C5.md; meta pair ar+en so /en/privacy is
        /// indexable too — written only with the row, on insert): column → value. Not in the
        /// header menu nor the footer columns (its link sits in the footer's bottom bar and next to each consent box).
        /// </summary>
        public static readonly (string Column, string Value)[] PrivacyRow =
        [
            ("TitleAr", "سياسة الخصوصية"),
            ("TitleEn", "Privacy policy"),
            ("SummaryAr", ""),
            ("SummaryEn", null),
            ("IntroAr", ""),
            ("IntroEn", null),
            ("BodyAr", PrivacyBodyAr),
            ("BodyEn", PrivacyBodyEn),
            ("EmptyTextAr", "سيتم نشر سياسة الخصوصية قريباً."),
            ("EmptyTextEn", "The privacy policy will be published soon."),
            ("MetaTitleAr", "سياسة الخصوصية"),
            ("MetaTitleEn", "Privacy Policy"),
            ("MetaDescriptionAr", "كيف يتعامل موقع إنوفست هولدينغ مع البيانات التي ترسلها عبر النماذج: ما نجمعه، ولماذا، ومن يطّلع عليه، وحقوقك."),
            ("MetaDescriptionEn", "How the Innovest Holding website handles the data you send through its forms: what we collect, why, who sees it, and your rights."),
        ];

        /// <summary>The TV hub intro: column → text (IntroAr is NOT NULL: its empty value is ''; IntroEn's is NULL).</summary>
        public static readonly (string Column, string Text)[] TvIntro = [("IntroAr", TvIntroAr), ("IntroEn", TvIntroEn)];

        /// <summary>The pages under the home page (the privacy page's siblings): it is ordered after them.</summary>
        private const string HomeChildren = "'about', 'ecosystem', 'news', 'contact'";

        /// <summary>
        /// A statement that names a column ADDED by this migration runs through EXEC: a generated script is one batch,
        /// and SQL Server compiles a batch before the ALTER TABLE has run ("Invalid column name").
        /// </summary>
        private static string Exec(string sql) => "EXEC(N'" + sql.Replace("'", "''") + "');";

        private static void UpData(MigrationBuilder mb)
        {
            // 1. Footer membership of a page = its menu membership today (a column created above, not an admin value).
            mb.Sql(Exec("UPDATE Pages SET ShowInFooter = ShowInMenu;"));

            // 2. One section row per (page, section), only where missing (IX_PageSections_PageKey_SectionKey is unique).
            var sections = string.Join(",\n", SectionRows.SelectMany(p => p.Sections.Select((s, i) => $"({L(p.Page)}, {L(s)}, {(i + 1) * 10})")));
            mb.Sql("INSERT INTO PageSections (PageKey, SectionKey, IsVisible, SortOrder, UpdatedAt)\n" +
                   $"SELECT v.P, v.S, 1, v.O, SYSUTCDATETIME() FROM (VALUES\n{sections}\n) v(P, S, O) " +
                   "WHERE NOT EXISTS (SELECT 1 FROM PageSections x WHERE x.PageKey = v.P AND x.SectionKey = v.S);");

            // 3. New content keys, each only when missing (IX_SiteContents_SectionKey is unique).
            var keys = string.Join(",\n", NewKeys.Select(r => $"({L(r[0])}, {L(r[1])}, {L(r[2])})"));
            mb.Sql("INSERT INTO SiteContents (SectionKey, ContentAr, ContentEn, UpdatedAt)\n" +
                   $"SELECT v.K, v.Ar, v.En, SYSUTCDATETIME() FROM (VALUES\n{keys}\n) v(K, Ar, En) " +
                   "WHERE NOT EXISTS (SELECT 1 FROM SiteContents s WHERE s.SectionKey = v.K);");

            // 4. A row for each known network that has none (no URL → not rendered), after the existing ones.
            mb.Sql("""
                INSERT INTO SocialLinks (Network, Url, SortOrder, IsActive, CreatedAt)
                SELECT v.N, NULL, ISNULL((SELECT MAX(SortOrder) FROM SocialLinks), 0) + ROW_NUMBER() OVER (ORDER BY v.I), 1, SYSUTCDATETIME()
                FROM (VALUES (N'linkedin', 1), (N'twitter', 2), (N'instagram', 3), (N'youtube', 4), (N'facebook', 5), (N'tiktok', 6), (N'linktree', 7)) v(N, I)
                WHERE NOT EXISTS (SELECT 1 FROM SocialLinks s WHERE s.Network = v.N);
                """);

            // 5. The privacy page, only when it has no row.
            mb.Sql(Exec("INSERT INTO Pages ([Key], " + string.Join(", ", PrivacyRow.Select(c => "[" + c.Column + "]")) +
                   ", ShowInMenu, ShowInFooter, IsPublished, SortOrder, UpdatedAt)\n" +
                   "SELECT 'privacy', " + string.Join(", ", PrivacyRow.Select(c => L(c.Value))) +
                   $", 0, 0, 1, ISNULL((SELECT MAX(SortOrder) FROM Pages WHERE [Key] IN ({HomeChildren})), 0) + 1, SYSUTCDATETIME()\n" +
                   "WHERE NOT EXISTS (SELECT 1 FROM Pages WHERE [Key] = 'privacy');"));

            // 6. The TV hub intro, each language only where it is EMPTY (NULL-safe) — an admin's intro is never replaced.
            foreach (var (column, text) in TvIntro)
                mb.Sql($"UPDATE Pages SET [{column}] = {L(text)}, UpdatedAt = SYSUTCDATETIME() " +
                       $"WHERE [Key] = 'tv' AND ([{column}] IS NULL OR LTRIM(RTRIM([{column}])) = N'');");
        }

        private static void DownData(MigrationBuilder mb)
        {
            // The TV intro back to empty, only while it still equals what Up wrote.
            mb.Sql($"UPDATE Pages SET IntroAr = N'' WHERE [Key] = 'tv' AND IntroAr = {L(TvIntroAr)} {Bin};");
            mb.Sql($"UPDATE Pages SET IntroEn = NULL WHERE [Key] = 'tv' AND IntroEn = {L(TvIntroEn)} {Bin};");

            // The privacy row, only while every text still equals what Up wrote.
            mb.Sql("DELETE FROM Pages WHERE [Key] = 'privacy' AND " + string.Join(" AND ", PrivacyRow.Select(c =>
                c.Value is null ? $"[{c.Column}] IS NULL" : $"[{c.Column}] = {L(c.Value)} {Bin}")) + ";");

            // Keys Up inserted, only while they still hold Up's values (an admin edit stays). The social rows stay: Up
            // cannot be told apart from an existing row without a URL, and an empty URL is not rendered.
            foreach (var r in NewKeys)
                mb.Sql($"DELETE FROM SiteContents WHERE SectionKey = {L(r[0])} AND ContentAr = {L(r[1])} {Bin} AND " +
                       (r[2] is null ? "ContentEn IS NULL;" : $"ContentEn = {L(r[2])} {Bin};"));
        }
    }
}
