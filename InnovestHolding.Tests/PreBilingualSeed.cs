namespace InnovestHolding.Tests;

/// <summary>
/// The rows the app's seeder wrote BEFORE the bilingual schema (old column names), frozen as literals: generated once
/// from <c>git show 85841fb:Data/DbSeeder.cs</c> (the last commit before the change) and never derived from current code,
/// so a drift between the migration's Arabic literals and what existing databases really contain turns the upgrade
/// tests red. 16 SiteContents, 8 NavItems, 6 NavSubItems, 3 Events, 3 News, 6 Platforms, 10 Partners, 4 Stats, 6 Faqs.
/// No user row (no password in the tests).
/// </summary>
public static class PreBilingualSeed
{
    public const string Sql = """
INSERT INTO SiteContents (SectionKey, ContentAr, UpdatedAt) VALUES (N'hero_title_ar', N'إنوفست هولدينغ', '2026-06-01T00:00:00');
INSERT INTO SiteContents (SectionKey, ContentAr, UpdatedAt) VALUES (N'hero_title_en', N'INNOVEST HOLDING', '2026-06-01T00:00:00');
INSERT INTO SiteContents (SectionKey, ContentAr, UpdatedAt) VALUES (N'hero_subtitle', N'نبني جسور الاستثمار والابتكار من خلال منصات متكاملة للتدريب والتعليم والاستثمار والفعاليات والشراكات الاستراتيجية', '2026-06-01T00:00:00');
INSERT INTO SiteContents (SectionKey, ContentAr, UpdatedAt) VALUES (N'about_text_1', N'إنوفست هولدينغ هي شركة رائدة في مجال الاستثمار والابتكار، تأسست برؤية طموحة لبناء منظومة متكاملة تجمع بين التدريب والتعليم، والاستثمار، والتشبيك، والفعاليات. نسعى لتمكين رواد الأعمال والمستثمرين والشركات من خلال منصات متخصصة توفر بيئة محفزة للنمو والتطور.', '2026-06-01T00:00:00');
INSERT INTO SiteContents (SectionKey, ContentAr, UpdatedAt) VALUES (N'about_text_2', N'نعمل على تقديم حلول مبتكرة تلبي احتياجات السوق المتنامية، مع التركيز على الجودة والاحترافية في كل ما نقدمه من خدمات ومنصات.', '2026-06-01T00:00:00');
INSERT INTO SiteContents (SectionKey, ContentAr, UpdatedAt) VALUES (N'about_years', N'+10', '2026-06-01T00:00:00');
INSERT INTO SiteContents (SectionKey, ContentAr, UpdatedAt) VALUES (N'vision_title', N'الرؤية', '2026-06-01T00:00:00');
INSERT INTO SiteContents (SectionKey, ContentAr, UpdatedAt) VALUES (N'vision_text', N'أن نكون المنصة الأولى للاستثمار والابتكار في المنطقة', '2026-06-01T00:00:00');
INSERT INTO SiteContents (SectionKey, ContentAr, UpdatedAt) VALUES (N'mission_title', N'الرسالة', '2026-06-01T00:00:00');
INSERT INTO SiteContents (SectionKey, ContentAr, UpdatedAt) VALUES (N'mission_text', N'تمكين الأفراد والشركات عبر منصات متكاملة ومبتكرة', '2026-06-01T00:00:00');
INSERT INTO SiteContents (SectionKey, ContentAr, UpdatedAt) VALUES (N'values_title', N'القيم', '2026-06-01T00:00:00');
INSERT INTO SiteContents (SectionKey, ContentAr, UpdatedAt) VALUES (N'values_text', N'الابتكار والجودة والشفافية والتميز المستمر', '2026-06-01T00:00:00');
INSERT INTO SiteContents (SectionKey, ContentAr, UpdatedAt) VALUES (N'contact_address', N'Salah Salem St, Heliopolis, Cairo', '2026-06-01T00:00:00');
INSERT INTO SiteContents (SectionKey, ContentAr, UpdatedAt) VALUES (N'contact_phone', N'+20 114 141 0808', '2026-06-01T00:00:00');
INSERT INTO SiteContents (SectionKey, ContentAr, UpdatedAt) VALUES (N'contact_email', N'info@innovestholding.com', '2026-06-01T00:00:00');
INSERT INTO SiteContents (SectionKey, ContentAr, UpdatedAt) VALUES (N'contact_hours', N'الأحد - الخميس: 9:00 ص - 5:00 م', '2026-06-01T00:00:00');
SET IDENTITY_INSERT NavItems ON;
INSERT INTO NavItems (Id, Title, Href, SortOrder, HasDropdown, IsActive) VALUES (1, N'الرئيسية', N'#hero', 1, 0, 1);
INSERT INTO NavItems (Id, Title, Href, SortOrder, HasDropdown, IsActive) VALUES (2, N'من نحن', N'#about', 2, 0, 1);
INSERT INTO NavItems (Id, Title, Href, SortOrder, HasDropdown, IsActive) VALUES (3, N'منصاتنا', N'#platforms', 3, 1, 1);
INSERT INTO NavItems (Id, Title, Href, SortOrder, HasDropdown, IsActive) VALUES (4, N'الأخبار', N'#news', 4, 0, 1);
INSERT INTO NavItems (Id, Title, Href, SortOrder, HasDropdown, IsActive) VALUES (5, N'الفعاليات', N'#events', 5, 0, 1);
INSERT INTO NavItems (Id, Title, Href, SortOrder, HasDropdown, IsActive) VALUES (6, N'الشركاء والرعاة', N'#partners', 6, 0, 1);
INSERT INTO NavItems (Id, Title, Href, SortOrder, HasDropdown, IsActive) VALUES (7, N'التسجيل السريع', N'#register', 7, 0, 1);
INSERT INTO NavItems (Id, Title, Href, SortOrder, HasDropdown, IsActive) VALUES (8, N'تواصل معنا', N'#contact', 8, 0, 1);
SET IDENTITY_INSERT NavItems OFF;
INSERT INTO NavSubItems (NavItemId, Title, Href, IconSvg, SortOrder) VALUES (3, N'التدريب والتعليم', N'#platforms', N'<path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z"/><path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z"/>', 1);
INSERT INTO NavSubItems (NavItemId, Title, Href, IconSvg, SortOrder) VALUES (3, N'منصة المستثمرين والتشبيك', N'#platforms', N'<path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/>', 2);
INSERT INTO NavSubItems (NavItemId, Title, Href, IconSvg, SortOrder) VALUES (3, N'الإعلام والمحتوى الرقمي', N'#platforms', N'<polygon points="23 7 16 12 23 17 23 7"/><rect x="1" y="5" width="15" height="14" rx="2" ry="2"/>', 3);
INSERT INTO NavSubItems (NavItemId, Title, Href, IconSvg, SortOrder) VALUES (3, N'المسابقات', N'#platforms', N'<path d="M6 9H4.5a2.5 2.5 0 0 1 0-5C7 4 9 7 12 7s5-3 7.5-3a2.5 2.5 0 0 1 0 5H18"/><path d="M12 7v10"/><path d="M6 21h12a2 2 0 0 0 2-2v-6a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v6a2 2 0 0 0 2 2z"/>', 4);
INSERT INTO NavSubItems (NavItemId, Title, Href, IconSvg, SortOrder) VALUES (3, N'الفعاليات والمعارض', N'#platforms', N'<rect x="3" y="4" width="18" height="18" rx="2" ry="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/>', 5);
INSERT INTO NavSubItems (NavItemId, Title, Href, IconSvg, SortOrder) VALUES (3, N'الشراكات الاستراتيجية و B2B', N'#platforms', N'<circle cx="12" cy="12" r="10"/><line x1="2" y1="12" x2="22" y2="12"/><path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/>', 6);
INSERT INTO Events (Title, Description, EventDay, EventMonth, Location, Time, IsActive, SortOrder, CreatedAt) VALUES (N'مؤتمر إنوفست للاستثمار والابتكار 2026', N'مؤتمر دولي يجمع كبار المستثمرين ورواد الأعمال لمناقشة أحدث اتجاهات الاستثمار وفرص الابتكار.', 15, N'يوليو', N'فندق الريتز كارلتون - الرياض', N'9:00 ص - 5:00 م', 1, 1, '2026-06-01T00:00:00');
INSERT INTO Events (Title, Description, EventDay, EventMonth, Location, Time, IsActive, SortOrder, CreatedAt) VALUES (N'ورشة عمل: بناء المشاريع الناشئة من الصفر', N'ورشة عمل تفاعلية مكثفة تغطي جميع مراحل بناء المشروع الناشئ بدءاً من الفكرة وحتى التمويل.', 28, N'أغسطس', N'مركز إنوفست للتدريب - عمّان', N'10:00 ص - 4:00 م', 1, 2, '2026-06-01T00:00:00');
INSERT INTO Events (Title, Description, EventDay, EventMonth, Location, Time, IsActive, SortOrder, CreatedAt) VALUES (N'معرض إنوفست الدولي للشراكات والتقنية', N'معرض دولي يجمع الشركات والمؤسسات من مختلف القطاعات لاستعراض أحدث التقنيات وبناء شراكات جديدة.', 10, N'سبتمبر', N'مركز دبي الدولي للمعارض', N'3 أيام', 1, 3, '2026-06-01T00:00:00');
INSERT INTO News (Title, Summary, Category, NewsDate, IsActive, CreatedAt) VALUES (N'إنوفست هولدينغ توقع شراكة استراتيجية مع كبرى شركات التكنولوجيا', N'أعلنت إنوفست هولدينغ عن توقيع اتفاقية شراكة استراتيجية جديدة تهدف إلى تعزيز التحول الرقمي وتوسيع نطاق خدماتها.', N'شراكات', N'25 مايو 2026', 1, '2026-06-01T00:00:00');
INSERT INTO News (Title, Summary, Category, NewsDate, IsActive, CreatedAt) VALUES (N'إطلاق برنامج تدريبي جديد في ريادة الأعمال والابتكار', N'تعلن منصة التدريب والتعليم عن إطلاق برنامج تدريبي شامل يستهدف رواد الأعمال الطموحين مع خبراء دوليين.', N'تدريب', N'18 مايو 2026', 1, '2026-06-01T00:00:00');
INSERT INTO News (Title, Summary, Category, NewsDate, IsActive, CreatedAt) VALUES (N'نجاح كبير لمعرض إنوفست الدولي للاستثمار 2026', N'اختُتمت فعاليات معرض إنوفست الدولي بمشاركة أكثر من 200 شركة ومؤسسة من 30 دولة حول العالم.', N'فعاليات', N'10 مايو 2026', 1, '2026-06-01T00:00:00');
INSERT INTO Platforms (Title, Description, IconSvg, SortOrder) VALUES (N'التدريب والتعليم', N'برامج تدريبية متخصصة وورش عمل احترافية مصممة لتطوير المهارات وبناء القدرات في مجالات الأعمال والتكنولوجيا والقيادة.', N'<path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z"/><path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z"/>', 1);
INSERT INTO Platforms (Title, Description, IconSvg, SortOrder) VALUES (N'منصة المستثمرين والتشبيك', N'منصة فريدة تربط بين المستثمرين ورواد الأعمال والشركات، وتوفر فرص تشبيك استراتيجية وشراكات مثمرة.', N'<path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/>', 2);
INSERT INTO Platforms (Title, Description, IconSvg, SortOrder) VALUES (N'الإعلام والمحتوى الرقمي', N'إنتاج محتوى رقمي إبداعي ومتميز، يشمل الفيديو والبودكاست والمقالات المتخصصة في عالم الأعمال والاستثمار.', N'<polygon points="23 7 16 12 23 17 23 7"/><rect x="1" y="5" width="15" height="14" rx="2" ry="2"/>', 3);
INSERT INTO Platforms (Title, Description, IconSvg, SortOrder) VALUES (N'المسابقات', N'مسابقات وتحديات مبتكرة تستهدف رواد الأعمال والمبدعين، مع جوائز قيمة وفرص حقيقية لتحويل الأفكار إلى مشاريع ناجحة.', N'<path d="M6 9H4.5a2.5 2.5 0 0 1 0-5C7 4 9 7 12 7s5-3 7.5-3a2.5 2.5 0 0 1 0 5H18"/><path d="M12 7v10"/><path d="M6 21h12a2 2 0 0 0 2-2v-6a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v6a2 2 0 0 0 2 2z"/>', 4);
INSERT INTO Platforms (Title, Description, IconSvg, SortOrder) VALUES (N'الفعاليات والمعارض', N'تنظيم وإدارة فعاليات ومعارض دولية ومحلية تجمع بين أصحاب القرار والمستثمرين ورواد الأعمال في بيئة محفزة.', N'<rect x="3" y="4" width="18" height="18" rx="2" ry="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/>', 5);
INSERT INTO Platforms (Title, Description, IconSvg, SortOrder) VALUES (N'الشراكات الاستراتيجية و B2B', N'بناء شراكات استراتيجية مع مؤسسات وشركات محلية ودولية، وتوفير حلول أعمال متكاملة لتعزيز النمو المشترك.', N'<circle cx="12" cy="12" r="10"/><line x1="2" y1="12" x2="22" y2="12"/><path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/>', 6);
INSERT INTO Partners (Name, SortOrder) VALUES (N'شركة الاتصالات', 1);
INSERT INTO Partners (Name, SortOrder) VALUES (N'بنك الاستثمار', 2);
INSERT INTO Partners (Name, SortOrder) VALUES (N'مجموعة التقنية', 3);
INSERT INTO Partners (Name, SortOrder) VALUES (N'شركة الطاقة', 4);
INSERT INTO Partners (Name, SortOrder) VALUES (N'مجموعة العقار', 5);
INSERT INTO Partners (Name, SortOrder) VALUES (N'شركة الصناعات', 6);
INSERT INTO Partners (Name, SortOrder) VALUES (N'بنك التنمية', 7);
INSERT INTO Partners (Name, SortOrder) VALUES (N'مجموعة الأعمال', 8);
INSERT INTO Partners (Name, SortOrder) VALUES (N'شركة التأمين', 9);
INSERT INTO Partners (Name, SortOrder) VALUES (N'مؤسسة التعليم', 10);
INSERT INTO Stats (Label, Value, Suffix, IconSvg, SortOrder) VALUES (N'شريك استراتيجي', 150, N'+', N'<path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/>', 1);
INSERT INTO Stats (Label, Value, Suffix, IconSvg, SortOrder) VALUES (N'مستثمر نشط', 2500, N'+', N'<line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/>', 2);
INSERT INTO Stats (Label, Value, Suffix, IconSvg, SortOrder) VALUES (N'فعالية سنوياً', 80, N'+', N'<rect x="3" y="4" width="18" height="18" rx="2" ry="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/>', 3);
INSERT INTO Stats (Label, Value, Suffix, IconSvg, SortOrder) VALUES (N'متدرب ومستفيد', 5000, N'+', N'<path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z"/><path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z"/>', 4);
INSERT INTO Faqs (Question, Answer, SortOrder, IsActive) VALUES (N'ما هي إنوفست هولدينغ؟', N'إنوفست هولدينغ هي شركة رائدة في مجال الاستثمار والابتكار، تقدم منظومة متكاملة من المنصات والخدمات تشمل التدريب والتعليم، والاستثمار والتشبيك، والفعاليات والمعارض، والشراكات الاستراتيجية. تأسست الشركة بهدف بناء جسور حقيقية بين المستثمرين ورواد الأعمال والشركات.', 1, 1);
INSERT INTO Faqs (Question, Answer, SortOrder, IsActive) VALUES (N'كيف يمكنني التسجيل في فعاليات إنوفست؟', N'يمكنك التسجيل بسهولة عبر نموذج التسجيل السريع في موقعنا، أو من خلال صفحة الفعاليات حيث يتوفر زر تسجيل لكل فعالية. كما يمكنك التواصل مع فريقنا مباشرة عبر البريد الإلكتروني أو الهاتف.', 2, 1);
INSERT INTO Faqs (Question, Answer, SortOrder, IsActive) VALUES (N'ما هي البرامج التدريبية المتاحة؟', N'نقدم مجموعة متنوعة من البرامج التدريبية في مجالات ريادة الأعمال، الاستثمار، التسويق الرقمي، القيادة والإدارة، التكنولوجيا والابتكار. تُقدم البرامج من قبل خبراء ومتخصصين دوليين ومحليين بأحدث الأساليب التعليمية.', 3, 1);
INSERT INTO Faqs (Question, Answer, SortOrder, IsActive) VALUES (N'كيف يمكنني أن أصبح شريكاً أو راعياً لإنوفست؟', N'نرحب بالشراكات مع المؤسسات والشركات الراغبة في التعاون. يمكنك التواصل معنا عبر نموذج الاتصال في الموقع أو إرسال بريد إلكتروني إلى فريق الشراكات. سنقوم بدراسة اقتراحكم والرد في أقرب وقت ممكن.', 4, 1);
INSERT INTO Faqs (Question, Answer, SortOrder, IsActive) VALUES (N'هل توفر إنوفست فرص استثمارية مباشرة؟', N'نعم، من خلال منصة المستثمرين والتشبيك، نوفر فرصاً استثمارية متنوعة في قطاعات مختلفة. نعمل على ربط المستثمرين بالمشاريع الواعدة ونقدم الدعم اللازم لضمان نجاح الاستثمارات.', 5, 1);
INSERT INTO Faqs (Question, Answer, SortOrder, IsActive) VALUES (N'أين تقام فعاليات ومعارض إنوفست؟', N'تُقام فعالياتنا في مختلف المدن الرئيسية في المنطقة العربية، بما في ذلك الرياض، دبي، عمّان، والقاهرة. كما نقدم فعاليات افتراضية عبر الإنترنت تتيح المشاركة من أي مكان في العالم.', 6, 1);
""";
}
