using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync()) return;

        // ── Admin User ──
        db.Users.Add(new AppUser
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@$$w0rd20216@#$")
        });

        // ── Site Content ──
        // Not seeded here: the migration AddBilingualContent inserts every SiteContents key (Ar + En) on fresh AND
        // existing databases. Seeding them here too would collide with IX_SiteContents_SectionKey on a fresh install.
        // InterestOptions, RegisterBenefits and SocialLinks are written by the same migration.
        // The English below must equal the migration's literals (test FreshAndUpgradedDatabasesMatch).

        // ── Navigation ──
        // Not seeded: the header and footer menus are generated from the route table (Infrastructure/Site/SiteRoutes)
        // and the Pages rows of the migration AddSiteStructure. NavItems holds only the admin's extra links.

        // ── Events (slugs as the migration AddMediaAndDetailPages gives existing rows: {type}-{id}) ──
        db.Events.AddRange(
            new Event { Slug = "event-1", TitleAr = "مؤتمر إنوفست للاستثمار والابتكار 2026", TitleEn = "Innovest Investment & Innovation Conference 2026", DescriptionAr = "مؤتمر دولي يجمع كبار المستثمرين ورواد الأعمال لمناقشة أحدث اتجاهات الاستثمار وفرص الابتكار.", DescriptionEn = "An international conference bringing senior investors and entrepreneurs together to discuss the latest investment trends and innovation opportunities.", EventDay = 15, EventMonthAr = "يوليو", EventMonthEn = "July", LocationAr = "فندق الريتز كارلتون - الرياض", LocationEn = "The Ritz-Carlton Hotel - Riyadh", TimeAr = "9:00 ص - 5:00 م", TimeEn = "9:00 AM - 5:00 PM", SortOrder = 1 },
            new Event { Slug = "event-2", TitleAr = "ورشة عمل: بناء المشاريع الناشئة من الصفر", TitleEn = "Workshop: Building Start-ups from Scratch", DescriptionAr = "ورشة عمل تفاعلية مكثفة تغطي جميع مراحل بناء المشروع الناشئ بدءاً من الفكرة وحتى التمويل.", DescriptionEn = "An intensive interactive workshop covering every stage of building a start-up, from idea to funding.", EventDay = 28, EventMonthAr = "أغسطس", EventMonthEn = "August", LocationAr = "مركز إنوفست للتدريب - عمّان", LocationEn = "Innovest Training Center - Amman", TimeAr = "10:00 ص - 4:00 م", TimeEn = "10:00 AM - 4:00 PM", SortOrder = 2 },
            new Event { Slug = "event-3", TitleAr = "معرض إنوفست الدولي للشراكات والتقنية", TitleEn = "Innovest International Partnerships & Technology Expo", DescriptionAr = "معرض دولي يجمع الشركات والمؤسسات من مختلف القطاعات لاستعراض أحدث التقنيات وبناء شراكات جديدة.", DescriptionEn = "An international exhibition gathering companies and institutions from many sectors to showcase the latest technologies and build new partnerships.", EventDay = 10, EventMonthAr = "سبتمبر", EventMonthEn = "September", LocationAr = "مركز دبي الدولي للمعارض", LocationEn = "Dubai International Exhibition Centre", TimeAr = "3 أيام", TimeEn = "3 days", SortOrder = 3 }
        );

        // ── News (SortOrder 1…3 = today's order) ──
        db.News.AddRange(
            new News { Slug = "news-1", TitleAr = "إنوفست القابضة توقع شراكة استراتيجية مع كبرى شركات التكنولوجيا", TitleEn = "Innovest Holding signs a strategic partnership with leading technology companies", SummaryAr = "أعلنت إنوفست القابضة عن توقيع اتفاقية شراكة استراتيجية جديدة تهدف إلى تعزيز التحول الرقمي وتوسيع نطاق خدماتها.", SummaryEn = "Innovest Holding announced a new strategic partnership agreement aimed at strengthening digital transformation and expanding its services.", CategoryAr = "شراكات", CategoryEn = "Partnerships", NewsDateAr = "25 مايو 2026", NewsDateEn = "May 25, 2026", SortOrder = 1 },
            new News { Slug = "news-2", TitleAr = "إطلاق برنامج تدريبي جديد في ريادة الأعمال والابتكار", TitleEn = "Launch of a new training program in entrepreneurship and innovation", SummaryAr = "تعلن منصة التدريب والتعليم عن إطلاق برنامج تدريبي شامل يستهدف رواد الأعمال الطموحين مع خبراء دوليين.", SummaryEn = "The Training & Education platform announces a comprehensive program for ambitious entrepreneurs, delivered with international experts.", CategoryAr = "تدريب", CategoryEn = "Training", NewsDateAr = "18 مايو 2026", NewsDateEn = "May 18, 2026", SortOrder = 2 },
            new News { Slug = "news-3", TitleAr = "نجاح كبير لمعرض إنوفست الدولي للاستثمار 2026", TitleEn = "Great success for the Innovest International Investment Exhibition 2026", SummaryAr = "اختُتمت فعاليات معرض إنوفست الدولي بمشاركة أكثر من 200 شركة ومؤسسة من 30 دولة حول العالم.", SummaryEn = "The exhibition concluded with more than 200 companies and institutions from 30 countries taking part.", CategoryAr = "فعاليات", CategoryEn = "Events", NewsDateAr = "10 مايو 2026", NewsDateEn = "May 10, 2026", SortOrder = 3 }
        );

        // ── Platforms = «Our services» of the company profile (the same literals as the migration ApplyCompanyProfile) ──
        db.Platforms.AddRange(
            new Platform { TitleAr = "تطوير الشركات الناشئة", TitleEn = "Startup Development", DescriptionAr = "احتضان الأفكار الناشئة\nتطوير نماذج الأعمال\nإعداد العرض التقديمي (Pitch Deck)\nدراسة الجدوى\nالإرشاد والتوجيه (Mentorship & Business Coaching)", DescriptionEn = "Incubating early-stage ideas\nBusiness model development\nPitch deck preparation\nFeasibility studies\nMentorship and business coaching", IconSvg = SeedIcons.Rocket, SortOrder = 1 },
            new Platform { TitleAr = "خدمات الاستثمار", TitleEn = "Investment Services", DescriptionAr = "جذب المستثمرين\nالتشبيك مع المستثمرين الملائكيين\nالتشبيك مع صناديق الاستثمار\nتجهيز الشركات للاستثمار\nتقييم الشركات الناشئة\nإدارة جولات الاستثمار", DescriptionEn = "Attracting investors\nConnecting with angel investors\nConnecting with investment funds\nPreparing companies for investment\nStartup valuation\nManaging investment rounds", IconSvg = SeedIcons.Chart, SortOrder = 2 },
            new Platform { TitleAr = "تطوير الأعمال", TitleEn = "Business Development", DescriptionAr = "الاستشارات\nتطوير الأعمال\nالتوسع الإقليمي\nدخول الأسواق\nتطوير الاستراتيجيات", DescriptionEn = "Consulting\nBusiness development\nRegional expansion\nMarket entry\nStrategy development", IconSvg = SeedIcons.Briefcase, SortOrder = 3 },
            new Platform { TitleAr = "الابتكار المؤسسي", TitleEn = "Corporate Innovation", DescriptionAr = "دعم الشركات الكبرى في الابتكار المؤسسي\nالتحول الرقمي\nالابتكار المفتوح\nالتعاون مع الشركات الناشئة", DescriptionEn = "Supporting large companies in corporate innovation\nDigital transformation\nOpen innovation\nCollaboration with startups", IconSvg = SeedIcons.Layers, SortOrder = 4 },
            new Platform { TitleAr = "منصة إنوفست", TitleEn = "Innovest Platform", DescriptionAr = "بوابة رواد الأعمال\nبوابة المستثمرين\nقاعدة بيانات الشركات الناشئة\nفرص التمويل\nإدارة الفعاليات\nنظام التوفيق (Matchmaking)\nالاستشارات والتوجيه\nأكاديمية التعلم\nمجتمع إنوفست لرواد الأعمال\nسوق الخدمات الإلكتروني", DescriptionEn = "Entrepreneur Portal\nInvestor Portal\nStartup Database\nFunding Opportunities\nEvent Management\nMatchmaking System\nMentorship Platform\nLearning Academy\nInnovest Community\nServices Marketplace", IconSvg = SeedIcons.Cpu, SortOrder = 5 },
            new Platform { TitleAr = "منتدى الإبتكار والإستثمار العربي", TitleEn = "Innovest Arab Forum", DescriptionAr = "معرض إنوفست للشركات الناشئة\nمنتدى الإبتكار والإستثمار العربي\nملتقى الذكاء الاصطناعي\nمسابقة العروض التقديمية\nاجتماعات المستثمرين\nالتشبيك بين الشركات\nجلسات حكومية\nجوائز الابتكار", DescriptionEn = "Innovest Startup Expo\nInnovest Arab Forum\nAI Summit\nPitch Competition\nInvestment Meetings\nBusiness Matching\nGovernment Sessions\nInnovation Awards", IconSvg = SeedIcons.Mic, SortOrder = 6 }
        );

        // ── Partners ──
        (string Ar, string En)[] partnerNames =
        [
            ("شركة الاتصالات", "Telecom Company"), ("بنك الاستثمار", "Investment Bank"), ("مجموعة التقنية", "Technology Group"),
            ("شركة الطاقة", "Energy Company"), ("مجموعة العقار", "Real Estate Group"), ("شركة الصناعات", "Industries Company"),
            ("بنك التنمية", "Development Bank"), ("مجموعة الأعمال", "Business Group"), ("شركة التأمين", "Insurance Company"),
            ("مؤسسة التعليم", "Education Foundation")
        ];
        for (int i = 0; i < partnerNames.Length; i++)
            db.Partners.Add(new Partner { NameAr = partnerNames[i].Ar, NameEn = partnerNames[i].En, SortOrder = i + 1 });

        // ── Stats = «Expected impact» of the company profile ──
        db.Stats.AddRange(
            new Stat { LabelAr = "رائد أعمال", LabelEn = "Entrepreneurs", Value = 100000, Suffix = "+", SortOrder = 1, IconSvg = SeedIcons.Rocket },
            new Stat { LabelAr = "شركة ناشئة", LabelEn = "Startups", Value = 10000, Suffix = "+", SortOrder = 2, IconSvg = SeedIcons.Chart },
            new Stat { LabelAr = "مستثمر", LabelEn = "Investors", Value = 5000, Suffix = "+", SortOrder = 3, IconSvg = SeedIcons.Users },
            new Stat { LabelAr = "صندوق استثمار", LabelEn = "Investment funds", Value = 500, Suffix = "+", SortOrder = 4, IconSvg = SeedIcons.Dollar },
            new Stat { LabelAr = "دولة عربية", LabelEn = "Arab countries", Value = 22, Suffix = "", SortOrder = 5, IconSvg = SeedIcons.Globe },
            new Stat { LabelAr = "شريك استراتيجي", LabelEn = "Strategic partners", Value = 100, Suffix = "+", SortOrder = 6, IconSvg = SeedIcons.Handshake }
        );

        // ── FAQ ──
        db.Faqs.AddRange(
            new Faq { QuestionAr = "ما هي إنوفست القابضة؟", QuestionEn = "What is Innovest Holding?", AnswerAr = "إنوفست القابضة هي شركة رائدة في مجال الاستثمار والابتكار، تقدم منظومة متكاملة من المنصات والخدمات تشمل التدريب والتعليم، والاستثمار والتشبيك، والفعاليات والمعارض، والشراكات الاستراتيجية. تأسست الشركة بهدف بناء جسور حقيقية بين المستثمرين ورواد الأعمال والشركات.", AnswerEn = "Innovest Holding is a leading investment and innovation company offering an integrated ecosystem of platforms and services that includes training and education, investment and networking, events and exhibitions, and strategic partnerships. The company was founded to build real bridges between investors, entrepreneurs and companies.", SortOrder = 1 },
            new Faq { QuestionAr = "كيف يمكنني التسجيل في فعاليات إنوفست؟", QuestionEn = "How can I register for Innovest events?", AnswerAr = "يمكنك التسجيل بسهولة عبر نموذج التسجيل السريع في موقعنا، أو من خلال صفحة الفعاليات حيث يتوفر زر تسجيل لكل فعالية. كما يمكنك التواصل مع فريقنا مباشرة عبر البريد الإلكتروني أو الهاتف.", AnswerEn = "You can register easily through the quick registration form on our website, or through the events section, where every event has its own registration button. You can also contact our team directly by email or phone.", SortOrder = 2 },
            new Faq { QuestionAr = "ما هي البرامج التدريبية المتاحة؟", QuestionEn = "What training programs are available?", AnswerAr = "نقدم مجموعة متنوعة من البرامج التدريبية في مجالات ريادة الأعمال، الاستثمار، التسويق الرقمي، القيادة والإدارة، التكنولوجيا والابتكار. تُقدم البرامج من قبل خبراء ومتخصصين دوليين ومحليين بأحدث الأساليب التعليمية.", AnswerEn = "We offer a wide range of training programs in entrepreneurship, investment, digital marketing, leadership and management, and technology and innovation. The programs are delivered by international and local experts and specialists using the latest teaching methods.", SortOrder = 3 },
            new Faq { QuestionAr = "كيف يمكنني أن أصبح شريكاً أو راعياً لإنوفست؟", QuestionEn = "How can I become a partner or sponsor of Innovest?", AnswerAr = "نرحب بالشراكات مع المؤسسات والشركات الراغبة في التعاون. يمكنك التواصل معنا عبر نموذج الاتصال في الموقع أو إرسال بريد إلكتروني إلى فريق الشراكات. سنقوم بدراسة اقتراحكم والرد في أقرب وقت ممكن.", AnswerEn = "We welcome partnerships with institutions and companies that wish to cooperate with us. You can contact us through the contact form on the website or send an email to the partnerships team. We will study your proposal and reply as soon as possible.", SortOrder = 4 },
            new Faq { QuestionAr = "هل توفر إنوفست فرص استثمارية مباشرة؟", QuestionEn = "Does Innovest offer direct investment opportunities?", AnswerAr = "نعم، من خلال منصة المستثمرين والتشبيك، نوفر فرصاً استثمارية متنوعة في قطاعات مختلفة. نعمل على ربط المستثمرين بالمشاريع الواعدة ونقدم الدعم اللازم لضمان نجاح الاستثمارات.", AnswerEn = "Yes. Through the Investors & Networking Platform we offer a variety of investment opportunities across different sectors. We connect investors with promising projects and provide the support needed to make investments succeed.", SortOrder = 5 },
            new Faq { QuestionAr = "أين تقام فعاليات ومعارض إنوفست؟", QuestionEn = "Where are Innovest events and exhibitions held?", AnswerAr = "تُقام فعالياتنا في مختلف المدن الرئيسية في المنطقة العربية، بما في ذلك الرياض، دبي، عمّان، والقاهرة. كما نقدم فعاليات افتراضية عبر الإنترنت تتيح المشاركة من أي مكان في العالم.", AnswerEn = "Our events take place in major cities across the Arab region, including Riyadh, Dubai, Amman and Cairo. We also offer virtual online events that allow participation from anywhere in the world.", SortOrder = 6 }
        );

        await db.SaveChangesAsync();
    }
}

