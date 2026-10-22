using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovestHolding.Migrations
{
    /// <summary>
    /// Data only: the company profile (res/Company Profile Content.docx) replaces the placeholder content of the bilingual release.
    ///
    /// Rule (owner decision, 2026-10-22): a value is replaced ONLY while it still equals the old seed text — an admin edit is
    /// never overwritten. Every literal is frozen here (old values copied from AddBilingualContent.Data.cs and the
    /// seeder of that release, new values from the profile); comparisons are binary (BIN2) and NULL-safe, and
    /// every written column is compared with its OWN old value, so an edit to any one column keeps that column. Each step does LESS when the data differs, and a second run changes nothing.
    /// Events, News and Partners are not touched (they are real data). DbSeeder writes the same Platforms and Stats for
    /// fresh installs (test Fresh_and_upgraded_databases_have_the_same_content).
    /// </summary>
    public partial class ApplyCompanyProfile : Migration
    {
        private const string Bin = "COLLATE Latin1_General_100_BIN2";

        /// <summary>
        /// N'…' with quotes doubled; NULL for null. A line break is written as NCHAR(10) between two literals: EF's SQL
        /// Server generator re-joins a Sql() statement's lines with the machine's newline, so a literal line break inside
        /// a string would be stored as CRLF on Windows and LF elsewhere.
        /// </summary>
        private static string L(string value) => value is null
            ? "NULL"
            : "N'" + value.Replace("'", "''").Replace("\n", "' + NCHAR(10) + N'") + "'";

        private static string Values(IEnumerable<string[]> rows) =>
            string.Join(",\n", rows.Select(r => "(" + string.Join(", ", r.Select(L)) + ")"));

        // ── SiteContents replaced while unchanged: key, old Ar, old En, new Ar, new En ──
        private static readonly string[][] Replaced =
        [
            ["hero_title", "إنوفست هولدينغ", "INNOVEST HOLDING",
                "نربط رواد الأعمال بالمستثمرين في أنحاء الوطن العربي", "Connecting Entrepreneurs with Investors Across the Arab World"],
            ["hero_subtitle",
                "نبني جسور الاستثمار والابتكار من خلال منصات متكاملة للتدريب والتعليم والاستثمار والفعاليات والشراكات الاستراتيجية",
                "Building bridges between investment and innovation through integrated platforms for training and education, investment, events and strategic partnerships",
                "تمكين الابتكار. ربط رأس المال. بناء المستقبل.", "Empowering Innovation. Connecting Capital. Building the Future."],
            ["hero_badge", "مرحباً بكم في إنوفست هولدينغ", "Welcome to Innovest Holding",
                "المنصة العربية لريادة الأعمال والابتكار والاستثمار", "The Arab Platform for Entrepreneurship, Innovation & Investment"],
            ["hero_cta_primary", "اكتشف منصاتنا", "Discover Our Platforms", "استكشف منظومتنا", "Explore Our Ecosystem"],
            ["tagline", "نبني جسور الاستثمار والابتكار", "Building bridges of investment and innovation",
                "تمكين الابتكار. ربط رأس المال. بناء المستقبل.", "Empowering Innovation. Connecting Capital. Building the Future."],
            ["about_text_1",
                "إنوفست هولدينغ هي شركة رائدة في مجال الاستثمار والابتكار، تأسست برؤية طموحة لبناء منظومة متكاملة تجمع بين التدريب والتعليم، والاستثمار، والتشبيك، والفعاليات. نسعى لتمكين رواد الأعمال والمستثمرين والشركات من خلال منصات متخصصة توفر بيئة محفزة للنمو والتطور.",
                "Innovest Holding is a leading investment and innovation company, founded with an ambitious vision to build an integrated ecosystem that brings together training and education, investment, networking and events. We empower entrepreneurs, investors and companies through specialized platforms that provide an environment for growth and development.",
                "تعد إنوفست هولدينغ شركة عربية متخصصة في بناء منظومة متكاملة لريادة الأعمال والابتكار والاستثمار، تعمل على تمكين الشركات الناشئة ورواد الأعمال وربطهم بالمستثمرين، وصناديق الاستثمار، والمستثمرين الملائكيين، وجهات التمويل، وحاضنات ومسرعات الأعمال، من أجل تحويل الأفكار المبتكرة إلى شركات قادرة على النمو والمنافسة محلياً وإقليمياً وعالمياً.",
                "Innovest Holding is an Arab company specialised in building an integrated ecosystem for entrepreneurship, innovation and investment. It empowers startups and entrepreneurs and connects them with investors, investment funds, angel investors, funding bodies, and business incubators and accelerators, so that innovative ideas become companies able to grow and compete locally, regionally and globally."],
            ["about_text_2",
                "نعمل على تقديم حلول مبتكرة تلبي احتياجات السوق المتنامية، مع التركيز على الجودة والاحترافية في كل ما نقدمه من خدمات ومنصات.",
                "We deliver innovative solutions that meet the needs of a growing market, focusing on quality and professionalism in every service and platform we offer.",
                "تعتمد الشركة على منصة رقمية متكاملة، بالإضافة إلى مجموعة من المنتديات والمؤتمرات والبرامج التدريبية والفعاليات الاستثمارية التي تُقام على مدار العام في مختلف الدول العربية.",
                "The company relies on an integrated digital platform, together with forums, conferences, training programmes and investment events held throughout the year across the Arab countries."],
            ["vision_title", "الرؤية", "Our Vision", "رؤيتنا", "Our Vision"],
            ["vision_text", "أن نكون المنصة الأولى للاستثمار والابتكار في المنطقة", "To be the leading platform for investment and innovation in the region",
                "أن نصبح المنصة العربية الأولى لربط الابتكار بالاستثمار، وأن نكون المحرك الرئيسي لمنظومة ريادة الأعمال في الشرق الأوسط وشمال أفريقيا.",
                "To become the leading Arab platform connecting innovation with investment, and the main driver of the entrepreneurship ecosystem in the Middle East and North Africa."],
            ["mission_title", "الرسالة", "Our Mission", "مهمتنا", "Our Mission"],
            ["mission_text", "تمكين الأفراد والشركات عبر منصات متكاملة ومبتكرة", "Empowering individuals and companies through integrated, innovative platforms",
                "بناء بيئة عربية مستدامة تدعم الابتكار وريادة الأعمال من خلال ربط رواد الأعمال بالمستثمرين والشركاء الاستراتيجيين وجهات التمويل، وتوفير الأدوات والخدمات اللازمة لتحويل الأفكار إلى شركات ناجحة.",
                "To build a sustainable Arab environment that supports innovation and entrepreneurship by connecting entrepreneurs with investors, strategic partners and funding bodies, and by providing the tools and services needed to turn ideas into successful companies."],
            ["values_title", "القيم", "Our Values", "قيمنا الأساسية", "Our Core Values"],
            ["values_text", "الابتكار والجودة والشفافية والتميز المستمر", "Innovation, quality, transparency and continuous excellence",
                "الابتكار، الشراكة، الشفافية، الاستدامة، الاحترافية، التأثير، التمكين",
                "Innovation, Partnership, Transparency, Sustainability, Professionalism, Impact, Empowerment"],
            ["platforms_badge", "منصاتنا", "Our Platforms", "خدماتنا", "Our Services"],
            ["platforms_title", "منصاتنا {المتكاملة}", "Our {Integrated} Platforms", "خدماتنا {المتكاملة}", "Our {Integrated} Services"],
            ["platforms_subtitle",
                "نقدم مجموعة متنوعة من المنصات المتخصصة المصممة لتلبية احتياجاتكم في التدريب والاستثمار والتشبيك والفعاليات",
                "We offer a diverse range of specialized platforms designed to meet your needs in training, investment, networking and events",
                "خدمات متكاملة لرواد الأعمال والشركات الناشئة والمستثمرين والشركات الكبرى، من الفكرة حتى النمو والتوسع.",
                "Integrated services for entrepreneurs, startups, investors and large companies, from the idea to growth and expansion."],
            ["register_text",
                "انضم إلى آلاف المستثمرين ورواد الأعمال والمتدربين الذين يثقون في إنوفست هولدينغ. سجّل الآن واحصل على أحدث العروض والفعاليات.",
                "Join thousands of investors, entrepreneurs and trainees who trust Innovest Holding. Register now and receive the latest offers and events.",
                "انضم إلى مجتمع إنوفست من رواد الأعمال والمستثمرين، وسجّل الآن لتصلك أحدث الفعاليات والفرص.",
                "Join the Innovest community of entrepreneurs and investors, and register now to receive the latest events and opportunities."],
            ["footer_description",
                "إنوفست هولدينغ - نبني جسور الاستثمار والابتكار من خلال منصات متكاملة للتدريب والتعليم والاستثمار والفعاليات والشراكات الاستراتيجية.",
                "Innovest Holding - building bridges between investment and innovation through integrated platforms for training and education, investment, events and strategic partnerships.",
                "إنوفست هولدينغ هي البوابة العربية التي تربط الابتكار بريادة الأعمال والاستثمار لبناء اقتصاد عربي قائم على المعرفة والابتكار.",
                "Innovest Holding is the Arab Gateway Connecting Innovation, Entrepreneurship, Investment, and Sustainable Growth."],
        ];

        // ── Neutral keys (one value, ContentEn NULL) replaced while unchanged: key, old, new ──
        private static readonly string[][] ReplacedNeutral =
        [
            ["contact_phone", "+20 114 141 0808", "+20 01141410808"],
            ["contact_email", "info@innovestholding.com", "info@innovest-holding.com"],
            ["seo_og_image", "/assets/logo.png", "/assets/brand/og-default.jpg"],
        ];

        // ── New SiteContents keys, inserted when missing: key, Ar, En (NULL for neutral keys) ──
        private static readonly string[][] NewKeys =
        [
            ["brand_positioning", "إنوفست هولدينغ هي البوابة العربية التي تربط الابتكار بريادة الأعمال والاستثمار لبناء اقتصاد عربي قائم على المعرفة والابتكار.",
                "Innovest Holding is the Arab Gateway Connecting Innovation, Entrepreneurship, Investment, and Sustainable Growth."],
            ["header_cta_label", "انضم إلى الشبكة", "Join the Network"],
            ["header_cta_page", "network.join", null],
            ["contact_whatsapp", "201141410808", null],
            ["contact_website", "https://www.innovest-holding.com", null],
            ["founder_heading", "كلمة {المؤسس}", "Founder's {Message}"],
            ["founder_name", "الدكتور وائل محمد الشريف", "Dr. Wael Mohamed Al-Sharif"],
            ["founder_title", "رئيس مجلس الإدارة", "Chairman of the Board"],
            ["founder_message", "", null],
            ["list_page_size", "12", null],
            ["ecosystem_badge", "منظومتنا", "Our Ecosystem"],
            ["ecosystem_title", "منظومة {متكاملة} لريادة الأعمال", "An {Integrated} Entrepreneurship Ecosystem"],
            ["ecosystem_subtitle", "ثلاث منصات تربط رواد الأعمال بالمستثمرين والجمهور في أنحاء الوطن العربي.",
                "Three platforms connecting entrepreneurs with investors and audiences across the Arab world."],
            ["cta_band_title", "كن جزءاً من {منظومة إنوفست}", "Be part of the {Innovest ecosystem}"],
            ["cta_band_text", "انضم إلى شبكة المستثمرين أو سجّل في فعاليات منتدى إنوفست العربي.",
                "Join the investors network or register for the Innovest Arab Forum events."],
            ["impact_title", "الأثر {المتوقع}", "Expected {Impact}"],
            ["impact_intro", "حيث إننا نسعى لتحقيق الآتي:", "What we aim to achieve:"],
            ["section_values_intro", "", null],
            ["section_why_points_title", "لماذا {إنوفست هولدينغ}؟", "Why {Innovest Holding}?"],
            ["section_why_points_intro", "لأننا نوفر منظومة متكاملة تشمل:", "Because we offer an integrated ecosystem that includes:"],
            ["section_sectors_title", "القطاعات التي {نخدمها}", "Sectors {We Serve}"],
            ["section_sectors_intro", "", null],
            ["section_ecosystem_pillars_title", "ماذا {نقدم}", "What {We Offer}"],
            ["section_ecosystem_pillars_intro", "نعمل على بناء منظومة متكاملة تشمل:", "We are building an integrated ecosystem that includes:"],
            ["section_programs_title", "{برامجنا}", "Our {Programs}"],
            ["section_programs_intro", "برامجنا المتاحة طوال العام:", "Our programmes, available all year round:"],
            ["section_forum_components_title", "مكونات {المنتدى}", "What the {Forum} Includes"],
            ["section_forum_components_intro", "", null],
            ["section_investor_services_title", "خدمات {المستثمرين}", "Investor {Services}"],
            ["section_investor_services_intro", "نساعد المستثمرين في الوصول إلى أفضل الفرص الاستثمارية:", "We help investors reach the best investment opportunities:"],
            ["section_startup_services_title", "تطوير {الشركات الناشئة}", "Startup {Development}"],
            ["section_startup_services_intro", "", null],
            ["section_government_services_title", "خدمات {الجهات الحكومية}", "{Government} Services"],
            ["section_government_services_intro", "", null],
            ["section_university_services_title", "خدمات {الجامعات}", "{University} Services"],
            ["section_university_services_intro", "", null],
            ["section_partner_types_title", "الجهات التي {نعمل معها}", "Who We {Work With}"],
            ["section_partner_types_intro", "نعمل مع كل من الجهات الآتية:", "We work with:"],
            ["section_partners_in_success_title", "شركاء {النجاح}", "Partners in {Success}"],
            ["section_partners_in_success_intro", "", null],
            ["section_network_steps_title", "كيف تعمل {الشبكة}", "How the {Network} Works"],
            ["section_network_steps_intro", "", null],
        ];

        // ── Stats: SortOrder, old label, old value → profile label Ar/En, value, suffix, icon ──
        private const string IconUsers = "<path d=\"M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M23 21v-2a4 4 0 0 0-3-3.87\"/><path d=\"M16 3.13a4 4 0 0 1 0 7.75\"/>";
        private const string IconRocket = "<path d=\"M4.5 16.5c-1.5 1.3-2 5-2 5s3.7-.5 5-2c.7-.8.7-2.1-.1-2.9a2.2 2.2 0 0 0-2.9-.1z\"/><path d=\"m12 15-3-3a22 22 0 0 1 2-4A12.9 12.9 0 0 1 22 2c0 2.7-.8 7.5-6 11a22.4 22.4 0 0 1-4 2z\"/><path d=\"M9 12H4s.6-3 2-4c1.6-1.1 5 0 5 0\"/><path d=\"M12 15v5s3-.6 4-2c1.1-1.6 0-5 0-5\"/>";
        private const string IconDollar = "<line x1=\"12\" y1=\"1\" x2=\"12\" y2=\"23\"/><path d=\"M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6\"/>";
        private const string IconChart = "<polyline points=\"22 7 13.5 15.5 8.5 10.5 2 17\"/><polyline points=\"16 7 22 7 22 13\"/>";
        private const string IconGlobe = "<circle cx=\"12\" cy=\"12\" r=\"10\"/><line x1=\"2\" y1=\"12\" x2=\"22\" y2=\"12\"/><path d=\"M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z\"/>";
        private const string IconHandshake = "<path d=\"M11 17l2 2a1 1 0 0 0 1.4 0l4-4a1 1 0 0 0 0-1.4L13 8\"/><path d=\"M14 7 9.5 2.5 2 10l5 5\"/><path d=\"m22 10-5-5-3 3\"/><path d=\"m8 14 2 2\"/>";
        private const string IconCalendar = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\" ry=\"2\"/><line x1=\"16\" y1=\"2\" x2=\"16\" y2=\"6\"/><line x1=\"8\" y1=\"2\" x2=\"8\" y2=\"6\"/><line x1=\"3\" y1=\"10\" x2=\"21\" y2=\"10\"/>";
        private const string IconBook = "<path d=\"M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z\"/><path d=\"M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z\"/>";

        /// <summary>Old row (SortOrder, LabelAr, Value, LabelEn, ShortAr, ShortEn, Icon) → new (LabelAr, LabelEn, Value, Suffix, Icon).</summary>
        private static readonly (int Order, string OldAr, int OldValue, string OldEn, string OldShortAr, string OldShortEn, string OldIcon,
            string Ar, string En, int Value, string Suffix, string Icon)[] StatRows =
        [
            (1, "شريك استراتيجي", 150, "Strategic partners", null, null, IconUsers, "رائد أعمال", "Entrepreneurs", 100000, "+", IconRocket),
            (2, "مستثمر نشط", 2500, "Active investors", "مستثمر", "Investors", IconDollar, "شركة ناشئة", "Startups", 10000, "+", IconChart),
            (3, "فعالية سنوياً", 80, "Events per year", null, null, IconCalendar, "مستثمر", "Investors", 5000, "+", IconUsers),
            (4, "متدرب ومستفيد", 5000, "Trainees and beneficiaries", null, null, IconBook, "صندوق استثمار", "Investment funds", 500, "+", IconDollar),
        ];

        /// <summary>The suffix every seed figure had.</summary>
        private const string OldStatSuffix = "+";

        private static readonly (int Order, string Ar, string En, int Value, string Suffix, string Icon)[] ExtraStats =
        [
            (5, "دولة عربية", "Arab countries", 22, "", IconGlobe),
            (6, "شريك استراتيجي", "Strategic partners", 100, "+", IconHandshake),
        ];

        // ── Platforms → the profile's services 01–06 ──
        private const string IconLayers = "<polygon points=\"12 2 2 7 12 12 22 7 12 2\"/><polyline points=\"2 17 12 22 22 17\"/><polyline points=\"2 12 12 17 22 12\"/>";
        private const string IconBriefcase = "<rect x=\"2\" y=\"7\" width=\"20\" height=\"14\" rx=\"2\"/><path d=\"M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16\"/>";
        private const string IconCpu = "<rect x=\"4\" y=\"4\" width=\"16\" height=\"16\" rx=\"2\"/><rect x=\"9\" y=\"9\" width=\"6\" height=\"6\"/><path d=\"M9 1v3M15 1v3M9 20v3M15 20v3M20 9h3M20 14h3M1 9h3M1 14h3\"/>";
        private const string IconMic = "<rect x=\"9\" y=\"2\" width=\"6\" height=\"12\" rx=\"3\"/><path d=\"M19 10v1a7 7 0 0 1-14 0v-1\"/><line x1=\"12\" y1=\"18\" x2=\"12\" y2=\"22\"/>";
        private const string OldIconMedia = "<polygon points=\"23 7 16 12 23 17 23 7\"/><rect x=\"1\" y=\"5\" width=\"15\" height=\"14\" rx=\"2\" ry=\"2\"/>";
        private const string OldIconTrophy = "<path d=\"M6 9H4.5a2.5 2.5 0 0 1 0-5C7 4 9 7 12 7s5-3 7.5-3a2.5 2.5 0 0 1 0 5H18\"/><path d=\"M12 7v10\"/><path d=\"M6 21h12a2 2 0 0 0 2-2v-6a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v6a2 2 0 0 0 2 2z\"/>";

        /// <summary>
        /// Old (TitleAr, DescriptionAr, TitleEn, DescriptionEn, ShortAr, ShortEn, ShowInFooter, Icon) →
        /// new (TitleAr, TitleEn, DescriptionAr, DescriptionEn, Icon). Descriptions are one bullet per line.
        /// </summary>
        private static readonly string[][] PlatformRows =
        [
            ["التدريب والتعليم", "برامج تدريبية متخصصة وورش عمل احترافية مصممة لتطوير المهارات وبناء القدرات في مجالات الأعمال والتكنولوجيا والقيادة.",
                "Training & Education", "Specialized training programs and professional workshops designed to develop skills and build capabilities in business, technology and leadership.",
                null, null, "1", IconBook,
                "تطوير الشركات الناشئة", "Startup Development",
                "احتضان الأفكار الناشئة\nتطوير نماذج الأعمال\nإعداد العرض التقديمي (Pitch Deck)\nدراسة الجدوى\nالإرشاد والتوجيه (Mentorship & Business Coaching)",
                "Incubating early-stage ideas\nBusiness model development\nPitch deck preparation\nFeasibility studies\nMentorship and business coaching", IconRocket],
            ["منصة المستثمرين والتشبيك", "منصة فريدة تربط بين المستثمرين ورواد الأعمال والشركات، وتوفر فرص تشبيك استراتيجية وشراكات مثمرة.",
                "Investors & Networking Platform", "A unique platform connecting investors, entrepreneurs and companies, creating strategic networking opportunities and fruitful partnerships.",
                "المستثمرين والتشبيك", "Investors & Networking", "1", IconUsers,
                "خدمات الاستثمار", "Investment Services",
                "جذب المستثمرين\nالتشبيك مع المستثمرين الملائكيين\nالتشبيك مع صناديق الاستثمار\nتجهيز الشركات للاستثمار\nتقييم الشركات الناشئة\nإدارة جولات الاستثمار",
                "Attracting investors\nConnecting with angel investors\nConnecting with investment funds\nPreparing companies for investment\nStartup valuation\nManaging investment rounds", IconChart],
            ["الإعلام والمحتوى الرقمي", "إنتاج محتوى رقمي إبداعي ومتميز، يشمل الفيديو والبودكاست والمقالات المتخصصة في عالم الأعمال والاستثمار.",
                "Media & Digital Content", "Creative, high-quality digital content including video, podcasts and specialized articles on business and investment.",
                "الإعلام الرقمي", "Digital Media", "1", OldIconMedia,
                "تطوير الأعمال", "Business Development",
                "الاستشارات\nتطوير الأعمال\nالتوسع الإقليمي\nدخول الأسواق\nتطوير الاستراتيجيات",
                "Consulting\nBusiness development\nRegional expansion\nMarket entry\nStrategy development", IconBriefcase],
            ["المسابقات", "مسابقات وتحديات مبتكرة تستهدف رواد الأعمال والمبدعين، مع جوائز قيمة وفرص حقيقية لتحويل الأفكار إلى مشاريع ناجحة.",
                "Competitions", "Innovative competitions and challenges for entrepreneurs and creators, with valuable prizes and real opportunities to turn ideas into successful projects.",
                null, null, "1", OldIconTrophy,
                "الابتكار المؤسسي", "Corporate Innovation",
                "دعم الشركات الكبرى في الابتكار المؤسسي\nالتحول الرقمي\nالابتكار المفتوح\nالتعاون مع الشركات الناشئة",
                "Supporting large companies in corporate innovation\nDigital transformation\nOpen innovation\nCollaboration with startups", IconLayers],
            ["الفعاليات والمعارض", "تنظيم وإدارة فعاليات ومعارض دولية ومحلية تجمع بين أصحاب القرار والمستثمرين ورواد الأعمال في بيئة محفزة.",
                "Events & Exhibitions", "Organizing and managing international and local events and exhibitions that bring decision makers, investors and entrepreneurs together in an inspiring environment.",
                null, null, "0", IconCalendar,
                "منصة إنوفست", "Innovest Platform",
                "بوابة رواد الأعمال\nبوابة المستثمرين\nقاعدة بيانات الشركات الناشئة\nفرص التمويل\nإدارة الفعاليات\nنظام التوفيق (Matchmaking)\nالاستشارات والتوجيه\nأكاديمية التعلم\nمجتمع إنوفست لرواد الأعمال\nسوق الخدمات الإلكتروني",
                "Entrepreneur Portal\nInvestor Portal\nStartup Database\nFunding Opportunities\nEvent Management\nMatchmaking System\nMentorship Platform\nLearning Academy\nInnovest Community\nServices Marketplace", IconCpu],
            ["الشراكات الاستراتيجية و B2B", "بناء شراكات استراتيجية مع مؤسسات وشركات محلية ودولية، وتوفير حلول أعمال متكاملة لتعزيز النمو المشترك.",
                "Strategic Partnerships & B2B", "Building strategic partnerships with local and international institutions and companies, and providing integrated business solutions for shared growth.",
                "الشراكات و B2B", "Partnerships & B2B", "1", IconGlobe,
                "منتدى إنوفست العربي", "Innovest Arab Forum",
                "معرض إنوفست للشركات الناشئة\nمنتدى الابتكار والاستثمار العربي\nملتقى الذكاء الاصطناعي\nمسابقة العروض التقديمية\nاجتماعات المستثمرين\nالتشبيك بين الشركات\nجلسات حكومية\nجوائز الابتكار",
                "Innovest Startup Expo\nInvestment Forum Event\nAI Summit\nPitch Competition\nInvestment Meetings\nBusiness Matching\nGovernment Sessions\nInnovation Awards", IconMic],
        ];

        private const string LinktreeUrl = "https://linktr.ee/Innovest.HoldingOfficial";

        // ── The eight anchor items of the one-page menu (Href, seed Arabic title): replaced by the generated menu ──
        private static readonly string[][] AnchorNav =
        [
            ["#hero", "الرئيسية"], ["#about", "من نحن"], ["#platforms", "منصاتنا"], ["#news", "الأخبار"], ["#events", "الفعاليات"],
            ["#partners", "الشركاء والرعاة"], ["#register", "التسجيل السريع"], ["#contact", "تواصل معنا"],
        ];

        /// <summary>
        /// NULL-safe binary equality of a text column with a value of the VALUES list (cast, so a list column that is NULL in
        /// every row still compares as text).
        /// </summary>
        private static string Same(string column, string value) =>
            $"({column} = CAST({value} AS nvarchar(max)) {Bin} OR ({column} IS NULL AND {value} IS NULL))";

        /// <summary>The column takes the new value only while it still holds its own old value; otherwise it is kept.</summary>
        private static string Guarded(string column, string oldValue, string newValue) =>
            $"{column} = CASE WHEN {Same(column, oldValue)} THEN {newValue} ELSE {column} END";

        protected override void Up(MigrationBuilder mb)
        {
            // Every column is guarded on its own: a row is found by its identity columns (still the old seed values), and
            // each other column is written only while it still equals ITS old seed value (NULL-safe, binary). A column the
            // admin edited keeps the admin's value.

            // 1. SiteContents: the Arabic identifies the row; the English is replaced only while it is still the old seed English.
            mb.Sql("UPDATE s SET ContentAr = v.NA, " + Guarded("s.ContentEn", "v.OE", "v.NE") +
                   ", UpdatedAt = SYSUTCDATETIME() FROM SiteContents s JOIN (VALUES\n" + Values(Replaced) +
                   $"\n) v(K, OA, OE, NA, NE) ON s.SectionKey = v.K AND s.ContentAr = v.OA {Bin};");
            mb.Sql("UPDATE s SET ContentAr = v.NA, UpdatedAt = SYSUTCDATETIME() FROM SiteContents s JOIN (VALUES\n" + Values(ReplacedNeutral) +
                   $"\n) v(K, OA, NA) ON s.SectionKey = v.K AND s.ContentAr = v.OA {Bin};");

            // 2. New keys, inserted when missing (never an unconditional INSERT: IX_SiteContents_SectionKey is unique).
            mb.Sql("INSERT INTO SiteContents (SectionKey, ContentAr, ContentEn, UpdatedAt)\n" +
                   "SELECT v.K, v.Ar, v.En, SYSUTCDATETIME() FROM (VALUES\n" + Values(NewKeys) +
                   "\n) v(K, Ar, En) WHERE NOT EXISTS (SELECT 1 FROM SiteContents s WHERE s.SectionKey = v.K);");

            // 3. Stats: a seed row (order, Arabic label and value unchanged) becomes a profile figure; its English label,
            //    short labels, suffix and icon are each replaced only while still the seed value. The two extra figures
            //    are added only when the table then holds exactly the four profile rows.
            mb.Sql("UPDATE s SET LabelAr = v.NA, [Value] = v.NV, " +
                   string.Join(", ",
                       Guarded("s.LabelEn", "v.OE", "v.NE"),
                       Guarded("s.ShortLabelAr", "v.OSA", "NULL"),
                       Guarded("s.ShortLabelEn", "v.OSE", "NULL"),
                       Guarded("s.Suffix", "v.OS", "v.NS"),
                       Guarded("s.IconSvg", "v.OI", "v.NI")) +
                   " FROM Stats s JOIN (VALUES\n" + string.Join(",\n", StatRows.Select(r =>
                       $"({r.Order}, {L(r.OldAr)}, {r.OldValue}, {L(r.OldEn)}, {L(r.OldShortAr)}, {L(r.OldShortEn)}, {L(OldStatSuffix)}, {L(r.OldIcon)}, " +
                       $"{L(r.Ar)}, {L(r.En)}, {r.Value}, {L(r.Suffix)}, {L(r.Icon)})")) +
                   $"\n) v(O, OA, OV, OE, OSA, OSE, OS, OI, NA, NE, NV, NS, NI) ON s.SortOrder = v.O AND s.LabelAr = v.OA {Bin} AND s.[Value] = v.OV;");
            var profile4 = string.Join(", ", StatRows.Select(r => $"({L(r.Ar)}, {r.Value})"));
            mb.Sql("IF (SELECT COUNT(*) FROM Stats) = 4 AND (SELECT COUNT(*) FROM Stats s JOIN (VALUES " + profile4 +
                   $") v(A, V) ON s.LabelAr = v.A {Bin} AND s.[Value] = v.V) = 4\n" +
                   "INSERT INTO Stats (LabelAr, LabelEn, [Value], Suffix, IconSvg, SortOrder) VALUES " +
                   string.Join(", ", ExtraStats.Select(r => $"({L(r.Ar)}, {L(r.En)}, {r.Value}, {L(r.Suffix)}, {L(r.Icon)}, {r.Order})")) + ";");

            // 4. Platforms → services 01–06: a row whose Arabic title AND description are still the seed text; its English
            //    title and description, short titles, footer flag and icon are each replaced only while still the seed value.
            mb.Sql("UPDATE p SET TitleAr = v.NTA, DescriptionAr = v.NDA, " +
                   string.Join(", ",
                       Guarded("p.TitleEn", "v.OTE", "v.NTE"),
                       Guarded("p.DescriptionEn", "v.ODE", "v.NDE"),
                       Guarded("p.ShortTitleAr", "v.OSA", "NULL"),
                       Guarded("p.ShortTitleEn", "v.OSE", "NULL"),
                       "p.ShowInFooter = CASE WHEN p.ShowInFooter = v.OFT THEN 1 ELSE p.ShowInFooter END",
                       Guarded("p.IconSvg", "v.OI", "v.NI")) +
                   " FROM Platforms p JOIN (VALUES\n" +
                   string.Join(",\n", PlatformRows.Select(r =>
                       $"({L(r[0])}, {L(r[1])}, {L(r[2])}, {L(r[3])}, {L(r[4])}, {L(r[5])}, {r[6]}, {L(r[7])}, " +
                       $"{L(r[8])}, {L(r[9])}, {L(r[10])}, {L(r[11])}, {L(r[12])})")) +
                   $"\n) v(OT, OD, OTE, ODE, OSA, OSE, OFT, OI, NTA, NTE, NDA, NDE, NI) ON p.TitleAr = v.OT {Bin} AND p.DescriptionAr = v.OD {Bin};");

            // 5. Social links: the profile's networks are added when missing; only an empty linktree address (the value
            //    the insert gives it) is set. Handles without a confirmed URL stay empty and are not rendered.
            mb.Sql("""
                INSERT INTO SocialLinks (Network, Url, SortOrder, IsActive, CreatedAt)
                SELECT v.N, NULL, v.S, 1, SYSUTCDATETIME() FROM (VALUES (N'facebook', 5), (N'tiktok', 6), (N'linktree', 7)) v(N, S)
                WHERE NOT EXISTS (SELECT 1 FROM SocialLinks s WHERE s.Network = v.N);
                """);
            mb.Sql($"UPDATE SocialLinks SET Url = {L(LinktreeUrl)} WHERE Network = N'linktree' AND Url IS NULL;");

            // 6. The one-page anchor items of the menu are deactivated (not deleted) while still active: the header is now
            //    generated from the route table. Admin-made items keep rendering as extra links.
            mb.Sql("UPDATE n SET IsActive = 0 FROM NavItems n JOIN (VALUES\n" + Values(AnchorNav) +
                   $"\n) v(H, T) ON n.Href = v.H {Bin} AND n.TitleAr = v.T {Bin} WHERE n.IsActive = 1;");
        }

        /// <summary>Mirror of Up: each column goes back only where it still equals what Up wrote.</summary>
        protected override void Down(MigrationBuilder mb)
        {
            mb.Sql("UPDATE n SET IsActive = 1 FROM NavItems n JOIN (VALUES\n" + Values(AnchorNav) +
                   $"\n) v(H, T) ON n.Href = v.H {Bin} AND n.TitleAr = v.T {Bin} WHERE n.IsActive = 0;");

            mb.Sql($"UPDATE SocialLinks SET Url = NULL WHERE Network = N'linktree' AND Url = {L(LinktreeUrl)} {Bin};");
            mb.Sql("DELETE FROM SocialLinks WHERE Network IN (N'facebook', N'tiktok', N'linktree') AND Url IS NULL;");

            mb.Sql("UPDATE p SET TitleAr = v.OT, DescriptionAr = v.OD, " +
                   string.Join(", ",
                       Guarded("p.TitleEn", "v.NTE", "v.OTE"),
                       Guarded("p.DescriptionEn", "v.NDE", "v.ODE"),
                       Guarded("p.ShortTitleAr", "NULL", "v.OSA"),
                       Guarded("p.ShortTitleEn", "NULL", "v.OSE"),
                       "p.ShowInFooter = CASE WHEN p.ShowInFooter = 1 THEN v.OFT ELSE p.ShowInFooter END",
                       Guarded("p.IconSvg", "v.NI", "v.OI")) +
                   " FROM Platforms p JOIN (VALUES\n" +
                   string.Join(",\n", PlatformRows.Select(r =>
                       $"({L(r[8])}, {L(r[9])}, {L(r[10])}, {L(r[11])}, {L(r[12])}, {L(r[0])}, {L(r[2])}, {L(r[1])}, {L(r[3])}, {L(r[4])}, {L(r[5])}, {r[6]}, {L(r[7])})")) +
                   $"\n) v(NTA, NTE, NDA, NDE, NI, OT, OTE, OD, ODE, OSA, OSE, OFT, OI) ON p.TitleAr = v.NTA {Bin} AND p.DescriptionAr = v.NDA {Bin};");

            mb.Sql("DELETE s FROM Stats s JOIN (VALUES " +
                   string.Join(", ", ExtraStats.Select(r => $"({L(r.Ar)}, {r.Value}, {r.Order})")) +
                   $") v(A, V, O) ON s.LabelAr = v.A {Bin} AND s.[Value] = v.V AND s.SortOrder = v.O;");
            mb.Sql("UPDATE s SET LabelAr = v.OA, [Value] = v.OV, " +
                   string.Join(", ",
                       Guarded("s.LabelEn", "v.NE", "v.OE"),
                       Guarded("s.ShortLabelAr", "NULL", "v.OSA"),
                       Guarded("s.ShortLabelEn", "NULL", "v.OSE"),
                       Guarded("s.Suffix", "v.NS", "v.OS"),
                       Guarded("s.IconSvg", "v.NI", "v.OI")) +
                   " FROM Stats s JOIN (VALUES\n" + string.Join(",\n", StatRows.Select(r =>
                       $"({r.Order}, {L(r.Ar)}, {r.Value}, {L(r.En)}, {L(r.Suffix)}, {L(r.Icon)}, {L(r.OldAr)}, {r.OldValue}, {L(r.OldEn)}, {L(r.OldShortAr)}, {L(r.OldShortEn)}, {L(OldStatSuffix)}, {L(r.OldIcon)})")) +
                   $"\n) v(O, NA, NV, NE, NS, NI, OA, OV, OE, OSA, OSE, OS, OI) ON s.SortOrder = v.O AND s.LabelAr = v.NA {Bin} AND s.[Value] = v.NV;");

            // Keys inserted by Up are removed only while unchanged since the insert.
            mb.Sql("DELETE s FROM SiteContents s JOIN (VALUES\n" + Values(NewKeys) +
                   $"\n) v(K, Ar, En) ON s.SectionKey = v.K AND s.ContentAr = v.Ar {Bin} AND " + Same("s.ContentEn", "v.En") + ";");

            mb.Sql("UPDATE s SET ContentAr = v.OA FROM SiteContents s JOIN (VALUES\n" + Values(ReplacedNeutral) +
                   $"\n) v(K, OA, NA) ON s.SectionKey = v.K AND s.ContentAr = v.NA {Bin};");
            mb.Sql("UPDATE s SET ContentAr = v.OA, " + Guarded("s.ContentEn", "v.NE", "v.OE") +
                   " FROM SiteContents s JOIN (VALUES\n" + Values(Replaced) +
                   $"\n) v(K, OA, OE, NA, NE) ON s.SectionKey = v.K AND s.ContentAr = v.NA {Bin};");
        }
    }
}
