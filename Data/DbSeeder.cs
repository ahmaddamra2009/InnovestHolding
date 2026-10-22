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

        // ── Navigation Items ──
        var navItems = new List<NavItem>
        {
            new() { TitleAr = "الرئيسية",       TitleEn = "Home",               Href = "#hero",      SortOrder = 1, ShowInFooter = true },
            new() { TitleAr = "من نحن",         TitleEn = "About Us",           Href = "#about",     SortOrder = 2, ShowInFooter = true },
            new() { TitleAr = "منصاتنا",        TitleEn = "Our Platforms",      Href = "#platforms", SortOrder = 3, HasDropdown = true, ShowInFooter = true },
            new() { TitleAr = "الأخبار",        TitleEn = "News",               Href = "#news",      SortOrder = 4, ShowInFooter = true },
            new() { TitleAr = "الفعاليات",      TitleEn = "Events",             Href = "#events",    SortOrder = 5, ShowInFooter = true },
            new() { TitleAr = "الشركاء والرعاة", TitleEn = "Partners & Sponsors", Href = "#partners",  SortOrder = 6 },
            new() { TitleAr = "التسجيل السريع",  TitleEn = "Quick Registration", Href = "#register",  SortOrder = 7 },
            new() { TitleAr = "تواصل معنا",     TitleEn = "Contact Us",         Href = "#contact",   SortOrder = 8 },
        };
        db.NavItems.AddRange(navItems);
        await db.SaveChangesAsync();

        // ── Nav Sub Items (under "منصاتنا") ──
        var platformsNav = await db.NavItems.FirstAsync(n => n.TitleAr == "منصاتنا");
        var subItems = new List<NavSubItem>
        {
            new() { NavItemId = platformsNav.Id, TitleAr = "التدريب والتعليم", TitleEn = "Training & Education", Href = "#platforms", SortOrder = 1, IconSvg = "<path d=\"M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z\"/><path d=\"M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z\"/>" },
            new() { NavItemId = platformsNav.Id, TitleAr = "منصة المستثمرين والتشبيك", TitleEn = "Investors & Networking Platform", Href = "#platforms", SortOrder = 2, IconSvg = "<path d=\"M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M23 21v-2a4 4 0 0 0-3-3.87\"/><path d=\"M16 3.13a4 4 0 0 1 0 7.75\"/>" },
            new() { NavItemId = platformsNav.Id, TitleAr = "الإعلام والمحتوى الرقمي", TitleEn = "Media & Digital Content", Href = "#platforms", SortOrder = 3, IconSvg = "<polygon points=\"23 7 16 12 23 17 23 7\"/><rect x=\"1\" y=\"5\" width=\"15\" height=\"14\" rx=\"2\" ry=\"2\"/>" },
            new() { NavItemId = platformsNav.Id, TitleAr = "المسابقات", TitleEn = "Competitions", Href = "#platforms", SortOrder = 4, IconSvg = "<path d=\"M6 9H4.5a2.5 2.5 0 0 1 0-5C7 4 9 7 12 7s5-3 7.5-3a2.5 2.5 0 0 1 0 5H18\"/><path d=\"M12 7v10\"/><path d=\"M6 21h12a2 2 0 0 0 2-2v-6a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v6a2 2 0 0 0 2 2z\"/>" },
            new() { NavItemId = platformsNav.Id, TitleAr = "الفعاليات والمعارض", TitleEn = "Events & Exhibitions", Href = "#platforms", SortOrder = 5, IconSvg = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\" ry=\"2\"/><line x1=\"16\" y1=\"2\" x2=\"16\" y2=\"6\"/><line x1=\"8\" y1=\"2\" x2=\"8\" y2=\"6\"/><line x1=\"3\" y1=\"10\" x2=\"21\" y2=\"10\"/>" },
            new() { NavItemId = platformsNav.Id, TitleAr = "الشراكات الاستراتيجية و B2B", TitleEn = "Strategic Partnerships & B2B", Href = "#platforms", SortOrder = 6, IconSvg = "<circle cx=\"12\" cy=\"12\" r=\"10\"/><line x1=\"2\" y1=\"12\" x2=\"22\" y2=\"12\"/><path d=\"M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z\"/>" },
        };
        db.NavSubItems.AddRange(subItems);

        // ── Events ──
        db.Events.AddRange(
            new Event { TitleAr = "مؤتمر إنوفست للاستثمار والابتكار 2026", TitleEn = "Innovest Investment & Innovation Conference 2026", DescriptionAr = "مؤتمر دولي يجمع كبار المستثمرين ورواد الأعمال لمناقشة أحدث اتجاهات الاستثمار وفرص الابتكار.", DescriptionEn = "An international conference bringing senior investors and entrepreneurs together to discuss the latest investment trends and innovation opportunities.", EventDay = 15, EventMonthAr = "يوليو", EventMonthEn = "July", LocationAr = "فندق الريتز كارلتون - الرياض", LocationEn = "The Ritz-Carlton Hotel - Riyadh", TimeAr = "9:00 ص - 5:00 م", TimeEn = "9:00 AM - 5:00 PM", SortOrder = 1 },
            new Event { TitleAr = "ورشة عمل: بناء المشاريع الناشئة من الصفر", TitleEn = "Workshop: Building Start-ups from Scratch", DescriptionAr = "ورشة عمل تفاعلية مكثفة تغطي جميع مراحل بناء المشروع الناشئ بدءاً من الفكرة وحتى التمويل.", DescriptionEn = "An intensive interactive workshop covering every stage of building a start-up, from idea to funding.", EventDay = 28, EventMonthAr = "أغسطس", EventMonthEn = "August", LocationAr = "مركز إنوفست للتدريب - عمّان", LocationEn = "Innovest Training Center - Amman", TimeAr = "10:00 ص - 4:00 م", TimeEn = "10:00 AM - 4:00 PM", SortOrder = 2 },
            new Event { TitleAr = "معرض إنوفست الدولي للشراكات والتقنية", TitleEn = "Innovest International Partnerships & Technology Expo", DescriptionAr = "معرض دولي يجمع الشركات والمؤسسات من مختلف القطاعات لاستعراض أحدث التقنيات وبناء شراكات جديدة.", DescriptionEn = "An international exhibition gathering companies and institutions from many sectors to showcase the latest technologies and build new partnerships.", EventDay = 10, EventMonthAr = "سبتمبر", EventMonthEn = "September", LocationAr = "مركز دبي الدولي للمعارض", LocationEn = "Dubai International Exhibition Centre", TimeAr = "3 أيام", TimeEn = "3 days", SortOrder = 3 }
        );

        // ── News (SortOrder 1…3 = today's order) ──
        db.News.AddRange(
            new News { TitleAr = "إنوفست هولدينغ توقع شراكة استراتيجية مع كبرى شركات التكنولوجيا", TitleEn = "Innovest Holding signs a strategic partnership with leading technology companies", SummaryAr = "أعلنت إنوفست هولدينغ عن توقيع اتفاقية شراكة استراتيجية جديدة تهدف إلى تعزيز التحول الرقمي وتوسيع نطاق خدماتها.", SummaryEn = "Innovest Holding announced a new strategic partnership agreement aimed at strengthening digital transformation and expanding its services.", CategoryAr = "شراكات", CategoryEn = "Partnerships", NewsDateAr = "25 مايو 2026", NewsDateEn = "May 25, 2026", SortOrder = 1 },
            new News { TitleAr = "إطلاق برنامج تدريبي جديد في ريادة الأعمال والابتكار", TitleEn = "Launch of a new training program in entrepreneurship and innovation", SummaryAr = "تعلن منصة التدريب والتعليم عن إطلاق برنامج تدريبي شامل يستهدف رواد الأعمال الطموحين مع خبراء دوليين.", SummaryEn = "The Training & Education platform announces a comprehensive program for ambitious entrepreneurs, delivered with international experts.", CategoryAr = "تدريب", CategoryEn = "Training", NewsDateAr = "18 مايو 2026", NewsDateEn = "May 18, 2026", SortOrder = 2 },
            new News { TitleAr = "نجاح كبير لمعرض إنوفست الدولي للاستثمار 2026", TitleEn = "Great success for the Innovest International Investment Exhibition 2026", SummaryAr = "اختُتمت فعاليات معرض إنوفست الدولي بمشاركة أكثر من 200 شركة ومؤسسة من 30 دولة حول العالم.", SummaryEn = "The exhibition concluded with more than 200 companies and institutions from 30 countries taking part.", CategoryAr = "فعاليات", CategoryEn = "Events", NewsDateAr = "10 مايو 2026", NewsDateEn = "May 10, 2026", SortOrder = 3 }
        );

        // ── Platforms ──
        db.Platforms.AddRange(
            new Platform { TitleAr = "التدريب والتعليم", TitleEn = "Training & Education", DescriptionEn = "Specialized training programs and professional workshops designed to develop skills and build capabilities in business, technology and leadership.", DescriptionAr = "برامج تدريبية متخصصة وورش عمل احترافية مصممة لتطوير المهارات وبناء القدرات في مجالات الأعمال والتكنولوجيا والقيادة.", IconSvg = "<path d=\"M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z\"/><path d=\"M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z\"/>", SortOrder = 1 },
            new Platform { TitleAr = "منصة المستثمرين والتشبيك", TitleEn = "Investors & Networking Platform", DescriptionEn = "A unique platform connecting investors, entrepreneurs and companies, creating strategic networking opportunities and fruitful partnerships.", ShortTitleAr = "المستثمرين والتشبيك", ShortTitleEn = "Investors & Networking", DescriptionAr = "منصة فريدة تربط بين المستثمرين ورواد الأعمال والشركات، وتوفر فرص تشبيك استراتيجية وشراكات مثمرة.", IconSvg = "<path d=\"M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M23 21v-2a4 4 0 0 0-3-3.87\"/><path d=\"M16 3.13a4 4 0 0 1 0 7.75\"/>", SortOrder = 2 },
            new Platform { TitleAr = "الإعلام والمحتوى الرقمي", TitleEn = "Media & Digital Content", DescriptionEn = "Creative, high-quality digital content including video, podcasts and specialized articles on business and investment.", ShortTitleAr = "الإعلام الرقمي", ShortTitleEn = "Digital Media", DescriptionAr = "إنتاج محتوى رقمي إبداعي ومتميز، يشمل الفيديو والبودكاست والمقالات المتخصصة في عالم الأعمال والاستثمار.", IconSvg = "<polygon points=\"23 7 16 12 23 17 23 7\"/><rect x=\"1\" y=\"5\" width=\"15\" height=\"14\" rx=\"2\" ry=\"2\"/>", SortOrder = 3 },
            new Platform { TitleAr = "المسابقات", TitleEn = "Competitions", DescriptionEn = "Innovative competitions and challenges for entrepreneurs and creators, with valuable prizes and real opportunities to turn ideas into successful projects.", DescriptionAr = "مسابقات وتحديات مبتكرة تستهدف رواد الأعمال والمبدعين، مع جوائز قيمة وفرص حقيقية لتحويل الأفكار إلى مشاريع ناجحة.", IconSvg = "<path d=\"M6 9H4.5a2.5 2.5 0 0 1 0-5C7 4 9 7 12 7s5-3 7.5-3a2.5 2.5 0 0 1 0 5H18\"/><path d=\"M12 7v10\"/><path d=\"M6 21h12a2 2 0 0 0 2-2v-6a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v6a2 2 0 0 0 2 2z\"/>", SortOrder = 4 },
            new Platform { TitleAr = "الفعاليات والمعارض", TitleEn = "Events & Exhibitions", DescriptionEn = "Organizing and managing international and local events and exhibitions that bring decision makers, investors and entrepreneurs together in an inspiring environment.", ShowInFooter = false, DescriptionAr = "تنظيم وإدارة فعاليات ومعارض دولية ومحلية تجمع بين أصحاب القرار والمستثمرين ورواد الأعمال في بيئة محفزة.", IconSvg = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\" ry=\"2\"/><line x1=\"16\" y1=\"2\" x2=\"16\" y2=\"6\"/><line x1=\"8\" y1=\"2\" x2=\"8\" y2=\"6\"/><line x1=\"3\" y1=\"10\" x2=\"21\" y2=\"10\"/>", SortOrder = 5 },
            new Platform { TitleAr = "الشراكات الاستراتيجية و B2B", TitleEn = "Strategic Partnerships & B2B", DescriptionEn = "Building strategic partnerships with local and international institutions and companies, and providing integrated business solutions for shared growth.", ShortTitleAr = "الشراكات و B2B", ShortTitleEn = "Partnerships & B2B", DescriptionAr = "بناء شراكات استراتيجية مع مؤسسات وشركات محلية ودولية، وتوفير حلول أعمال متكاملة لتعزيز النمو المشترك.", IconSvg = "<circle cx=\"12\" cy=\"12\" r=\"10\"/><line x1=\"2\" y1=\"12\" x2=\"22\" y2=\"12\"/><path d=\"M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z\"/>", SortOrder = 6 }
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

        // ── Stats ──
        db.Stats.AddRange(
            new Stat { LabelAr = "شريك استراتيجي", LabelEn = "Strategic partners", Value = 150, Suffix = "+", SortOrder = 1, IconSvg = "<path d=\"M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M23 21v-2a4 4 0 0 0-3-3.87\"/><path d=\"M16 3.13a4 4 0 0 1 0 7.75\"/>" },
            new Stat { LabelAr = "مستثمر نشط", LabelEn = "Active investors", ShortLabelAr = "مستثمر", ShortLabelEn = "Investors", Value = 2500, Suffix = "+", SortOrder = 2, IconSvg = "<line x1=\"12\" y1=\"1\" x2=\"12\" y2=\"23\"/><path d=\"M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6\"/>" },
            new Stat { LabelAr = "فعالية سنوياً", LabelEn = "Events per year", Value = 80, Suffix = "+", SortOrder = 3, IconSvg = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\" ry=\"2\"/><line x1=\"16\" y1=\"2\" x2=\"16\" y2=\"6\"/><line x1=\"8\" y1=\"2\" x2=\"8\" y2=\"6\"/><line x1=\"3\" y1=\"10\" x2=\"21\" y2=\"10\"/>" },
            new Stat { LabelAr = "متدرب ومستفيد", LabelEn = "Trainees and beneficiaries", Value = 5000, Suffix = "+", SortOrder = 4, IconSvg = "<path d=\"M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z\"/><path d=\"M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z\"/>" }
        );

        // ── FAQ ──
        db.Faqs.AddRange(
            new Faq { QuestionAr = "ما هي إنوفست هولدينغ؟", QuestionEn = "What is Innovest Holding?", AnswerAr = "إنوفست هولدينغ هي شركة رائدة في مجال الاستثمار والابتكار، تقدم منظومة متكاملة من المنصات والخدمات تشمل التدريب والتعليم، والاستثمار والتشبيك، والفعاليات والمعارض، والشراكات الاستراتيجية. تأسست الشركة بهدف بناء جسور حقيقية بين المستثمرين ورواد الأعمال والشركات.", AnswerEn = "Innovest Holding is a leading investment and innovation company offering an integrated ecosystem of platforms and services that includes training and education, investment and networking, events and exhibitions, and strategic partnerships. The company was founded to build real bridges between investors, entrepreneurs and companies.", SortOrder = 1 },
            new Faq { QuestionAr = "كيف يمكنني التسجيل في فعاليات إنوفست؟", QuestionEn = "How can I register for Innovest events?", AnswerAr = "يمكنك التسجيل بسهولة عبر نموذج التسجيل السريع في موقعنا، أو من خلال صفحة الفعاليات حيث يتوفر زر تسجيل لكل فعالية. كما يمكنك التواصل مع فريقنا مباشرة عبر البريد الإلكتروني أو الهاتف.", AnswerEn = "You can register easily through the quick registration form on our website, or through the events section, where every event has its own registration button. You can also contact our team directly by email or phone.", SortOrder = 2 },
            new Faq { QuestionAr = "ما هي البرامج التدريبية المتاحة؟", QuestionEn = "What training programs are available?", AnswerAr = "نقدم مجموعة متنوعة من البرامج التدريبية في مجالات ريادة الأعمال، الاستثمار، التسويق الرقمي، القيادة والإدارة، التكنولوجيا والابتكار. تُقدم البرامج من قبل خبراء ومتخصصين دوليين ومحليين بأحدث الأساليب التعليمية.", AnswerEn = "We offer a wide range of training programs in entrepreneurship, investment, digital marketing, leadership and management, and technology and innovation. The programs are delivered by international and local experts and specialists using the latest teaching methods.", SortOrder = 3 },
            new Faq { QuestionAr = "كيف يمكنني أن أصبح شريكاً أو راعياً لإنوفست؟", QuestionEn = "How can I become a partner or sponsor of Innovest?", AnswerAr = "نرحب بالشراكات مع المؤسسات والشركات الراغبة في التعاون. يمكنك التواصل معنا عبر نموذج الاتصال في الموقع أو إرسال بريد إلكتروني إلى فريق الشراكات. سنقوم بدراسة اقتراحكم والرد في أقرب وقت ممكن.", AnswerEn = "We welcome partnerships with institutions and companies that wish to cooperate with us. You can contact us through the contact form on the website or send an email to the partnerships team. We will study your proposal and reply as soon as possible.", SortOrder = 4 },
            new Faq { QuestionAr = "هل توفر إنوفست فرص استثمارية مباشرة؟", QuestionEn = "Does Innovest offer direct investment opportunities?", AnswerAr = "نعم، من خلال منصة المستثمرين والتشبيك، نوفر فرصاً استثمارية متنوعة في قطاعات مختلفة. نعمل على ربط المستثمرين بالمشاريع الواعدة ونقدم الدعم اللازم لضمان نجاح الاستثمارات.", AnswerEn = "Yes. Through the Investors & Networking Platform we offer a variety of investment opportunities across different sectors. We connect investors with promising projects and provide the support needed to make investments succeed.", SortOrder = 5 },
            new Faq { QuestionAr = "أين تقام فعاليات ومعارض إنوفست؟", QuestionEn = "Where are Innovest events and exhibitions held?", AnswerAr = "تُقام فعالياتنا في مختلف المدن الرئيسية في المنطقة العربية، بما في ذلك الرياض، دبي، عمّان، والقاهرة. كما نقدم فعاليات افتراضية عبر الإنترنت تتيح المشاركة من أي مكان في العالم.", AnswerEn = "Our events take place in major cities across the Arab region, including Riyadh, Dubai, Amman and Cairo. We also offer virtual online events that allow participation from anywhere in the world.", SortOrder = 6 }
        );

        await db.SaveChangesAsync();
    }
}
