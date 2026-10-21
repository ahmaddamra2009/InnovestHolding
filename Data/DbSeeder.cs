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
        var contents = new List<SiteContent>
        {
            new() { SectionKey = "hero_title_ar",  ContentAr = "إنوفست هولدينغ" },
            new() { SectionKey = "hero_title_en",  ContentAr = "INNOVEST HOLDING" },
            new() { SectionKey = "hero_subtitle",  ContentAr = "نبني جسور الاستثمار والابتكار من خلال منصات متكاملة للتدريب والتعليم والاستثمار والفعاليات والشراكات الاستراتيجية" },
            new() { SectionKey = "about_text_1",   ContentAr = "إنوفست هولدينغ هي شركة رائدة في مجال الاستثمار والابتكار، تأسست برؤية طموحة لبناء منظومة متكاملة تجمع بين التدريب والتعليم، والاستثمار، والتشبيك، والفعاليات. نسعى لتمكين رواد الأعمال والمستثمرين والشركات من خلال منصات متخصصة توفر بيئة محفزة للنمو والتطور." },
            new() { SectionKey = "about_text_2",   ContentAr = "نعمل على تقديم حلول مبتكرة تلبي احتياجات السوق المتنامية، مع التركيز على الجودة والاحترافية في كل ما نقدمه من خدمات ومنصات." },
            new() { SectionKey = "about_years",    ContentAr = "+10" },
            new() { SectionKey = "vision_title",   ContentAr = "الرؤية" },
            new() { SectionKey = "vision_text",    ContentAr = "أن نكون المنصة الأولى للاستثمار والابتكار في المنطقة" },
            new() { SectionKey = "mission_title",  ContentAr = "الرسالة" },
            new() { SectionKey = "mission_text",   ContentAr = "تمكين الأفراد والشركات عبر منصات متكاملة ومبتكرة" },
            new() { SectionKey = "values_title",   ContentAr = "القيم" },
            new() { SectionKey = "values_text",    ContentAr = "الابتكار والجودة والشفافية والتميز المستمر" },
            new() { SectionKey = "contact_address", ContentAr = "Salah Salem St, Heliopolis, Cairo" },
            new() { SectionKey = "contact_phone",  ContentAr = "+20 114 141 0808" },
            new() { SectionKey = "contact_email",  ContentAr = "info@innovestholding.com" },
            new() { SectionKey = "contact_hours",  ContentAr = "الأحد - الخميس: 9:00 ص - 5:00 م" },
        };
        db.SiteContents.AddRange(contents);

        // ── Navigation Items ──
        var navItems = new List<NavItem>
        {
            new() { Title = "الرئيسية",       Href = "#hero",      SortOrder = 1 },
            new() { Title = "من نحن",         Href = "#about",     SortOrder = 2 },
            new() { Title = "منصاتنا",        Href = "#platforms", SortOrder = 3, HasDropdown = true },
            new() { Title = "الأخبار",        Href = "#news",      SortOrder = 4 },
            new() { Title = "الفعاليات",      Href = "#events",    SortOrder = 5 },
            new() { Title = "الشركاء والرعاة", Href = "#partners",  SortOrder = 6 },
            new() { Title = "التسجيل السريع",  Href = "#register",  SortOrder = 7 },
            new() { Title = "تواصل معنا",     Href = "#contact",   SortOrder = 8 },
        };
        db.NavItems.AddRange(navItems);
        await db.SaveChangesAsync();

        // ── Nav Sub Items (under "منصاتنا") ──
        var platformsNav = await db.NavItems.FirstAsync(n => n.Title == "منصاتنا");
        var subItems = new List<NavSubItem>
        {
            new() { NavItemId = platformsNav.Id, Title = "التدريب والتعليم",          Href = "#platforms", SortOrder = 1, IconSvg = "<path d=\"M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z\"/><path d=\"M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z\"/>" },
            new() { NavItemId = platformsNav.Id, Title = "منصة المستثمرين والتشبيك",  Href = "#platforms", SortOrder = 2, IconSvg = "<path d=\"M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M23 21v-2a4 4 0 0 0-3-3.87\"/><path d=\"M16 3.13a4 4 0 0 1 0 7.75\"/>" },
            new() { NavItemId = platformsNav.Id, Title = "الإعلام والمحتوى الرقمي",   Href = "#platforms", SortOrder = 3, IconSvg = "<polygon points=\"23 7 16 12 23 17 23 7\"/><rect x=\"1\" y=\"5\" width=\"15\" height=\"14\" rx=\"2\" ry=\"2\"/>" },
            new() { NavItemId = platformsNav.Id, Title = "المسابقات",                Href = "#platforms", SortOrder = 4, IconSvg = "<path d=\"M6 9H4.5a2.5 2.5 0 0 1 0-5C7 4 9 7 12 7s5-3 7.5-3a2.5 2.5 0 0 1 0 5H18\"/><path d=\"M12 7v10\"/><path d=\"M6 21h12a2 2 0 0 0 2-2v-6a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v6a2 2 0 0 0 2 2z\"/>" },
            new() { NavItemId = platformsNav.Id, Title = "الفعاليات والمعارض",        Href = "#platforms", SortOrder = 5, IconSvg = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\" ry=\"2\"/><line x1=\"16\" y1=\"2\" x2=\"16\" y2=\"6\"/><line x1=\"8\" y1=\"2\" x2=\"8\" y2=\"6\"/><line x1=\"3\" y1=\"10\" x2=\"21\" y2=\"10\"/>" },
            new() { NavItemId = platformsNav.Id, Title = "الشراكات الاستراتيجية و B2B", Href = "#platforms", SortOrder = 6, IconSvg = "<circle cx=\"12\" cy=\"12\" r=\"10\"/><line x1=\"2\" y1=\"12\" x2=\"22\" y2=\"12\"/><path d=\"M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z\"/>" },
        };
        db.NavSubItems.AddRange(subItems);

        // ── Events ──
        db.Events.AddRange(
            new Event { Title = "مؤتمر إنوفست للاستثمار والابتكار 2026", Description = "مؤتمر دولي يجمع كبار المستثمرين ورواد الأعمال لمناقشة أحدث اتجاهات الاستثمار وفرص الابتكار.", EventDay = 15, EventMonth = "يوليو", Location = "فندق الريتز كارلتون - الرياض", Time = "9:00 ص - 5:00 م", SortOrder = 1 },
            new Event { Title = "ورشة عمل: بناء المشاريع الناشئة من الصفر", Description = "ورشة عمل تفاعلية مكثفة تغطي جميع مراحل بناء المشروع الناشئ بدءاً من الفكرة وحتى التمويل.", EventDay = 28, EventMonth = "أغسطس", Location = "مركز إنوفست للتدريب - عمّان", Time = "10:00 ص - 4:00 م", SortOrder = 2 },
            new Event { Title = "معرض إنوفست الدولي للشراكات والتقنية", Description = "معرض دولي يجمع الشركات والمؤسسات من مختلف القطاعات لاستعراض أحدث التقنيات وبناء شراكات جديدة.", EventDay = 10, EventMonth = "سبتمبر", Location = "مركز دبي الدولي للمعارض", Time = "3 أيام", SortOrder = 3 }
        );

        // ── News ──
        db.News.AddRange(
            new News { Title = "إنوفست هولدينغ توقع شراكة استراتيجية مع كبرى شركات التكنولوجيا", Summary = "أعلنت إنوفست هولدينغ عن توقيع اتفاقية شراكة استراتيجية جديدة تهدف إلى تعزيز التحول الرقمي وتوسيع نطاق خدماتها.", Category = "شراكات", NewsDate = "25 مايو 2026" },
            new News { Title = "إطلاق برنامج تدريبي جديد في ريادة الأعمال والابتكار", Summary = "تعلن منصة التدريب والتعليم عن إطلاق برنامج تدريبي شامل يستهدف رواد الأعمال الطموحين مع خبراء دوليين.", Category = "تدريب", NewsDate = "18 مايو 2026" },
            new News { Title = "نجاح كبير لمعرض إنوفست الدولي للاستثمار 2026", Summary = "اختُتمت فعاليات معرض إنوفست الدولي بمشاركة أكثر من 200 شركة ومؤسسة من 30 دولة حول العالم.", Category = "فعاليات", NewsDate = "10 مايو 2026" }
        );

        // ── Platforms ──
        db.Platforms.AddRange(
            new Platform { Title = "التدريب والتعليم", Description = "برامج تدريبية متخصصة وورش عمل احترافية مصممة لتطوير المهارات وبناء القدرات في مجالات الأعمال والتكنولوجيا والقيادة.", IconSvg = "<path d=\"M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z\"/><path d=\"M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z\"/>", SortOrder = 1 },
            new Platform { Title = "منصة المستثمرين والتشبيك", Description = "منصة فريدة تربط بين المستثمرين ورواد الأعمال والشركات، وتوفر فرص تشبيك استراتيجية وشراكات مثمرة.", IconSvg = "<path d=\"M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M23 21v-2a4 4 0 0 0-3-3.87\"/><path d=\"M16 3.13a4 4 0 0 1 0 7.75\"/>", SortOrder = 2 },
            new Platform { Title = "الإعلام والمحتوى الرقمي", Description = "إنتاج محتوى رقمي إبداعي ومتميز، يشمل الفيديو والبودكاست والمقالات المتخصصة في عالم الأعمال والاستثمار.", IconSvg = "<polygon points=\"23 7 16 12 23 17 23 7\"/><rect x=\"1\" y=\"5\" width=\"15\" height=\"14\" rx=\"2\" ry=\"2\"/>", SortOrder = 3 },
            new Platform { Title = "المسابقات", Description = "مسابقات وتحديات مبتكرة تستهدف رواد الأعمال والمبدعين، مع جوائز قيمة وفرص حقيقية لتحويل الأفكار إلى مشاريع ناجحة.", IconSvg = "<path d=\"M6 9H4.5a2.5 2.5 0 0 1 0-5C7 4 9 7 12 7s5-3 7.5-3a2.5 2.5 0 0 1 0 5H18\"/><path d=\"M12 7v10\"/><path d=\"M6 21h12a2 2 0 0 0 2-2v-6a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v6a2 2 0 0 0 2 2z\"/>", SortOrder = 4 },
            new Platform { Title = "الفعاليات والمعارض", Description = "تنظيم وإدارة فعاليات ومعارض دولية ومحلية تجمع بين أصحاب القرار والمستثمرين ورواد الأعمال في بيئة محفزة.", IconSvg = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\" ry=\"2\"/><line x1=\"16\" y1=\"2\" x2=\"16\" y2=\"6\"/><line x1=\"8\" y1=\"2\" x2=\"8\" y2=\"6\"/><line x1=\"3\" y1=\"10\" x2=\"21\" y2=\"10\"/>", SortOrder = 5 },
            new Platform { Title = "الشراكات الاستراتيجية و B2B", Description = "بناء شراكات استراتيجية مع مؤسسات وشركات محلية ودولية، وتوفير حلول أعمال متكاملة لتعزيز النمو المشترك.", IconSvg = "<circle cx=\"12\" cy=\"12\" r=\"10\"/><line x1=\"2\" y1=\"12\" x2=\"22\" y2=\"12\"/><path d=\"M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z\"/>", SortOrder = 6 }
        );

        // ── Partners ──
        var partnerNames = new[] { "شركة الاتصالات", "بنك الاستثمار", "مجموعة التقنية", "شركة الطاقة", "مجموعة العقار", "شركة الصناعات", "بنك التنمية", "مجموعة الأعمال", "شركة التأمين", "مؤسسة التعليم" };
        for (int i = 0; i < partnerNames.Length; i++)
            db.Partners.Add(new Partner { Name = partnerNames[i], SortOrder = i + 1 });

        // ── Stats ──
        db.Stats.AddRange(
            new Stat { Label = "شريك استراتيجي", Value = 150, Suffix = "+", SortOrder = 1, IconSvg = "<path d=\"M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M23 21v-2a4 4 0 0 0-3-3.87\"/><path d=\"M16 3.13a4 4 0 0 1 0 7.75\"/>" },
            new Stat { Label = "مستثمر نشط", Value = 2500, Suffix = "+", SortOrder = 2, IconSvg = "<line x1=\"12\" y1=\"1\" x2=\"12\" y2=\"23\"/><path d=\"M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6\"/>" },
            new Stat { Label = "فعالية سنوياً", Value = 80, Suffix = "+", SortOrder = 3, IconSvg = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\" ry=\"2\"/><line x1=\"16\" y1=\"2\" x2=\"16\" y2=\"6\"/><line x1=\"8\" y1=\"2\" x2=\"8\" y2=\"6\"/><line x1=\"3\" y1=\"10\" x2=\"21\" y2=\"10\"/>" },
            new Stat { Label = "متدرب ومستفيد", Value = 5000, Suffix = "+", SortOrder = 4, IconSvg = "<path d=\"M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z\"/><path d=\"M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z\"/>" }
        );

        // ── FAQ ──
        db.Faqs.AddRange(
            new Faq { Question = "ما هي إنوفست هولدينغ؟", Answer = "إنوفست هولدينغ هي شركة رائدة في مجال الاستثمار والابتكار، تقدم منظومة متكاملة من المنصات والخدمات تشمل التدريب والتعليم، والاستثمار والتشبيك، والفعاليات والمعارض، والشراكات الاستراتيجية. تأسست الشركة بهدف بناء جسور حقيقية بين المستثمرين ورواد الأعمال والشركات.", SortOrder = 1 },
            new Faq { Question = "كيف يمكنني التسجيل في فعاليات إنوفست؟", Answer = "يمكنك التسجيل بسهولة عبر نموذج التسجيل السريع في موقعنا، أو من خلال صفحة الفعاليات حيث يتوفر زر تسجيل لكل فعالية. كما يمكنك التواصل مع فريقنا مباشرة عبر البريد الإلكتروني أو الهاتف.", SortOrder = 2 },
            new Faq { Question = "ما هي البرامج التدريبية المتاحة؟", Answer = "نقدم مجموعة متنوعة من البرامج التدريبية في مجالات ريادة الأعمال، الاستثمار، التسويق الرقمي، القيادة والإدارة، التكنولوجيا والابتكار. تُقدم البرامج من قبل خبراء ومتخصصين دوليين ومحليين بأحدث الأساليب التعليمية.", SortOrder = 3 },
            new Faq { Question = "كيف يمكنني أن أصبح شريكاً أو راعياً لإنوفست؟", Answer = "نرحب بالشراكات مع المؤسسات والشركات الراغبة في التعاون. يمكنك التواصل معنا عبر نموذج الاتصال في الموقع أو إرسال بريد إلكتروني إلى فريق الشراكات. سنقوم بدراسة اقتراحكم والرد في أقرب وقت ممكن.", SortOrder = 4 },
            new Faq { Question = "هل توفر إنوفست فرص استثمارية مباشرة؟", Answer = "نعم، من خلال منصة المستثمرين والتشبيك، نوفر فرصاً استثمارية متنوعة في قطاعات مختلفة. نعمل على ربط المستثمرين بالمشاريع الواعدة ونقدم الدعم اللازم لضمان نجاح الاستثمارات.", SortOrder = 5 },
            new Faq { Question = "أين تقام فعاليات ومعارض إنوفست؟", Answer = "تُقام فعالياتنا في مختلف المدن الرئيسية في المنطقة العربية، بما في ذلك الرياض، دبي، عمّان، والقاهرة. كما نقدم فعاليات افتراضية عبر الإنترنت تتيح المشاركة من أي مكان في العالم.", SortOrder = 6 }
        );

        await db.SaveChangesAsync();
    }
}
