using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovestHolding.Migrations
{
    /// <summary>
    /// The data half of AddBilingualContent. Every literal below is FROZEN: a migration must produce the same result
    /// forever, so nothing here may be read from application code that can change later. DbSeeder writes the same
    /// entity English for fresh installs (test FreshAndUpgradedDatabasesMatch compares both, row by row).
    ///
    /// Guard polarity (ANTIGRAVITY §3): every write is conditional so it does LESS when data already exists —
    /// keys are inserted only when missing, English is written only where it is NULL and only where the Arabic is
    /// still the original seed text (an admin-edited Arabic row is left for the admin to translate).
    /// </summary>
    public partial class AddBilingualContent
    {
        private const string Bin = "COLLATE Latin1_General_100_BIN2";

        /// <summary>N'…' with quotes doubled; NULL for null.</summary>
        private static string L(string value) => value is null ? "NULL" : "N'" + value.Replace("'", "''") + "'";

        private static string Values(IEnumerable<string[]> rows) =>
            string.Join(",\n", rows.Select(r => "(" + string.Join(", ", r.Select(L)) + ")"));

        // ── SiteContent keys: (key, Arabic, English or null). Neutral keys keep English NULL. ──
        private static readonly string[][] Keys =
        [
            // Hero / header
            ["hero_title", "إنوفست هولدينغ", "INNOVEST HOLDING"],
            ["hero_kicker", "INNOVEST HOLDING", null],
            ["hero_subtitle", "نبني جسور الاستثمار والابتكار من خلال منصات متكاملة للتدريب والتعليم والاستثمار والفعاليات والشراكات الاستراتيجية", "Building bridges between investment and innovation through integrated platforms for training and education, investment, events and strategic partnerships"],
            ["hero_badge", "مرحباً بكم في إنوفست هولدينغ", "Welcome to Innovest Holding"],
            ["hero_cta_primary", "اكتشف منصاتنا", "Discover Our Platforms"],
            ["cta_register", "سجّل الآن", "Register Now"],
            // About
            ["tagline", "نبني جسور الاستثمار والابتكار", "Building bridges of investment and innovation"],
            ["about_years", "+10", null],
            ["about_years_label", "سنوات من\nالخبرة والإنجاز", "Years of experience\nand achievement"],
            ["about_badge", "من نحن", "About Us"],
            ["about_title", "نحن {إنوفست هولدينغ}", "We are {Innovest Holding}"],
            ["about_text_1", "إنوفست هولدينغ هي شركة رائدة في مجال الاستثمار والابتكار، تأسست برؤية طموحة لبناء منظومة متكاملة تجمع بين التدريب والتعليم، والاستثمار، والتشبيك، والفعاليات. نسعى لتمكين رواد الأعمال والمستثمرين والشركات من خلال منصات متخصصة توفر بيئة محفزة للنمو والتطور.", "Innovest Holding is a leading investment and innovation company, founded with an ambitious vision to build an integrated ecosystem that brings together training and education, investment, networking and events. We empower entrepreneurs, investors and companies through specialized platforms that provide an environment for growth and development."],
            ["about_text_2", "نعمل على تقديم حلول مبتكرة تلبي احتياجات السوق المتنامية، مع التركيز على الجودة والاحترافية في كل ما نقدمه من خدمات ومنصات.", "We deliver innovative solutions that meet the needs of a growing market, focusing on quality and professionalism in every service and platform we offer."],
            ["vision_title", "الرؤية", "Our Vision"],
            ["vision_text", "أن نكون المنصة الأولى للاستثمار والابتكار في المنطقة", "To be the leading platform for investment and innovation in the region"],
            ["mission_title", "الرسالة", "Our Mission"],
            ["mission_text", "تمكين الأفراد والشركات عبر منصات متكاملة ومبتكرة", "Empowering individuals and companies through integrated, innovative platforms"],
            ["values_title", "القيم", "Our Values"],
            ["values_text", "الابتكار والجودة والشفافية والتميز المستمر", "Innovation, quality, transparency and continuous excellence"],
            // Section headings
            ["platforms_badge", "منصاتنا", "Our Platforms"],
            ["platforms_title", "منصاتنا {المتكاملة}", "Our {Integrated} Platforms"],
            ["platforms_subtitle", "نقدم مجموعة متنوعة من المنصات المتخصصة المصممة لتلبية احتياجاتكم في التدريب والاستثمار والتشبيك والفعاليات", "We offer a diverse range of specialized platforms designed to meet your needs in training, investment, networking and events"],
            ["platforms_read_more", "اعرف المزيد", "Learn more"],
            ["news_badge", "الأخبار والتحديثات", "News & Updates"],
            ["news_title", "آخر {الأخبار} والتحديثات", "Latest {News} & Updates"],
            ["news_subtitle", "تابع أحدث الأخبار والمستجدات من عالم إنوفست هولدينغ ومنصاتها المتعددة", "Follow the latest news and updates from the world of Innovest Holding and its platforms"],
            ["news_read_more", "اقرأ المزيد", "Read more"],
            ["home_news_limit", "6", null],
            ["events_badge", "الفعاليات القادمة", "Upcoming Events"],
            ["events_title", "الفعاليات {القادمة}", "Upcoming {Events}"],
            ["events_subtitle", "انضم إلى فعالياتنا القادمة وتعرف على أحدث الفرص في عالم الاستثمار والأعمال", "Join our upcoming events and discover the latest opportunities in investment and business"],
            ["partners_badge", "شركاؤنا ورعاتنا", "Our Partners & Sponsors"],
            ["partners_title", "شركاؤنا {ورعاتنا}", "Our Partners & {Sponsors}"],
            ["partners_subtitle", "نفتخر بشراكاتنا مع كبرى المؤسسات والشركات الرائدة في مختلف القطاعات", "We are proud of our partnerships with leading institutions and companies across sectors"],
            ["faq_badge", "الأسئلة الشائعة", "FAQ"],
            ["faq_title", "الأسئلة {الشائعة}", "Frequently {Asked} Questions"],
            ["faq_subtitle", "إجابات على أكثر الأسئلة شيوعاً حول إنوفست هولدينغ وخدماتها", "Answers to the most common questions about Innovest Holding and its services"],
            ["register_badge", "التسجيل السريع", "Quick Registration"],
            ["register_title", "سجّل الآن وانضم إلى {مجتمع إنوفست}", "Register now and join the {Innovest community}"],
            ["register_text", "انضم إلى آلاف المستثمرين ورواد الأعمال والمتدربين الذين يثقون في إنوفست هولدينغ. سجّل الآن واحصل على أحدث العروض والفعاليات.", "Join thousands of investors, entrepreneurs and trainees who trust Innovest Holding. Register now and receive the latest offers and events."],
            ["contact_badge", "تواصل معنا", "Contact Us"],
            ["contact_title", "تواصل {معنا}", "Get in {touch}"],
            ["contact_subtitle", "نحن هنا لمساعدتك. لا تتردد في التواصل معنا لأي استفسار أو اقتراح", "We are here to help. Do not hesitate to contact us with any enquiry or suggestion"],
            ["contact_intro", "فريق إنوفست هولدينغ مستعد دائماً للإجابة على استفساراتكم وتقديم المساعدة. يمكنكم التواصل معنا عبر أي من القنوات التالية:", "The Innovest Holding team is always ready to answer your questions and assist you. You can reach us through any of the following channels:"],
            // Contact details
            ["contact_address", "Salah Salem St, Heliopolis, Cairo", "Salah Salem St, Heliopolis, Cairo"],
            ["contact_phone", "+20 114 141 0808", null],
            ["contact_email", "info@innovestholding.com", null],
            ["contact_hours", "الأحد - الخميس: 9:00 ص - 5:00 م", "Sunday - Thursday: 9:00 AM - 5:00 PM"],
            // Footer
            ["footer_description", "إنوفست هولدينغ - نبني جسور الاستثمار والابتكار من خلال منصات متكاملة للتدريب والتعليم والاستثمار والفعاليات والشراكات الاستراتيجية.", "Innovest Holding - building bridges between investment and innovation through integrated platforms for training and education, investment, events and strategic partnerships."],
            ["footer_quicklinks_title", "روابط سريعة", "Quick links"],
            ["footer_platforms_title", "منصاتنا", "Our platforms"],
            ["footer_contact_title", "تواصل معنا", "Contact us"],
            ["footer_copyright", "© {year} إنوفست هولدينغ. جميع الحقوق محفوظة.", "© {year} Innovest Holding. All rights reserved."],
            ["footer_credit_text", "تصميم وتطوير بواسطة", "Designed and developed by"],
            ["footer_credit_name", "كودرز للبرمجيات", "Coderz Software"],
            ["footer_credit_url", "https://www.coderz-tech.com", null],
            // SEO / organisation
            ["site_name", "إنوفست هولدينغ", "Innovest Holding"],
            ["seo_title", "نبني جسور الاستثمار والابتكار", "Investment & Innovation Bridges"],
            ["seo_description", "إنوفست هولدينغ - نبني جسور الاستثمار والابتكار", "Innovest Holding: integrated platforms for training and education, investment, events and strategic partnerships."],
            ["seo_og_image", "/assets/logo.png", null],
            ["seo_og_image_alt", "إنوفست هولدينغ", "Innovest Holding"],
            // The production domain, so canonical/hreflang/og:url/sitemap work from the first start without a manual
            // step; the admin can change it. An existing row (if any) is never overwritten.
            ["site_base_url", "https://www.innovest-holding.com", null],
            ["org_address_locality", "", null],
            ["org_address_country", "", null],
        ];

        /// <summary>The 16 keys of InitialCreate's seed after the hero rename (Down keeps these, deletes the rest).</summary>
        private static readonly string[] OriginalKeys =
        [
            "hero_title", "hero_kicker", "hero_subtitle", "about_text_1", "about_text_2", "about_years",
            "vision_title", "vision_text", "mission_title", "mission_text", "values_title", "values_text",
            "contact_address", "contact_phone", "contact_email", "contact_hours"
        ];

        // ── Entity English: (Arabic seed text, English) ──
        private static readonly string[][] NavItemTitles =
        [
            ["الرئيسية", "Home"], ["من نحن", "About Us"], ["منصاتنا", "Our Platforms"], ["الأخبار", "News"],
            ["الفعاليات", "Events"], ["الشركاء والرعاة", "Partners & Sponsors"], ["التسجيل السريع", "Quick Registration"],
            ["تواصل معنا", "Contact Us"],
        ];

        private static readonly string[][] PlatformTitles =
        [
            ["التدريب والتعليم", "Training & Education"],
            ["منصة المستثمرين والتشبيك", "Investors & Networking Platform"],
            ["الإعلام والمحتوى الرقمي", "Media & Digital Content"],
            ["المسابقات", "Competitions"],
            ["الفعاليات والمعارض", "Events & Exhibitions"],
            ["الشراكات الاستراتيجية و B2B", "Strategic Partnerships & B2B"],
        ];

        private static readonly string[][] PlatformDescriptions =
        [
            ["برامج تدريبية متخصصة وورش عمل احترافية مصممة لتطوير المهارات وبناء القدرات في مجالات الأعمال والتكنولوجيا والقيادة.", "Specialized training programs and professional workshops designed to develop skills and build capabilities in business, technology and leadership."],
            ["منصة فريدة تربط بين المستثمرين ورواد الأعمال والشركات، وتوفر فرص تشبيك استراتيجية وشراكات مثمرة.", "A unique platform connecting investors, entrepreneurs and companies, creating strategic networking opportunities and fruitful partnerships."],
            ["إنتاج محتوى رقمي إبداعي ومتميز، يشمل الفيديو والبودكاست والمقالات المتخصصة في عالم الأعمال والاستثمار.", "Creative, high-quality digital content including video, podcasts and specialized articles on business and investment."],
            ["مسابقات وتحديات مبتكرة تستهدف رواد الأعمال والمبدعين، مع جوائز قيمة وفرص حقيقية لتحويل الأفكار إلى مشاريع ناجحة.", "Innovative competitions and challenges for entrepreneurs and creators, with valuable prizes and real opportunities to turn ideas into successful projects."],
            ["تنظيم وإدارة فعاليات ومعارض دولية ومحلية تجمع بين أصحاب القرار والمستثمرين ورواد الأعمال في بيئة محفزة.", "Organizing and managing international and local events and exhibitions that bring decision makers, investors and entrepreneurs together in an inspiring environment."],
            ["بناء شراكات استراتيجية مع مؤسسات وشركات محلية ودولية، وتوفير حلول أعمال متكاملة لتعزيز النمو المشترك.", "Building strategic partnerships with local and international institutions and companies, and providing integrated business solutions for shared growth."],
        ];

        /// <summary>(TitleAr, ShortTitleAr, ShortTitleEn) — the shortened footer names of today's page.</summary>
        private static readonly string[][] PlatformShortTitles =
        [
            ["منصة المستثمرين والتشبيك", "المستثمرين والتشبيك", "Investors & Networking"],
            ["الإعلام والمحتوى الرقمي", "الإعلام الرقمي", "Digital Media"],
            ["الشراكات الاستراتيجية و B2B", "الشراكات و B2B", "Partnerships & B2B"],
        ];

        private static readonly string[][] StatLabels =
        [
            ["شريك استراتيجي", "Strategic partners"], ["مستثمر نشط", "Active investors"],
            ["فعالية سنوياً", "Events per year"], ["متدرب ومستفيد", "Trainees and beneficiaries"],
        ];

        private static readonly string[][] NewsTitles =
        [
            ["إنوفست هولدينغ توقع شراكة استراتيجية مع كبرى شركات التكنولوجيا", "Innovest Holding signs a strategic partnership with leading technology companies"],
            ["إطلاق برنامج تدريبي جديد في ريادة الأعمال والابتكار", "Launch of a new training program in entrepreneurship and innovation"],
            ["نجاح كبير لمعرض إنوفست الدولي للاستثمار 2026", "Great success for the Innovest International Investment Exhibition 2026"],
        ];

        private static readonly string[][] NewsSummaries =
        [
            ["أعلنت إنوفست هولدينغ عن توقيع اتفاقية شراكة استراتيجية جديدة تهدف إلى تعزيز التحول الرقمي وتوسيع نطاق خدماتها.", "Innovest Holding announced a new strategic partnership agreement aimed at strengthening digital transformation and expanding its services."],
            ["تعلن منصة التدريب والتعليم عن إطلاق برنامج تدريبي شامل يستهدف رواد الأعمال الطموحين مع خبراء دوليين.", "The Training & Education platform announces a comprehensive program for ambitious entrepreneurs, delivered with international experts."],
            ["اختُتمت فعاليات معرض إنوفست الدولي بمشاركة أكثر من 200 شركة ومؤسسة من 30 دولة حول العالم.", "The exhibition concluded with more than 200 companies and institutions from 30 countries taking part."],
        ];

        private static readonly string[][] NewsCategories = [["شراكات", "Partnerships"], ["تدريب", "Training"], ["فعاليات", "Events"]];

        private static readonly string[][] NewsDates = [["25 مايو 2026", "May 25, 2026"], ["18 مايو 2026", "May 18, 2026"], ["10 مايو 2026", "May 10, 2026"]];

        private static readonly string[][] EventTitles =
        [
            ["مؤتمر إنوفست للاستثمار والابتكار 2026", "Innovest Investment & Innovation Conference 2026"],
            ["ورشة عمل: بناء المشاريع الناشئة من الصفر", "Workshop: Building Start-ups from Scratch"],
            ["معرض إنوفست الدولي للشراكات والتقنية", "Innovest International Partnerships & Technology Expo"],
        ];

        private static readonly string[][] EventDescriptions =
        [
            ["مؤتمر دولي يجمع كبار المستثمرين ورواد الأعمال لمناقشة أحدث اتجاهات الاستثمار وفرص الابتكار.", "An international conference bringing senior investors and entrepreneurs together to discuss the latest investment trends and innovation opportunities."],
            ["ورشة عمل تفاعلية مكثفة تغطي جميع مراحل بناء المشروع الناشئ بدءاً من الفكرة وحتى التمويل.", "An intensive interactive workshop covering every stage of building a start-up, from idea to funding."],
            ["معرض دولي يجمع الشركات والمؤسسات من مختلف القطاعات لاستعراض أحدث التقنيات وبناء شراكات جديدة.", "An international exhibition gathering companies and institutions from many sectors to showcase the latest technologies and build new partnerships."],
        ];

        private static readonly string[][] EventMonths = [["يوليو", "July"], ["أغسطس", "August"], ["سبتمبر", "September"]];

        private static readonly string[][] EventLocations =
        [
            ["فندق الريتز كارلتون - الرياض", "The Ritz-Carlton Hotel - Riyadh"],
            ["مركز إنوفست للتدريب - عمّان", "Innovest Training Center - Amman"],
            ["مركز دبي الدولي للمعارض", "Dubai International Exhibition Centre"],
        ];

        private static readonly string[][] EventTimes =
            [["9:00 ص - 5:00 م", "9:00 AM - 5:00 PM"], ["10:00 ص - 4:00 م", "10:00 AM - 4:00 PM"], ["3 أيام", "3 days"]];

        private static readonly string[][] PartnerNames =
        [
            ["شركة الاتصالات", "Telecom Company"], ["بنك الاستثمار", "Investment Bank"], ["مجموعة التقنية", "Technology Group"],
            ["شركة الطاقة", "Energy Company"], ["مجموعة العقار", "Real Estate Group"], ["شركة الصناعات", "Industries Company"],
            ["بنك التنمية", "Development Bank"], ["مجموعة الأعمال", "Business Group"], ["شركة التأمين", "Insurance Company"],
            ["مؤسسة التعليم", "Education Foundation"],
        ];

        private static readonly string[][] FaqQuestions =
        [
            ["ما هي إنوفست هولدينغ؟", "What is Innovest Holding?"],
            ["كيف يمكنني التسجيل في فعاليات إنوفست؟", "How can I register for Innovest events?"],
            ["ما هي البرامج التدريبية المتاحة؟", "What training programs are available?"],
            ["كيف يمكنني أن أصبح شريكاً أو راعياً لإنوفست؟", "How can I become a partner or sponsor of Innovest?"],
            ["هل توفر إنوفست فرص استثمارية مباشرة؟", "Does Innovest offer direct investment opportunities?"],
            ["أين تقام فعاليات ومعارض إنوفست؟", "Where are Innovest events and exhibitions held?"],
        ];

        private static readonly string[][] FaqAnswers =
        [
            ["إنوفست هولدينغ هي شركة رائدة في مجال الاستثمار والابتكار، تقدم منظومة متكاملة من المنصات والخدمات تشمل التدريب والتعليم، والاستثمار والتشبيك، والفعاليات والمعارض، والشراكات الاستراتيجية. تأسست الشركة بهدف بناء جسور حقيقية بين المستثمرين ورواد الأعمال والشركات.", "Innovest Holding is a leading investment and innovation company offering an integrated ecosystem of platforms and services that includes training and education, investment and networking, events and exhibitions, and strategic partnerships. The company was founded to build real bridges between investors, entrepreneurs and companies."],
            ["يمكنك التسجيل بسهولة عبر نموذج التسجيل السريع في موقعنا، أو من خلال صفحة الفعاليات حيث يتوفر زر تسجيل لكل فعالية. كما يمكنك التواصل مع فريقنا مباشرة عبر البريد الإلكتروني أو الهاتف.", "You can register easily through the quick registration form on our website, or through the events section, where every event has its own registration button. You can also contact our team directly by email or phone."],
            ["نقدم مجموعة متنوعة من البرامج التدريبية في مجالات ريادة الأعمال، الاستثمار، التسويق الرقمي، القيادة والإدارة، التكنولوجيا والابتكار. تُقدم البرامج من قبل خبراء ومتخصصين دوليين ومحليين بأحدث الأساليب التعليمية.", "We offer a wide range of training programs in entrepreneurship, investment, digital marketing, leadership and management, and technology and innovation. The programs are delivered by international and local experts and specialists using the latest teaching methods."],
            ["نرحب بالشراكات مع المؤسسات والشركات الراغبة في التعاون. يمكنك التواصل معنا عبر نموذج الاتصال في الموقع أو إرسال بريد إلكتروني إلى فريق الشراكات. سنقوم بدراسة اقتراحكم والرد في أقرب وقت ممكن.", "We welcome partnerships with institutions and companies that wish to cooperate with us. You can contact us through the contact form on the website or send an email to the partnerships team. We will study your proposal and reply as soon as possible."],
            ["نعم، من خلال منصة المستثمرين والتشبيك، نوفر فرصاً استثمارية متنوعة في قطاعات مختلفة. نعمل على ربط المستثمرين بالمشاريع الواعدة ونقدم الدعم اللازم لضمان نجاح الاستثمارات.", "Yes. Through the Investors & Networking Platform we offer a variety of investment opportunities across different sectors. We connect investors with promising projects and provide the support needed to make investments succeed."],
            ["تُقام فعالياتنا في مختلف المدن الرئيسية في المنطقة العربية، بما في ذلك الرياض، دبي، عمّان، والقاهرة. كما نقدم فعاليات افتراضية عبر الإنترنت تتيح المشاركة من أي مكان في العالم.", "Our events take place in major cities across the Arab region, including Riyadh, Dubai, Amman and Cairo. We also offer virtual online events that allow participation from anywhere in the world."],
        ];

        // ── New tables (seeded only when empty) ──
        private static readonly string[][] InterestOptionRows =
        [
            ["التدريب والتعليم", "Training & Education"], ["الاستثمار والتمويل", "Investment & Financing"],
            ["التشبيك والشراكات", "Networking & Partnerships"], ["الفعاليات والمعارض", "Events & Exhibitions"],
            ["الإعلام والمحتوى الرقمي", "Media & Digital Content"], ["المسابقات", "Competitions"],
        ];

        private static readonly string[][] RegisterBenefitRows =
        [
            ["وصول حصري للفعاليات والمعارض", "Exclusive access to events and exhibitions"],
            ["عروض خاصة على البرامج التدريبية", "Special offers on training programs"],
            ["تحديثات مستمرة عن فرص الاستثمار", "Ongoing updates on investment opportunities"],
            ["تشبيك مع شبكة واسعة من المستثمرين", "Networking with a wide network of investors"],
        ];

        /// <summary>UPDATE t SET En = v.En … WHERE Ar = v.Ar (binary) AND En IS NULL.</summary>
        private static string Translate(string table, string arColumn, string enColumn, string[][] pairs) =>
            $"UPDATE t SET [{enColumn}] = v.En FROM [{table}] t JOIN (VALUES\n{Values(pairs)}\n) v(Ar, En) " +
            $"ON t.[{arColumn}] = v.Ar {Bin} WHERE t.[{enColumn}] IS NULL;";

        /// <summary>Inserts the rows (with SortOrder 1…n) only when the table is empty.</summary>
        private static string SeedIfEmpty(string table, string arColumn, string enColumn, string[][] rows)
        {
            var values = string.Join(",\n", rows.Select((r, i) => $"({L(r[0])}, {L(r[1])}, {i + 1})"));
            return $"IF NOT EXISTS (SELECT 1 FROM [{table}])\n" +
                   $"INSERT INTO [{table}] ([{arColumn}], [{enColumn}], [SortOrder], [IsActive], [CreatedAt])\n" +
                   $"SELECT v.Ar, v.En, v.S, 1, SYSUTCDATETIME() FROM (VALUES\n{values}\n) v(Ar, En, S);";
        }

        private static void UpData(MigrationBuilder mb)
        {
            // 1. Hero keys: hero_title_ar → hero_title (its English = the old hero_title_en text), hero_title_en →
            //    hero_kicker. Guarded: a second run, or a database that already has the new names, changes nothing.
            mb.Sql("""
                UPDATE t SET ContentEn = s.ContentAr FROM SiteContents t JOIN SiteContents s ON s.SectionKey = N'hero_title_en'
                WHERE t.SectionKey = N'hero_title_ar' AND t.ContentEn IS NULL;
                """);
            mb.Sql("""
                UPDATE SiteContents SET SectionKey = N'hero_title' WHERE SectionKey = N'hero_title_ar'
                  AND NOT EXISTS (SELECT 1 FROM SiteContents WHERE SectionKey = N'hero_title');
                """);
            mb.Sql("""
                UPDATE SiteContents SET SectionKey = N'hero_kicker' WHERE SectionKey = N'hero_title_en'
                  AND NOT EXISTS (SELECT 1 FROM SiteContents WHERE SectionKey = N'hero_kicker');
                """);

            // 2. English for existing keys, only where the Arabic is still the original text. contact_address is in
            //    the list with Arabic = English (its seed stores English in ContentAr; ContentAr is NOT changed).
            //    hero_kicker is optional with no English, hero_title got its English in step 1.
            var translatable = Keys.Where(k => k[2] != null && k[0] != "hero_title").ToArray();
            mb.Sql("UPDATE s SET ContentEn = v.En FROM SiteContents s JOIN (VALUES\n" + Values(translatable) +
                   $"\n) v(K, Ar, En) ON s.SectionKey = v.K AND s.ContentAr = v.Ar {Bin} WHERE s.ContentEn IS NULL;");

            // 3. Every key that is missing is inserted (never InsertData: an unconditional INSERT of a key that
            //    already exists would violate IX_SiteContents_SectionKey and stop the app at startup).
            mb.Sql("INSERT INTO SiteContents (SectionKey, ContentAr, ContentEn, UpdatedAt)\n" +
                   "SELECT v.K, v.Ar, v.En, SYSUTCDATETIME() FROM (VALUES\n" + Values(Keys) +
                   "\n) v(K, Ar, En) WHERE NOT EXISTS (SELECT 1 FROM SiteContents s WHERE s.SectionKey = v.K);");

            // 4. Entity English on rows that still carry the original Arabic (tables are empty on a fresh DB → no-op).
            mb.Sql(Translate("NavItems", "TitleAr", "TitleEn", NavItemTitles));
            mb.Sql("UPDATE NavItems SET ShowInFooter = 1 WHERE Href IN (N'#hero', N'#about', N'#platforms', N'#news', N'#events');");
            mb.Sql(Translate("NavSubItems", "TitleAr", "TitleEn", PlatformTitles));
            mb.Sql(Translate("Platforms", "TitleAr", "TitleEn", PlatformTitles));
            mb.Sql(Translate("Platforms", "DescriptionAr", "DescriptionEn", PlatformDescriptions));
            mb.Sql("UPDATE p SET ShortTitleAr = v.SAr, ShortTitleEn = v.SEn FROM Platforms p JOIN (VALUES\n" +
                   Values(PlatformShortTitles) + $"\n) v(Ar, SAr, SEn) ON p.TitleAr = v.Ar {Bin} WHERE p.ShortTitleAr IS NULL;");
            mb.Sql($"UPDATE Platforms SET ShowInFooter = 0 WHERE TitleAr = {L("الفعاليات والمعارض")} {Bin};");
            mb.Sql(Translate("Stats", "LabelAr", "LabelEn", StatLabels));
            mb.Sql($"UPDATE Stats SET ShortLabelAr = {L("مستثمر")}, ShortLabelEn = N'Investors' " +
                   $"WHERE LabelAr = {L("مستثمر نشط")} {Bin} AND ShortLabelAr IS NULL;");
            mb.Sql(Translate("News", "TitleAr", "TitleEn", NewsTitles));
            mb.Sql(Translate("News", "SummaryAr", "SummaryEn", NewsSummaries));
            mb.Sql(Translate("News", "CategoryAr", "CategoryEn", NewsCategories));
            mb.Sql(Translate("News", "NewsDateAr", "NewsDateEn", NewsDates));
            // Today's order is creation order (the three seed rows were inserted oldest = newest-dated first).
            mb.Sql("UPDATE n SET SortOrder = r.rn FROM News n JOIN (SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) rn FROM News) r ON r.Id = n.Id;");
            mb.Sql(Translate("Events", "TitleAr", "TitleEn", EventTitles));
            mb.Sql(Translate("Events", "DescriptionAr", "DescriptionEn", EventDescriptions));
            mb.Sql(Translate("Events", "EventMonthAr", "EventMonthEn", EventMonths));
            mb.Sql(Translate("Events", "LocationAr", "LocationEn", EventLocations));
            mb.Sql(Translate("Events", "TimeAr", "TimeEn", EventTimes));
            mb.Sql(Translate("Partners", "NameAr", "NameEn", PartnerNames));
            mb.Sql(Translate("Faqs", "QuestionAr", "QuestionEn", FaqQuestions));
            mb.Sql(Translate("Faqs", "AnswerAr", "AnswerEn", FaqAnswers));

            // 5. New tables: the page's literal lists (social links have no URL yet → not rendered).
            mb.Sql(SeedIfEmpty("InterestOptions", "NameAr", "NameEn", InterestOptionRows));
            mb.Sql(SeedIfEmpty("RegisterBenefits", "TextAr", "TextEn", RegisterBenefitRows));
            mb.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [SocialLinks])
                INSERT INTO [SocialLinks] ([Network], [Url], [SortOrder], [IsActive], [CreatedAt]) VALUES
                (N'linkedin', NULL, 1, 1, SYSUTCDATETIME()), (N'twitter', NULL, 2, 1, SYSUTCDATETIME()),
                (N'instagram', NULL, 3, 1, SYSUTCDATETIME()), (N'youtube', NULL, 4, 1, SYSUTCDATETIME());
                """);
        }

        /// <summary>
        /// Before the schema is reverted: drop the keys this migration added and give the hero keys their old names.
        /// Down discards all English text — the real rollback is the database backup (deploy note).
        /// </summary>
        private static void DownData(MigrationBuilder mb)
        {
            var keep = string.Join(", ", OriginalKeys.Select(L));
            mb.Sql($"DELETE FROM SiteContents WHERE SectionKey NOT IN ({keep}) AND SectionKey <> N'sys_timestamps_utc';");
            mb.Sql("""
                UPDATE SiteContents SET SectionKey = N'hero_title_en', ContentEn = NULL WHERE SectionKey = N'hero_kicker'
                  AND NOT EXISTS (SELECT 1 FROM SiteContents WHERE SectionKey = N'hero_title_en');
                """);
            mb.Sql("""
                UPDATE SiteContents SET SectionKey = N'hero_title_ar', ContentEn = NULL WHERE SectionKey = N'hero_title'
                  AND NOT EXISTS (SELECT 1 FROM SiteContents WHERE SectionKey = N'hero_title_ar');
                """);
            mb.Sql("UPDATE SiteContents SET ContentEn = NULL;");
        }
    }
}
