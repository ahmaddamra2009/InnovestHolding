using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovestHolding.Migrations
{
    /// <summary>
    /// The data steps of AddForumContent. Every literal is FROZEN here, copied from FORUM-CONTENT.md (Layla's normalised
    /// `ar` / `en` fields, never `ar_printed`), with the CTO-accepted defaults and Anas's answers of 2026-10-23: the typos of
    /// FORUM-CONTENT §12 corrected, "Qadroon", the two "Sheikh … bin Hamdan" bullets merged, Western digits for figures, the
    /// 5th edition without date or place, «420 مليون» without a currency, the 4 ministries hidden, all 9 programme events
    /// visible (June–September as past events), the contact details as printed (the handle as text). "[?]" values are
    /// empty. A test pins: no literal holds an old name, every literal fits its column, the registries equal these lists.
    ///
    /// Guard polarity (ANTIGRAVITY §3): every step INSERTs a row only where its identity is missing (binary collation on
    /// text), so a second run, or a row an admin already has, makes the step do nothing. The only change to existing rows
    /// is the forum_components swap, applied only while the group holds exactly the 8 untouched AddSiteStructure rows.
    /// Down removes a row only while it still equals what Up wrote.
    /// </summary>
    public partial class AddForumContent
    {
        private const string Bin = "COLLATE Latin1_General_100_BIN2";

        /// <summary>N'…' with quotes doubled; NULL for null. Multi-line values are built with NCHAR(10) (see AddAdminControl.L).</summary>
        private static string L(string value)
        {
            if (value is null) return "NULL";
            var lines = value.Split('\n').Select(line => "N'" + line.Replace("'", "''") + "'");
            return value.Contains('\n')
                ? "(CAST(N'' AS nvarchar(max)) + " + string.Join(" + NCHAR(10) + ", lines) + ")"
                : "N'" + value.Replace("'", "''") + "'";
        }

        /// <summary>A text equal to <paramref name="value"/> (binary), NULL-safe.</summary>
        private static string Eq(string column, string value) =>
            value is null ? $"{column} IS NULL" : $"{column} {Bin} = {L(value)}";

        /// <summary>A statement that names a table or column created by this migration runs through EXEC (a generated
        /// script compiles a whole batch before its ALTER TABLE has run).</summary>
        private static string Exec(string sql) => "EXEC(N'" + sql.Replace("'", "''") + "');";

        // ── SiteContents: key, Arabic, English (NULL = neutral or no English) ──

        public const string AboutTextAr =
            "تنطلق النسخة الخامسة من منتدى إنوفست في ظل تصاعد أهمية الابتكار وريادة الأعمال كركيزة أساسية للنهوض الاقتصادي وتحقيق التنمية المستدامة في العالم العربي.\n" +
            "\n" +
            "ويواصل المنتدى مسيرته نحو تمكين الشباب، وتقديم منصة فعالة لدعم المشروعات الناشئة، من خلال حزمة متكاملة من البرامج والفعاليات التي تجمع بين التدريب، التشبيك، المعارض، التحكيم، والتحفيز على الاستثمار.\n" +
            "\n" +
            "شباب قادرون قررت إطلاق النسخ القادمة من المؤتمر تحت اسم: (Innovest Forum – منتدى إنوفست)\n" +
            "\n" +
            "ليكون المظلة الكبرى التي تضم كل النسخ السابقة والمستقبلية من المؤتمر، ويستهدف هذا الحدث المتميز توسيع نطاقه ليشمل دولًا عربية أخرى، مُقدِّمًا فرصًا أوسع في مجال ريادة الأعمال والاستثمار على المستوى العربي والشرق الأوسط.";

        public const string AboutTextEn =
            "The fifth edition of the Innovest Forum is launched at a time when innovation and entrepreneurship are increasingly recognised as a fundamental pillar of economic advancement and sustainable development in the Arab world.\n" +
            "\n" +
            "The Forum continues its journey towards empowering young people and providing an effective platform to support emerging ventures, through an integrated package of programmes and events that combine training, networking, exhibitions, judging and the promotion of investment.\n" +
            "\n" +
            "Qadroon Youth decided to launch the coming editions of the conference under the name: (Innovest Forum – منتدى إنوفست)\n" +
            "\n" +
            "It is to be the overarching umbrella for all past and future editions of the conference. This distinguished event aims to expand its reach to further Arab countries, offering wider opportunities in entrepreneurship and investment across the Arab region and the Middle East.";

        public const string PositioningTextAr =
            "ليس مجرد مؤتمر، بل هو منصة للابتكار والاستثمار تهدف إلى تعزيز التعاون بين رواد الأعمال والمستثمرين في العالم العربي، وتوفير بيئة مثالية لطرح الأفكار، تبادل الخبرات، واكتشاف فرص جديدة.\n" +
            "\n" +
            "تُمثّل إنوفست منصة تكاملية تربط بين الدولة، ورواد الأعمال، والمستثمرين لخلق اقتصاد قائم على الابتكار وتحقيق نمو مستدام.";

        public const string PositioningTextEn =
            "More than a conference, it is a platform for innovation and investment that aims to strengthen cooperation between entrepreneurs and investors in the Arab world, and to provide an ideal environment for presenting ideas, exchanging expertise and discovering new opportunities.\n" +
            "\n" +
            "Innovest is an integrated platform linking the State, entrepreneurs and investors to create an innovation-driven economy and achieve sustainable growth.";

        public static readonly string[][] NewKeys =
        [
            ["forum_tagline", "نحو منظومة عربية مبتكرة للاستثمار والفرص الواعدة.", "Towards an innovative Arab ecosystem for investment and promising opportunities."],
            ["forum_edition_label", "النسخة الخامسة", "Fifth Edition"],
            ["forum_about_lead", "شباب قادرون وإنوفست القابضة تطلقان منتدى إنوفست", "Qadroon Youth and Innovest Holding launch the Innovest Forum"],
            ["forum_about_text", AboutTextAr, AboutTextEn],
            ["forum_positioning_title", "منتدى إنوفست", "Innovest Forum"],
            ["forum_positioning_text", PositioningTextAr, PositioningTextEn],
            ["forum_strategic_link_title", "الربط الاستراتيجي", "Strategic Link"],
            ["forum_strategic_link_text", "إنوفست يربط بين:\nالأفكار + المواهب + التمويل\nشركات ناجحة واستثمارات حقيقية",
                "Innovest connects:\nIdeas + Talent + Funding\nSuccessful companies and real investments"],
            ["forum_editions_title", "النسخ السابقة", "Past Editions"],
            ["forum_editions_more_label", "عرض كل النسخ", "View all editions"],
            ["forum_patronage_title", "الجهات المنظمة", "Organising Bodies"],
            ["forum_contact_title", "طرق التواصل", "Contact Us"],
            ["forum_contact_phone_label", "للتواصل والحجز", "Contact and Booking"],
            ["forum_contact_phone", "+20 15 1520 4475", null],
            ["forum_contact_social_label", "وسائل التواصل الاجتماعي للمنتدى", "Forum Social Media"],
            ["forum_contact_social", "innovest1", null],
            ["forum_contact_website_label", "الموقع الرسمي للمنتدى", "Official Forum Website"],
            ["forum_contact_website", "https://www.innovest-arab.com", null],
            ["forum_contact_address", "55 شارع جمال زين، المنطقة الأولى، مدينة نصر، القاهرة، مصر.", "55 Gamal Zein St., First District, Nasr City, Cairo, Egypt."],
            ["forum_editions_limit", "3", null],

            // Headings, intros and notes of the forum lists (ContentItemGroups).
            ["section_forum_figures_title", "منظومة إنوفست للابتكار", "Innovest Innovation Ecosystem"],
            ["section_forum_figures_intro", "منظومة إنوفست للابتكار والاستثمار ليست مجرد منتدى، بل منظومة اقتصادية متكاملة لبناء بيئة مستدامة للابتكار والاستثمار في العالم العربي.",
                "The Innovest Innovation and Investment Ecosystem is more than a forum: it is an integrated economic ecosystem for building a sustainable environment for innovation and investment in the Arab world."],
            ["section_forum_audiences_title", "التأثير على الأطراف الرئيسية", "Multi-Stakeholder Impact"],
            ["section_forum_audiences_intro", "", null],
            ["section_forum_pillars_title", "القطاعات الرئيسية", "Core Pillars"],
            ["section_forum_pillars_intro",
                "",
                null],
            ["section_forum_goals_title", "أهداف النسخة الخامسة", "Goals of the Fifth Edition"],
            ["section_forum_goals_intro", "", null],
            ["section_forum_goals_note",
                "يعكس المنتدى التزامه بتعزيز الابتكار والاستثمار في المنطقة العربية، ويوفر فرصًا ممتازة للتفاعل والتعاون من أجل تحقيق التنمية الاقتصادية والاجتماعية المستدامة.",
                "The Forum reflects its commitment to promoting innovation and investment in the Arab region, and offers excellent opportunities for interaction and cooperation towards sustainable economic and social development."],
            ["section_forum_sectors_title", "المحاور والقطاعات", "Focus Areas and Sectors"],
            ["section_forum_sectors_intro", "", null],
            ["section_forum_sectors_note",
                "تمثل هذه المحاور الاتجاهات الرئيسية التي تركز عليها النسخة الخامسة من المنتدى، استجابةً للتحديات التنموية واحتياجات المجتمعات العربية، بهدف تحفيز الحلول الابتكارية والمشروعات المستدامة.",
                "These axes represent the main directions of the Forum's fifth edition, responding to the development challenges and needs of Arab societies, with the aim of stimulating innovative solutions and sustainable projects."],
            ["section_forum_components_note",
                "ويشمل المنتدى الملتقى العربي للذكاء الاصطناعي ومسابقة شباب الجامعات التكنولوجية.",
                "The Forum also includes the Arab Artificial Intelligence Forum and the Technological Universities Youth Competition."],
        ];

        // ── Pages: the editions page (a List page under the forum, after its siblings) ──

        public static readonly (string Column, string Value)[] EditionsPageRow =
        [
            ("TitleAr", "النسخ السابقة"),
            ("TitleEn", "Past Editions"),
            ("SummaryAr", "محطات منتدى إنوفست منذ نسخته الأولى عام 2023."),
            ("SummaryEn", "Milestones of the Innovest Forum since its first edition in 2023."),
            ("IntroAr", "صور ومحطات من النسخ السابقة لمنتدى إنوفست والفعاليات المصاحبة لها."),
            ("IntroEn", "Photos and highlights from past editions of the Innovest Forum and their side events."),
            ("BodyAr", ""),
            ("BodyEn", null),
            ("EmptyTextAr", "ستُنشر النسخ السابقة قريبًا."),
            ("EmptyTextEn", "Past editions will be published soon."),
            ("MetaTitleAr", "النسخ السابقة لمنتدى إنوفست"),
            ("MetaTitleEn", "Past Editions of the Innovest Forum"),
            ("MetaDescriptionAr", "تعرّف على النسخ السابقة لمنتدى إنوفست منذ 2023: مؤتمرات ريادة الأعمال والاستثمار، وملتقيات الجامعات، وقمة الشباب العربي في الأردن."),
            ("MetaDescriptionEn", "Explore past editions of the Innovest Forum since 2023: entrepreneurship and investment conferences, university forums and the Arab youth summit in Jordan."),
        ];

        public const string EditionsPageKey = "forum.editions";

        /// <summary>The forum's existing children (the editions page is ordered after them).</summary>
        private const string ForumChildren = "'forum.events', 'forum.speakers', 'forum.partners', 'forum.registration'";

        // ── PageSections: (page, section, SortOrder) — interleaved with C5's 10/20/30/40 ──

        public static readonly (string Page, string Section, int Order)[] SectionRows =
        [
            ("forum", "about", 5),
            ("forum", "figures", 12),
            ("forum", "audiences", 14),
            ("forum", "pillars", 15),
            ("forum", "goals", 16),
            ("forum", "sectors", 18),
            ("forum", "editions", 32),
            ("forum", "patronage", 34),
            ("forum", "contact", 36),
            ("forum.editions", "list", 10),
            ("forum.registration", "contact", 20),
        ];

        // ── ContentItems ──

        /// <param name="Icon">A key of Infrastructure/Content/Icons, or null.</param>
        public sealed record Item(string TextAr, string TextEn, string Icon, string DescriptionAr = null, string DescriptionEn = null);

        /// <summary>The new groups, each inserted only while the table holds no row of that group.</summary>
        public static readonly (string Group, Item[] Rows)[] ItemGroups =
        [
            ("forum_figures",
            [
                new("2000+", "2000+", null, "متدرب", "trainees"),
                new("459", "459", null, "مستفيد من المسابقات", "competition beneficiaries"),
                new("300+", "300+", null, "استشارة ودراسة جدوى", "consultations and feasibility studies"),
                new("58+", "58+", null, "شركة مدعومة", "companies supported"),
                new("420 مليون", "420 million", null, "قيمة الشراكات", "in partnerships"),
                new("50", "50", null, "شركة ضمن الاستثمار القادم", "companies in the next investment"),
            ]),
            ("forum_audiences",
            [
                new("أولًا: الدولة", "For the State", "building",
                    "دعم التحول نحو اقتصاد المعرفة\nتعزيز بيئة الابتكار وريادة الأعمال\nجذب الاستثمارات المحلية والدولية\nخلق فرص عمل مستدامة للشباب\nدعم خطط التنمية الاقتصادية",
                    "Supporting the transition to a knowledge economy\nStrengthening the innovation and entrepreneurship environment\nAttracting local and international investment\nCreating sustainable jobs for young people\nSupporting economic development plans"),
                new("ثانيًا: رواد الأعمال", "For Entrepreneurs", "rocket",
                    "تحويل الأفكار إلى مشاريع قابلة للتنفيذ\nتوفير التدريب والتأهيل (Business + AI)\nالوصول إلى مستثمرين وشركاء\nاحتضان وتسريع نمو الشركات الناشئة\nفرص ظهور إعلامي وتسويقي",
                    "Turning ideas into viable projects\nTraining and capacity building (Business + AI)\nAccess to investors and partners\nIncubating startups and accelerating their growth\nMedia and marketing exposure"),
                new("ثالثًا: المستثمرون", "For Investors", "coins",
                    "الوصول إلى مشاريع جاهزة للاستثمار (Deal Flow قوي)\nتقليل مخاطر الاستثمار عبر التقييم والتأهيل\nفرص استثمار في قطاعات مبتكرة (Tech / AI)\nبناء شراكات استراتيجية مربحة\nتحقيق عوائد مالية مستدامة",
                    "Access to investment-ready projects (strong Deal Flow)\nLower investment risk through assessment and preparation\nInvestment opportunities in innovative sectors (Tech / AI)\nBuilding profitable strategic partnerships\nAchieving sustainable financial returns"),
            ]),
            ("forum_pillars",
            [
                new("التدريب والاستشارات", "Training & Consulting", "book",
                    "+2000 متدرب\n459 مستفيد من المسابقات\n+300 استشارة ودراسة جدوى\nحاضنة أعمال إنوفست",
                    "2000+ trainees\n459 competition beneficiaries\n300+ consultations and feasibility studies\nInnovest Business Incubator"),
                new("الإعلام والتأثير", "Media & Influence", "mic",
                    "منصة إلكترونية عالمية\nبودكاست متخصص\nArab Innovest TV",
                    "A global online platform\nA specialised podcast\nArab Innovest TV"),
                new("التكنولوجيا والابتكار", "Technology & Innovation", "idea",
                    "منصة إنوفست الرقمية\nAI Youth Hub (مركز تأهيل شباب الذكاء الاصطناعي)\nحلول رقمية لدعم الشركات الناشئة\nبناء منتجات تقنية قابلة للاستثمار",
                    "The Innovest digital platform\nAI Youth Hub (an AI training centre for young people)\nDigital solutions supporting startups\nBuilding investable tech products"),
                new("الفعاليات والمسابقات والجوائز", "Events, Competitions & Awards", "award",
                    "مؤتمرات عربية كبرى\nمعارض للشركات الناشئة\nملتقيات جامعية\nقمم إقليمية\nInnovest Awards (جوائز الابتكار والاستثمار العربي)\n" +
                    "تكريم أفضل المشاريع والشركات ورواد الأعمال وجائزة الشيخ/ محمد بن حمدان الجائزة العربية لريادة الأعمال والاستثمار",
                    "Major Arab conferences\nStartup exhibitions\nUniversity forums\nRegional summits\nInnovest Awards (Arab Innovation and Investment Awards)\n" +
                    "Honouring the best projects, companies and entrepreneurs, and the Sheikh Mohammed bin Hamdan Arab Award for Entrepreneurship and Investment"),
                new("الاستثمار والشراكات", "Investment & Partnerships", "coins",
                    "شبكة المستثمرين الملائكيين\nصندوق استثماري\nدعم +58 شركة\nشراكات بقيمة 420 مليون\nالاستثمار القادم في 50 شركة",
                    "Angel investor network\nInvestment fund\n58+ companies supported\nPartnerships worth 420 million\nNext investment: 50 companies"),
                new("الأثر", "Impact", "target",
                    "نحوّل الأفكار – مشاريع – شركات – استثمارات حقيقية\nبناء مستقبل الابتكار والاستثمار في العالم العربي",
                    "We turn ideas into projects, companies and real investments\nBuilding the Future of Innovation & Investment in the Arab World"),
            ]),
            ("forum_goals",
            [
                new("بناء منظومة عربية متكاملة للابتكار والاستدامة.", "Building an integrated Arab ecosystem for innovation and sustainability.", null),
                new("تمكين الشباب من تحويل الأفكار إلى مشاريع قابلة للتطبيق والاستثمار.", "Empowering young people to turn ideas into projects that can be implemented and invested in.", null),
                new("دعم المشروعات الريادية ذات الأثر البيئي والمجتمعي.", "Supporting entrepreneurial ventures with environmental and social impact.", null),
                new("تحفيز بيئة الابتكار من خلال التدريب، التشبيك، والمسابقات.", "Stimulating the innovation environment through training, networking and competitions.", null),
                new("تعزيز الروابط بين رواد الأعمال، المستثمرين، الحاضنات، والجهات الداعمة.", "Strengthening links between entrepreneurs, investors, incubators and supporting bodies.", null),
            ]),
            ("forum_sectors",
            [
                new("المشروعات الخضراء والاقتصاد الأخضر", "Green Projects and the Green Economy", null),
                new("محور الشمول المالي والتقنيات الذكية", "Financial Inclusion and Smart Technologies", null),
                new("الزراعة والصناعة الذكية والطاقة المستدامة", "Smart Agriculture and Industry, and Sustainable Energy", null),
                new("التحول الرقمي والتكنولوجيا المتقدمة", "Digital Transformation and Advanced Technology", null),
                new("التعليم، الصحة، والسياحة العلاجية والمجتمعية", "Education, Health, and Medical and Community Tourism", null),
                new("الاستثمار، التمويل وريادة الأعمال", "Investment, Finance and Entrepreneurship", null),
            ]),
        ];

        /// <summary>The 8 forum_components rows of AddSiteStructure, byte for byte (the swap's guard and Down's restore).</summary>
        public static readonly Item[] OldComponents =
        [
            new("معرض إنوفست للشركات الناشئة", "Innovest Startup Expo", "rocket"),
            new("منتدى الابتكار والاستثمار العربي", "Investment Forum Event", "users"),
            new("ملتقى الذكاء الاصطناعي", "AI Summit", "cpu"),
            new("مسابقة العروض التقديمية", "Pitch Competition", "mic"),
            new("اجتماعات المستثمرين", "Investment Meetings", "briefcase"),
            new("التشبيك بين الشركات", "Business Matching", "link"),
            new("جلسات حكومية", "Government Sessions", "building"),
            new("جوائز الابتكار", "Innovation Awards", "award"),
        ];

        /// <summary>The 7 day components of the event profile (§9): title + paragraph; the awards row lists the 5 awards.</summary>
        public static readonly Item[] NewComponents =
        [
            new("الجلسات الحوارية وورش العمل", "Panel Discussions and Workshops", "mic",
                "بمشاركة نخبة من المفكرين ورواد الأعمال وصنّاع القرار العرب، تُناقش أبرز القضايا المرتبطة بالابتكار، ريادة الأعمال، الاقتصاد الرقمي، والاستثمار المستدام في العالم العربي، وتدعم ورش العمل مجالات الابتكار وريادة الأعمال.",
                "With a select group of Arab thinkers, entrepreneurs and decision-makers, the sessions discuss the key issues of innovation, entrepreneurship, the digital economy and sustainable investment in the Arab world, while the workshops support innovation and entrepreneurship."),
            new("مسابقة INNOVEST للابتكار وريادة الأعمال", "INNOVEST Innovation and Entrepreneurship Competition", "target",
                "تُعد هذه المسابقة إحدى الركائز الأساسية للمنتدى، حيث تشمل المشاركين الذين تم تأهيلهم مسبقًا عبر برنامج تدريبي مكثف نُفذ قبل انطلاق المنتدى، بالإضافة إلى المشاركين الذين تقدموا مباشرة للمسابقة دون حضور البرنامج. يتم خلال المسابقة عرض المشاريع الريادية أمام لجنة تحكيم متخصصة، ويُمنح الفائزون جوائز وفرص احتضان وتطوير لمشاريعهم.",
                "This competition is one of the Forum's core pillars. It includes participants who were prepared in advance through an intensive training programme held before the Forum, as well as participants who applied directly without attending the programme. Entrepreneurial projects are presented to a specialised judging panel, and the winners receive prizes and incubation and development opportunities for their projects."),
            new("لقاءات المستثمرين (Meet the Investors)", "Meet the Investors", "briefcase",
                "جلسات ثنائية مباشرة تجمع بين رواد الأعمال والمستثمرين، لعرض المشاريع الريادية واستكشاف فرص التمويل والاحتضان.",
                "Direct one-to-one sessions bringing entrepreneurs and investors together to present ventures and explore funding and incubation opportunities."),
            new("جلسات الإلهام (INNOVEST Talks)", "INNOVEST Talks", "idea",
                "كلمات تحفيزية وقصص نجاح مؤثرة يقدمها رواد أعمال ومبتكرون عرب، يشاركون تجاربهم وتحدياتهم في مسيرتهم المهنية.",
                "Motivational talks and inspiring success stories from Arab entrepreneurs and innovators, who share their experiences and the challenges of their careers."),
            new("منصة الشراكات والتشبيك العربي", "Arab Partnerships and Networking Platform", "handshake",
                "بيئة تفاعلية احترافية تُمكّن المشاركين من توقيع اتفاقيات تعاون، وبناء شراكات استراتيجية بين الجهات الداعمة للابتكار، إلى جانب تعزيز التواصل الفعّال وتبادل الخبرات وبناء علاقات مهنية تدعم استدامة منظومة الابتكار في العالم العربي.",
                "A professional, interactive environment where participants sign cooperation agreements and build strategic partnerships among organisations that support innovation, while strengthening effective communication, exchanging expertise and building professional relationships that sustain the innovation ecosystem in the Arab world."),
            new("معرض INNOVEST للابتكار", "INNOVEST Innovation Exhibition", "cpu",
                "منصة لعرض الابتكارات، النماذج الأولية، والتقنيات الحديثة من الشركات الناشئة، والمراكز البحثية، والمؤسسات الداعمة للابتكار.",
                "A showcase for innovations, prototypes and new technologies from startups, research centres and institutions that support innovation."),
            new("جوائز منتدى إنوفست", "Innovest Forum Awards", "award",
                "الجائزة العربية لريادة الأعمال والابتكار\nالجائزة العربية للمسؤولية المجتمعية للشركات\nالجائزة العربية للذكاء الاصطناعي\nجائزة أفضل شركة ناشئة\nجائزة أفضل رائد أعمال",
                "Arab Award for Entrepreneurship and Innovation\nArab Award for Corporate Social Responsibility\nArab Award for Artificial Intelligence\nBest Startup Award\nBest Entrepreneur Award"),
        ];

        /// <summary>The components heading: replaced (with the swap only) while it still holds ApplyCompanyProfile's value.</summary>
        public static readonly (string Ar, string En) OldComponentsTitle = ("مكونات {المنتدى}", "What the {Forum} Includes");
        public static readonly (string Ar, string En) NewComponentsTitle = ("مكونات اليوم", "Programme of the Day");

        // ── Partner groups and partners (§5). Groups go BEFORE the existing ones; partners after, off the home strip. ──

        public sealed record Group(string NameAr, string NameEn, bool ShowOnForumHub);

        public static readonly Group[] PartnerGroups =
        [
            new("برعاية", "Under the Patronage of", true),
            new("بمشاركة", "With the Participation of", false),
            new("بتنظيم", "Powered by", true),
            new("شريك حصري", "Exclusive Partner", false),
            new("شريك استراتيجي", "Strategic Partner", false),
        ];

        public sealed record PartnerRow(string NameAr, string NameEn, string GroupAr, bool IsActive);

        public static readonly PartnerRow[] Partners =
        [
            new("جامعة الدول العربية", "Arab League", "برعاية", true),
            new("وزارة البيئة", "Ministry of Environment", "بمشاركة", false),
            new("وزارة الشباب والرياضة", "Ministry of Youth and Sports", "بمشاركة", false),
            new("وزارة الصناعة", "Ministry of Industry", "بمشاركة", false),
            new("وزارة التخطيط والتنمية الاقتصادية", "Ministry of Planning and Economic Development", "بمشاركة", false),
            new("مؤسسة شباب قادرون للتنمية المستدامة", "Qadroon Youth Foundation for Sustainable Development", "بتنظيم", true),
            new("كابيتال للعلوم والتكنولوجيا", "CAPITAL – Capital For Science & Technology", "شريك حصري", true),
            new("أكاديمية ألوندرا", "Alondra Academy", "شريك استراتيجي", true),
        ];

        // ── The 2026 programme (§10): month precision, offset +02:00, chronological. All visible (Anas, 2026-10-23). ──

        /// <param name="PlaceAr">Written to LocationAr/En only (City stays empty: the venue is shown once, QA F10-QA-1).</param>
        public sealed record EventRow(string Slug, string TitleAr, string TitleEn, int Month, string MonthAr, string MonthEn,
            string PlaceAr, string PlaceEn);

        public const string EventOffset = "+02:00";
        public const int EventYear = 2026;

        public static readonly EventRow[] Events =
        [
            new("2026-06-first-preparatory-forum", "الملتقى التحضيري الأول", "First Preparatory Forum", 6, "يونيو", "June", "", null),
            new("2026-06-ai-digital-economy-forum", "ملتقى الذكاء الاصطناعي والاقتصاد الرقمي", "Artificial Intelligence and Digital Economy Forum", 6, "يونيو", "June", "", null),
            new("2026-07-arab-women-entrepreneurship-forum", "ملتقى المرأة العربية لريادة الأعمال", "Arab Women Entrepreneurship Forum", 7, "يوليو", "July", "", null),
            new("2026-08-innovest-upper-egypt-forum", "ملتقى إنوفست صعيد مصر", "Innovest Upper Egypt Forum", 8, "أغسطس", "August", "صعيد مصر", "Upper Egypt"),
            new("2026-09-innovest-universities-forum", "ملتقى إنوفست الجامعات", "Innovest Universities Forum", 9, "سبتمبر", "September", "", null),
            new("2026-10-regional-summit-libya", "ملتقى القمة الإقليمية – ليبيا", "Regional Summit – Libya", 10, "أكتوبر", "October", "ليبيا", "Libya"),
            new("2026-11-innovest-competition-finale", "ختام مسابقة إنوفست", "Innovest Competition Finale", 11, "نوفمبر", "November", "", null),
            new("2026-11-regional-summit-bahrain", "ملتقى القمة الإقليمية – البحرين", "Regional Summit – Bahrain", 11, "نوفمبر", "November", "البحرين", "Bahrain"),
            new("2026-12-annual-closing-arab-league", "الختام السنوي بجامعة الدول العربية", "Annual Closing at the Arab League", 12, "ديسمبر", "December", "جامعة الدول العربية", "Arab League"),
        ];

        // ── Naming rule (O1/O2): seeded values → the forum's full name. (table, row filter, column, seeded value, new value) ──

        public const string CardComponentsAr =
            "معرض إنوفست للشركات الناشئة\nمنتدى الابتكار والاستثمار العربي\nملتقى الذكاء الاصطناعي\nمسابقة العروض التقديمية\nاجتماعات المستثمرين\nالتشبيك بين الشركات\nجلسات حكومية\nجوائز الابتكار";
        public const string CardComponentsEn =
            "Innovest Startup Expo\nInvestment Forum Event\nAI Summit\nPitch Competition\nInvestment Meetings\nBusiness Matching\nGovernment Sessions\nInnovation Awards";
        public const string ForumFullNameAr = "منتدى إنوفست للابتكار والاستثمار";
        public const string ForumFullNameEn = "Innovest Forum for Innovation and Investment";

        /// <summary>The hub's English meta title: the full name does not fit the 60-character title rule with the
        /// " | Innovest Holding" suffix (63), so the title carries it in this form (60 with the suffix).</summary>
        public const string HubMetaTitleEn = "Innovest Forum: Innovation and Investment";

        public static readonly (string Table, string Key, string Column, string Old, string New)[] NamingUpdates =
        [
            ("Platforms", "TitleAr = N'منتدى إنوفست'", "DescriptionAr", CardComponentsAr,
                CardComponentsAr.Replace("منتدى الابتكار والاستثمار العربي", ForumFullNameAr, StringComparison.Ordinal)),
            ("Platforms", "TitleAr = N'منتدى إنوفست'", "DescriptionEn", CardComponentsEn,
                CardComponentsEn.Replace("Investment Forum Event", ForumFullNameEn, StringComparison.Ordinal)),
            ("Pages", "[Key] = 'forum'", "MetaTitleEn", "Innovest Forum for Investment", HubMetaTitleEn),
        ];

        /// <summary>"2026-06-01T00:00:00+02:00" and the month's last day at 23:59.</summary>
        public static (string Starts, string Ends) MonthRange(int month)
        {
            var last = DateTime.DaysInMonth(EventYear, month);
            return ($"{EventYear:D4}-{month:D2}-01T00:00:00{EventOffset}", $"{EventYear:D4}-{month:D2}-{last:D2}T23:59:00{EventOffset}");
        }

        // ── Past editions (§6), newest first. Photos are imported at startup (ForumMediaImport). ──

        public sealed record EditionRow(short Year, string LabelAr, string LabelEn, string TitleAr, string TitleEn, string PlaceAr, string PlaceEn);

        public static readonly EditionRow[] Editions =
        [
            new(2026, "النسخة الرابعة", "Fourth Edition", "منتدى إنوفست 2026", "Innovest Forum 2026", "", null),
            new(2025, "النسخة الرابعة", "Fourth Edition", "قمة الابتكار والتكنولوجيا للشباب العربي – الأردن 2025", "Arab Youth Innovation and Technology Summit – Jordan 2025", "الأردن", "Jordan"),
            new(2025, "النسخة الرابعة", "Fourth Edition", "ملتقى Innovest Tech University", "Innovest Tech University Forum", "", null),
            new(2025, "النسخة الثالثة", "Third Edition", "منتدى إنوفست 2025", "Innovest Forum 2025", "", null),
            new(2024, "النسخة الثانية", "Second Edition", "مؤتمر ريادة الأعمال والاستثمار العربي 2024", "Arab Entrepreneurship and Investment Conference 2024", "", null),
            new(2023, "النسخة الأولى", "First Edition", "مؤتمر ريادة الأعمال والتحول الرقمي 2023", "Entrepreneurship and Digital Transformation Conference 2023", "", null),
        ];

        /// <summary>The startup importer's marker (Infrastructure/Content/ForumMediaImport): Down removes it with the rows.</summary>
        public const string MediaMarker = "sys_forum_media_v1";

        private static string ItemValues(Item[] rows) => string.Join(",\n", rows.Select((r, i) =>
            $"({L(r.TextAr)}, {L(r.TextEn)}, {(r.Icon is null ? "CAST(NULL AS varchar(40))" : "'" + r.Icon + "'")}, {Text(r.DescriptionAr)}, {Text(r.DescriptionEn)}, {i + 1})"));

        /// <summary>A description literal; NULL typed as nvarchar (an all-NULL VALUES column would otherwise be int).</summary>
        private static string Text(string value) => value is null ? "CAST(NULL AS nvarchar(2000))" : L(value);

        private static string InsertItems(string group, Item[] rows) =>
            "INSERT INTO ContentItems ([Group], TextAr, TextEn, IconKey, DescriptionAr, DescriptionEn, SortOrder, IsActive, CreatedAt)\n" +
            $"SELECT '{group}', v.TA, v.TE, v.I, v.DA, v.DE, v.S, 1, SYSUTCDATETIME() FROM (VALUES\n{ItemValues(rows)}\n) v(TA, TE, I, DA, DE, S);";

        /// <summary>The number of rows of <paramref name="group"/> equal (binary, description and IsActive included) to one of
        /// <paramref name="rows"/>, counted once per frozen row.</summary>
        private static string Matching(string group, Item[] rows) =>
            $"(SELECT COUNT(DISTINCT v.S) FROM ContentItems c JOIN (VALUES\n{ItemValues(rows)}\n) v(TA, TE, I, DA, DE, S) " +
            $"ON c.TextAr {Bin} = v.TA AND c.TextEn {Bin} = v.TE AND ISNULL(c.IconKey, '') {Bin} = ISNULL(v.I, '') " +
            $"AND ISNULL(c.DescriptionAr, N'') {Bin} = ISNULL(v.DA, N'') AND ISNULL(c.DescriptionEn, N'') {Bin} = ISNULL(v.DE, N'') " +
            $"WHERE c.[Group] = '{group}' AND c.IsActive = 1)";

        private static string GroupCount(string group) => $"(SELECT COUNT(*) FROM ContentItems WHERE [Group] = '{group}')";

        /// <summary>The statements of Up's data part, in order (the tests run them a second time to prove a re-run is a no-op).</summary>
        public static IReadOnlyList<string> UpSql()
        {
            var sql = new List<string>();

            // 1. Content keys, each only when missing (IX_SiteContents_SectionKey is unique).
            var keys = string.Join(",\n", NewKeys.Select(r => $"({L(r[0])}, {L(r[1])}, {L(r[2])})"));
            sql.Add("INSERT INTO SiteContents (SectionKey, ContentAr, ContentEn, UpdatedAt)\n" +
                    $"SELECT v.K, v.Ar, v.En, SYSUTCDATETIME() FROM (VALUES\n{keys}\n) v(K, Ar, En) " +
                    "WHERE NOT EXISTS (SELECT 1 FROM SiteContents s WHERE s.SectionKey = v.K);");

            // 2. The editions page, only when it has no row (after the forum's other children).
            sql.Add($"INSERT INTO Pages ([Key], {string.Join(", ", EditionsPageRow.Select(c => "[" + c.Column + "]"))}, " +
                    "ShowInMenu, ShowInFooter, IsPublished, SortOrder, UpdatedAt)\n" +
                    $"SELECT '{EditionsPageKey}', {string.Join(", ", EditionsPageRow.Select(c => L(c.Value)))}, 1, 1, 1, " +
                    $"ISNULL((SELECT MAX(SortOrder) FROM Pages WHERE [Key] IN ({ForumChildren})), 0) + 1, SYSUTCDATETIME()\n" +
                    $"WHERE NOT EXISTS (SELECT 1 FROM Pages WHERE [Key] = '{EditionsPageKey}');");

            // 3. Section rows, only where missing (IX_PageSections_PageKey_SectionKey is unique).
            var sections = string.Join(",\n", SectionRows.Select(s => $"('{s.Page}', '{s.Section}', {s.Order})"));
            sql.Add("INSERT INTO PageSections (PageKey, SectionKey, IsVisible, SortOrder, UpdatedAt)\n" +
                    $"SELECT v.P, v.S, 1, v.O, SYSUTCDATETIME() FROM (VALUES\n{sections}\n) v(P, S, O) " +
                    "WHERE NOT EXISTS (SELECT 1 FROM PageSections x WHERE x.PageKey = v.P AND x.SectionKey = v.S);");

            // 4. The new lists: a group only while the table holds no row of it (a group the admin started is left alone).
            foreach (var (group, rows) in ItemGroups)
                sql.Add($"IF NOT EXISTS (SELECT 1 FROM ContentItems WHERE [Group] = '{group}')\n" + InsertItems(group, rows));

            // 5. forum_components: the 8 company-profile rows become the 7 day components — only while the group holds
            //    exactly those 8 rows, untouched (text, icon, no description, active). The heading follows, only while it
            //    still holds its frozen value. Otherwise nothing (the admin's list wins; the deploy note says so).
            sql.Add($"IF {GroupCount("forum_components")} = {OldComponents.Length} AND {Matching("forum_components", OldComponents)} = {OldComponents.Length}\n" +
                    "BEGIN\n" +
                    "DELETE FROM ContentItems WHERE [Group] = 'forum_components';\n" +
                    InsertItems("forum_components", NewComponents) + "\n" +
                    $"UPDATE SiteContents SET ContentAr = {L(NewComponentsTitle.Ar)}, ContentEn = {L(NewComponentsTitle.En)}, UpdatedAt = SYSUTCDATETIME() " +
                    $"WHERE SectionKey = 'section_forum_components_title' AND {Eq("ContentAr", OldComponentsTitle.Ar)} AND {Eq("ContentEn", OldComponentsTitle.En)};\n" +
                    "END");

            // 6. Partner groups (by NameAr), before the existing groups, with their forum-hub flag (a new column: EXEC).
            var groups = string.Join(",\n", PartnerGroups.Select((g, i) => $"({L(g.NameAr)}, {L(g.NameEn)}, {(g.ShowOnForumHub ? 1 : 0)}, {i + 1})"));
            sql.Add(Exec("INSERT INTO PartnerGroups (NameAr, NameEn, ShowOnForumHub, SortOrder, IsActive, CreatedAt)\n" +
                    $"SELECT v.A, v.E, v.H, ISNULL((SELECT MIN(SortOrder) FROM PartnerGroups), 10) - 10 + v.I, 1, SYSUTCDATETIME() FROM (VALUES\n{groups}\n) v(A, E, H, I) " +
                    $"WHERE NOT EXISTS (SELECT 1 FROM PartnerGroups g WHERE g.NameAr {Bin} = v.A);"));

            // 7. Partners (by NameAr), after the existing ones, not on the home strip, no logo yet (the importer adds it).
            var partners = string.Join(",\n", Partners.Select((p, i) => $"({L(p.NameAr)}, {L(p.NameEn)}, {L(p.GroupAr)}, {(p.IsActive ? 1 : 0)}, {i + 1})"));
            sql.Add("INSERT INTO Partners (NameAr, NameEn, PartnerGroupId, IsActive, ShowOnHome, SortOrder, CreatedAt, UpdatedAt)\n" +
                    $"SELECT v.A, v.E, (SELECT TOP 1 g.Id FROM PartnerGroups g WHERE g.NameAr {Bin} = v.G ORDER BY g.Id), v.Act, 0, " +
                    "ISNULL((SELECT MAX(SortOrder) FROM Partners), 0) + v.I, SYSUTCDATETIME(), SYSUTCDATETIME() FROM (VALUES\n" +
                    $"{partners}\n) v(A, E, G, Act, I) " +
                    $"WHERE NOT EXISTS (SELECT 1 FROM Partners p WHERE p.NameAr {Bin} = v.A);");

            // 8. The 2026 programme (by Slug, or an event the admin already typed with the same Arabic title).
            var events = string.Join(",\n", Events.Select((e, i) =>
            {
                var (starts, ends) = MonthRange(e.Month);
                return $"('{e.Slug}', {L(e.TitleAr)}, {L(e.TitleEn)}, {L(e.MonthAr)}, {L(e.MonthEn)}, {L(e.PlaceAr)}, {L(e.PlaceEn)}, " +
                       $"'{starts}', '{ends}', {i + 1})";
            }));
            sql.Add(Exec("INSERT INTO Events (Slug, TitleAr, TitleEn, DescriptionAr, DescriptionEn, SummaryAr, SummaryEn, EventDay, EventMonthAr, EventMonthEn, " +
                    "LocationAr, LocationEn, TimeAr, TimeEn, CityAr, CityEn, AddressAr, AddressEn, StartsAt, EndsAt, DateIsMonth, Status, AttendanceMode, " +
                    "RegistrationOpen, MetaTitleAr, MetaTitleEn, MetaDescriptionAr, MetaDescriptionEn, IsActive, SortOrder, CreatedAt, UpdatedAt)\n" +
                    // The place goes to Location only (City empty), so the detail page names it once.
                    "SELECT v.Slug, v.TA, v.TE, N'', NULL, N'', NULL, 1, v.MA, v.ME, v.CA, v.CE, N'', NULL, N'', NULL, N'', NULL, " +
                    "CAST(v.S AS datetimeoffset(0)), CAST(v.E AS datetimeoffset(0)), 1, 0, 0, 1, N'', NULL, N'', NULL, 1, " +
                    "ISNULL((SELECT MAX(SortOrder) FROM Events), 0) + v.I, SYSUTCDATETIME(), SYSUTCDATETIME() FROM (VALUES\n" +
                    $"{events}\n) v(Slug, TA, TE, MA, ME, CA, CE, S, E, I) " +
                    $"WHERE NOT EXISTS (SELECT 1 FROM Events x WHERE x.Slug = v.Slug OR x.TitleAr {Bin} = v.TA);"));

            // 9. Past editions (by Year + TitleAr), after any existing ones; photos come from the startup importer.
            var editions = string.Join(",\n", Editions.Select((e, i) =>
                $"({e.Year}, {L(e.LabelAr)}, {L(e.LabelEn)}, {L(e.TitleAr)}, {L(e.TitleEn)}, {L(e.PlaceAr)}, {L(e.PlaceEn)}, {i + 1})"));
            sql.Add(Exec("INSERT INTO ForumEditions ([Year], EditionLabelAr, EditionLabelEn, TitleAr, TitleEn, PlaceAr, PlaceEn, TextAr, TextEn, SortOrder, IsActive, CreatedAt, UpdatedAt)\n" +
                    "SELECT v.Y, v.LA, v.LE, v.TA, v.TE, v.PA, v.PE, N'', NULL, ISNULL((SELECT MAX(SortOrder) FROM ForumEditions), 0) + v.I, 1, " +
                    $"SYSUTCDATETIME(), SYSUTCDATETIME() FROM (VALUES\n{editions}\n) v(Y, LA, LE, TA, TE, PA, PE, I) " +
                    $"WHERE NOT EXISTS (SELECT 1 FROM ForumEditions x WHERE x.[Year] = v.Y AND x.TitleAr {Bin} = v.TA);"));

            // 10. Naming rule (CTO, QA O1/O2): the home forum card's components list and the hub's English meta title name
            //     the forum «منتدى إنوفست للابتكار والاستثمار» / "Innovest Forum for Innovation and Investment". Each column is
            //     changed only while it still holds its seeded value (binary): an admin's edit is never overwritten.
            foreach (var (table, key, column, old, @new) in NamingUpdates)
                sql.Add($"UPDATE {table} SET [{column}] = {L(@new)} WHERE {key} AND {Eq("[" + column + "]", old)};");

            return sql;
        }

        /// <summary>The statements of Down's data part (run before the tables and columns are dropped).</summary>
        public static IReadOnlyList<string> DownSql()
        {
            var sql = new List<string>();

            // Imported photos: their media rows go with the table (the files are left for the orphan sweep).
            sql.Add(Exec("DECLARE @images TABLE (Id int);\n" +
                         "DELETE FROM ForumEditionPhotos OUTPUT deleted.ImageId INTO @images;\n" +
                         "DELETE FROM MediaFiles WHERE Id IN (SELECT Id FROM @images);"));

            // Events Up inserted, while still equal.
            foreach (var e in Events)
                sql.Add(Exec($"DELETE FROM Events WHERE Slug = '{e.Slug}' AND DateIsMonth = 1 AND {Eq("TitleAr", e.TitleAr)} AND {Eq("TitleEn", e.TitleEn)};"));

            // Partners Up inserted, while still equal (name, off the home strip, no URL), with their logo rows.
            foreach (var p in Partners)
                sql.Add(Exec("DECLARE @logos TABLE (Id int);\n" +
                        "DELETE FROM Partners OUTPUT deleted.LogoId INTO @logos " +
                        $"WHERE {Eq("NameAr", p.NameAr)} AND {Eq("NameEn", p.NameEn)} AND ShowOnHome = 0 AND Url IS NULL;\n" +
                        "DELETE FROM MediaFiles WHERE Id IN (SELECT Id FROM @logos WHERE Id IS NOT NULL);"));

            // Partner groups Up inserted, while still equal (their partners, if any, fall back to "no group").
            foreach (var g in PartnerGroups)
                sql.Add($"DELETE FROM PartnerGroups WHERE {Eq("NameAr", g.NameAr)} AND {Eq("NameEn", g.NameEn)};");

            // forum_components back to the 8 rows while the group holds exactly the 7, untouched; then the heading.
            sql.Add($"IF {GroupCount("forum_components")} = {NewComponents.Length} AND {Matching("forum_components", NewComponents)} = {NewComponents.Length}\n" +
                    "BEGIN\n" +
                    "DELETE FROM ContentItems WHERE [Group] = 'forum_components';\n" +
                    InsertItems("forum_components", OldComponents) + "\n" +
                    $"UPDATE SiteContents SET ContentAr = {L(OldComponentsTitle.Ar)}, ContentEn = {L(OldComponentsTitle.En)} " +
                    $"WHERE SectionKey = 'section_forum_components_title' AND {Eq("ContentAr", NewComponentsTitle.Ar)} AND {Eq("ContentEn", NewComponentsTitle.En)};\n" +
                    "END");

            // The new lists' rows, each while still equal to what Up wrote.
            foreach (var (group, rows) in ItemGroups)
                foreach (var r in rows)
                    sql.Add($"DELETE FROM ContentItems WHERE [Group] = '{group}' AND {Eq("TextAr", r.TextAr)} AND {Eq("TextEn", r.TextEn)} AND " +
                            $"{(r.Icon is null ? "IconKey IS NULL" : $"IconKey = '{r.Icon}'")} AND {Eq("DescriptionAr", r.DescriptionAr)} AND {Eq("DescriptionEn", r.DescriptionEn)};");

            // The section rows (structure only, created by Up).
            foreach (var s in SectionRows)
                sql.Add($"DELETE FROM PageSections WHERE PageKey = '{s.Page}' AND SectionKey = '{s.Section}';");

            // The editions page, while every text still equals what Up wrote.
            sql.Add($"DELETE FROM Pages WHERE [Key] = '{EditionsPageKey}' AND " +
                    string.Join(" AND ", EditionsPageRow.Select(c => Eq("[" + c.Column + "]", c.Value))) + ";");

            // Keys Up inserted, while they still hold Up's values (an admin edit stays); the importer's marker.
            foreach (var r in NewKeys)
                sql.Add($"DELETE FROM SiteContents WHERE SectionKey = {L(r[0])} AND {Eq("ContentAr", r[1])} AND {Eq("ContentEn", r[2])};");
            sql.Add($"DELETE FROM SiteContents WHERE SectionKey = '{MediaMarker}';");

            // The naming updates back, only while each value is still what Up wrote.
            foreach (var (table, key, column, old, @new) in NamingUpdates)
                sql.Add($"UPDATE {table} SET [{column}] = {L(old)} WHERE {key} AND {Eq("[" + column + "]", @new)};");
            return sql;
        }

        private static void UpData(MigrationBuilder mb)
        {
            foreach (var s in UpSql()) mb.Sql(s);
        }

        private static void DownData(MigrationBuilder mb)
        {
            foreach (var s in DownSql()) mb.Sql(s);
        }
    }
}