/// <summary>Icon markup of the seeded Platforms and Stats rows (seeder-owned IconSvg columns; same as the migration).</summary>
internal static class SeedIcons
{
    public const string Users = "<path d=\"M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M23 21v-2a4 4 0 0 0-3-3.87\"/><path d=\"M16 3.13a4 4 0 0 1 0 7.75\"/>";
    public const string Rocket = "<path d=\"M4.5 16.5c-1.5 1.3-2 5-2 5s3.7-.5 5-2c.7-.8.7-2.1-.1-2.9a2.2 2.2 0 0 0-2.9-.1z\"/><path d=\"m12 15-3-3a22 22 0 0 1 2-4A12.9 12.9 0 0 1 22 2c0 2.7-.8 7.5-6 11a22.4 22.4 0 0 1-4 2z\"/><path d=\"M9 12H4s.6-3 2-4c1.6-1.1 5 0 5 0\"/><path d=\"M12 15v5s3-.6 4-2c1.1-1.6 0-5 0-5\"/>";
    public const string Dollar = "<line x1=\"12\" y1=\"1\" x2=\"12\" y2=\"23\"/><path d=\"M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6\"/>";
    public const string Chart = "<polyline points=\"22 7 13.5 15.5 8.5 10.5 2 17\"/><polyline points=\"16 7 22 7 22 13\"/>";
    public const string Globe = "<circle cx=\"12\" cy=\"12\" r=\"10\"/><line x1=\"2\" y1=\"12\" x2=\"22\" y2=\"12\"/><path d=\"M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z\"/>";
    public const string Handshake = "<path d=\"M11 17l2 2a1 1 0 0 0 1.4 0l4-4a1 1 0 0 0 0-1.4L13 8\"/><path d=\"M14 7 9.5 2.5 2 10l5 5\"/><path d=\"m22 10-5-5-3 3\"/><path d=\"m8 14 2 2\"/>";
    public const string Layers = "<polygon points=\"12 2 2 7 12 12 22 7 12 2\"/><polyline points=\"2 17 12 22 22 17\"/><polyline points=\"2 12 12 17 22 12\"/>";
    public const string Briefcase = "<rect x=\"2\" y=\"7\" width=\"20\" height=\"14\" rx=\"2\"/><path d=\"M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16\"/>";
    public const string Cpu = "<rect x=\"4\" y=\"4\" width=\"16\" height=\"16\" rx=\"2\"/><rect x=\"9\" y=\"9\" width=\"6\" height=\"6\"/><path d=\"M9 1v3M15 1v3M9 20v3M15 20v3M20 9h3M20 14h3M1 9h3M1 14h3\"/>";
    public const string Mic = "<rect x=\"9\" y=\"2\" width=\"6\" height=\"12\" rx=\"3\"/><path d=\"M19 10v1a7 7 0 0 1-14 0v-1\"/><line x1=\"12\" y1=\"18\" x2=\"12\" y2=\"22\"/>";
}
