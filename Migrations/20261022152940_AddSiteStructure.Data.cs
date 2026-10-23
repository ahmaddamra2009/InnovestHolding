using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovestHolding.Migrations
{
    /// <summary>
    /// The reference rows of AddSiteStructure. Every literal is FROZEN (copied from res/Company Profile Content.docx and
    /// the page plan), never read from application code. The migration owns these rows: DbSeeder never writes Pages or
    /// ContentItems, so a fresh install and an upgraded database get the same rows.
    ///
    /// Guard polarity (ANTIGRAVITY §3): a page row is inserted only when its Key is missing, and a list group only when
    /// the table holds no row of that group — a second run, or rows an admin already added, make the step do nothing.
    /// </summary>
    public partial class AddSiteStructure
    {
        /// <summary>N'…' with quotes doubled; NULL for null.</summary>
        private static string L(string value) => value is null ? "NULL" : "N'" + value.Replace("'", "''") + "'";

        // ── Pages: Key, Parent order, TitleAr, TitleEn, SummaryAr, SummaryEn, IntroAr, IntroEn, BodyAr, BodyEn,
        //    EmptyTextAr, EmptyTextEn, MetaTitleAr, MetaTitleEn, MetaDescriptionAr, MetaDescriptionEn ──
        private static readonly string[][] PageRows =
        [
            ["home", "0", "الرئيسية", "Home", "", null, "", null, "", null, "", null,
                "المنصة العربية لريادة الأعمال والابتكار والاستثمار", "The Arab Platform for Entrepreneurship, Innovation & Investment",
                "إنوفست هولدينغ هي البوابة العربية التي تربط الابتكار بريادة الأعمال والاستثمار لبناء اقتصاد عربي قائم على المعرفة والابتكار.",
                "Innovest Holding is the Arab gateway connecting innovation, entrepreneurship, investment and sustainable growth."],
            ["about", "1", "عن إنوفست هولدينغ", "About Innovest Holding",
                "شركة عربية تبني منظومة متكاملة لريادة الأعمال والابتكار والاستثمار.",
                "An Arab company building an integrated ecosystem for entrepreneurship, innovation and investment.",
                "تعد إنوفست هولدينغ شركة عربية متخصصة في بناء منظومة متكاملة لريادة الأعمال والابتكار والاستثمار، تعمل على تمكين الشركات الناشئة ورواد الأعمال وربطهم بالمستثمرين، وصناديق الاستثمار، والمستثمرين الملائكيين، وجهات التمويل، وحاضنات ومسرعات الأعمال، من أجل تحويل الأفكار المبتكرة إلى شركات قادرة على النمو والمنافسة محلياً وإقليمياً وعالمياً.",
                "Innovest Holding is an Arab company specialised in building an integrated ecosystem for entrepreneurship, innovation and investment. It empowers startups and entrepreneurs and connects them with investors, investment funds, angel investors, funding bodies, and business incubators and accelerators, so that innovative ideas become companies able to grow and compete locally, regionally and globally.",
                "تعتمد الشركة على منصة رقمية متكاملة، بالإضافة إلى مجموعة من المنتديات والمؤتمرات والبرامج التدريبية والفعاليات الاستثمارية التي تُقام على مدار العام في مختلف الدول العربية.",
                "The company relies on an integrated digital platform, together with forums, conferences, training programmes and investment events held throughout the year across the Arab countries.",
                "", null,
                "عن إنوفست هولدينغ: الرؤية والرسالة والقيم", "About Innovest Holding: Vision, Mission and Values",
                "تعرّف على إنوفست هولدينغ ورؤيتها ورسالتها وقيمها والقطاعات التي تخدمها وأثرها المتوقع في منظومة ريادة الأعمال العربية.",
                "Learn about Innovest Holding: its vision, mission and core values, the sectors it serves and its expected impact on the Arab entrepreneurship ecosystem."],
            ["ecosystem", "2", "منظومتنا", "Our Ecosystem",
                "المنتدى وشبكة المستثمرين والبرنامج التلفزيوني وخدماتنا في منظومة واحدة.",
                "The Forum, the Angel Investors Network, the TV Program and our services in one ecosystem.",
                "نعمل على بناء منظومة متكاملة تربط رواد الأعمال والشركات الناشئة بالمستثمرين والشركاء الاستراتيجيين وجهات التمويل، وتوفر الأدوات والخدمات اللازمة لتحويل الأفكار إلى شركات ناجحة.",
                "We are building an integrated ecosystem that connects entrepreneurs and startups with investors, strategic partners and funding bodies, and provides the tools and services needed to turn ideas into successful companies.",
                "", null, "", null,
                "منظومة إنوفست لريادة الأعمال والاستثمار", "The Innovest Entrepreneurship & Investment Ecosystem",
                "منتدى إنوفست العربي وشبكة المستثمرين الملائكيين العرب والبرنامج التلفزيوني، مع خدماتنا وبرامجنا للشركات الناشئة والمستثمرين والحكومات والجامعات.",
                "The Innovest Arab Forum, the Arab Angel Investors Network and the TV Program, with our services and programmes for startups, investors, governments and universities."],
            ["forum", "1", "منتدى إنوفست العربي", "Innovest Arab Forum",
                "أكبر منتدى عربي للابتكار والاستثمار وريادة الأعمال.",
                "The largest Arab forum for innovation, investment and entrepreneurship.",
                "منتدى إنوفست العربي هو أكبر منتدى عربي للابتكار والاستثمار وريادة الأعمال، يجمع رواد الأعمال والشركات الناشئة والمستثمرين والجهات الحكومية في معرض وملتقيات ومسابقات واجتماعات استثمارية.",
                "The Innovest Arab Forum is the largest Arab forum for innovation, investment and entrepreneurship. It brings entrepreneurs, startups, investors and government bodies together in an expo, summits, competitions and investment meetings.",
                "", null, "", null,
                "منتدى إنوفست العربي للابتكار والاستثمار", "Innovest Arab Forum for Innovation & Investment",
                "أكبر منتدى عربي للابتكار والاستثمار وريادة الأعمال: معرض الشركات الناشئة، منتدى الاستثمار، ملتقى الذكاء الاصطناعي، مسابقة العروض وجوائز الابتكار.",
                "The largest Arab forum for innovation, investment and entrepreneurship: a startup expo, an investment forum, an AI summit, a pitch competition and innovation awards."],
            ["forum.events", "1", "الفعاليات", "Events",
                "فعاليات منتدى إنوفست العربي ومواعيدها.", "Innovest Arab Forum events and dates.",
                "تابع فعاليات منتدى إنوفست العربي ومواعيدها وأماكن انعقادها.", "Follow the Innovest Arab Forum events, their dates and venues.",
                "", null,
                "سيتم الإعلان عن الفعاليات قريباً.", "Events will be announced soon.",
                "فعاليات منتدى إنوفست العربي", "Innovest Arab Forum Events",
                "مواعيد وأماكن فعاليات منتدى إنوفست العربي للابتكار والاستثمار وريادة الأعمال، وطريقة التسجيل لحضورها.",
                "Dates and venues of the Innovest Arab Forum events on innovation, investment and entrepreneurship, and how to register to attend."],
            ["forum.speakers", "2", "المتحدثون", "Speakers",
                "متحدثو منتدى إنوفست العربي.", "Speakers of the Innovest Arab Forum.",
                "", null, "", null,
                "سيتم الإعلان عن المتحدثين قريباً.", "Speakers will be announced soon.",
                "متحدثو منتدى إنوفست العربي", "Innovest Arab Forum Speakers",
                "تعرّف على متحدثي منتدى إنوفست العربي من رواد الأعمال والمستثمرين والخبراء.",
                "Meet the speakers of the Innovest Arab Forum: entrepreneurs, investors and experts."],
            ["forum.partners", "3", "الشركاء والرعاة", "Partners & Sponsors",
                "شركاؤنا ورعاة منتدى إنوفست العربي.", "Our partners and the sponsors of the Innovest Arab Forum.",
                "نعمل مع الوزارات والجامعات والمستثمرين والبنوك وصناديق الاستثمار والمؤسسات الدولية والقطاع الخاص.",
                "We work with ministries, universities, investors, banks, investment funds, international institutions and the private sector.",
                "", null,
                "سيتم الإعلان عن الشركاء والرعاة قريباً.", "Partners and sponsors will be announced soon.",
                "شركاء ورعاة منتدى إنوفست العربي", "Innovest Arab Forum Partners & Sponsors",
                "شركاء إنوفست هولدينغ ورعاة منتدى إنوفست العربي من الجهات الحكومية والجامعات والمستثمرين والقطاع الخاص.",
                "Innovest Holding's partners and the sponsors of the Innovest Arab Forum from government, universities, investors and the private sector."],
            ["forum.registration", "4", "التسجيل", "Registration",
                "سجّل اهتمامك بحضور فعاليات المنتدى.", "Register your interest in attending the Forum.",
                "سجّل بياناتك وسنتواصل معك بتفاصيل فعاليات منتدى إنوفست العربي القادمة.",
                "Leave your details and we will contact you about the upcoming Innovest Arab Forum events.",
                "", null, "", null,
                "التسجيل في منتدى إنوفست العربي", "Register for the Innovest Arab Forum",
                "سجّل اهتمامك بحضور منتدى إنوفست العربي وتلقَّ تفاصيل الفعاليات القادمة ومواعيدها.",
                "Register your interest in attending the Innovest Arab Forum and receive the details and dates of upcoming events."],
            ["network", "2", "شبكة المستثمرين الملائكيين العرب", "Arab Angel Investors Network",
                "نربط المستثمرين بأفضل الفرص الاستثمارية في الشركات الناشئة.",
                "Connecting investors with the best investment opportunities in startups.",
                "نساعد المستثمرين في الوصول إلى أفضل الفرص الاستثمارية، ونساعد الشركات الناشئة على الاستعداد للاستثمار والتشبيك مع المستثمرين الملائكيين وصناديق الاستثمار.",
                "We help investors reach the best investment opportunities, and we help startups become investment-ready and connect with angel investors and investment funds.",
                "", null, "", null,
                "شبكة المستثمرين الملائكيين العرب", "Arab Angel Investors Network",
                "شبكة إنوفست للمستثمرين الملائكيين: تدفق الصفقات، فحص الشركات الناشئة، أيام العروض التقديمية ومطابقة الاستثمارات.",
                "The Innovest angel investors network: deal flow, startup screening, pitch days and investment matching."],
            ["network.investors", "1", "للمستثمرين", "For Investors",
                "خدماتنا للمستثمرين وصناديق الاستثمار.", "Our services for investors and investment funds.",
                "نساعد المستثمرين في الوصول إلى أفضل الفرص الاستثمارية من خلال تدفق صفقات مدروس ودعم العناية الواجبة.",
                "We help investors reach the best investment opportunities through a curated deal flow and due-diligence support.",
                "", null, "", null,
                "خدمات المستثمرين في شبكة إنوفست", "Services for Investors in the Innovest Network",
                "تدفق الصفقات، دعم العناية الواجبة، فحص الشركات الناشئة، أيام العروض التقديمية، مطابقة الاستثمارات وفرص المحفظة الاستثمارية.",
                "Deal flow, due-diligence support, startup screening, pitch days, investment matching and portfolio opportunities."],
            ["network.startups", "2", "للشركات الناشئة", "For Startups",
                "ندعم الشركات الناشئة من الفكرة حتى جولة الاستثمار.", "We support startups from the idea to the investment round.",
                "ندعم الشركات الناشئة في احتضان الأفكار وتطوير نماذج الأعمال وإعداد العرض التقديمي ودراسة الجدوى، ونجهزها للاستثمار وللتشبيك مع المستثمرين.",
                "We support startups in incubating ideas, developing business models, preparing pitch decks and feasibility studies, and we prepare them for investment and for meeting investors.",
                "", null, "", null,
                "دعم الشركات الناشئة والاستعداد للاستثمار", "Startup Support and Investment Readiness",
                "خدمات إنوفست للشركات الناشئة: الاحتضان، تطوير نموذج العمل، إعداد العرض التقديمي، دراسة الجدوى، الإرشاد والاستعداد للاستثمار.",
                "Innovest services for startups: incubation, business model development, pitch deck preparation, feasibility studies, mentorship and investment readiness."],
            ["network.opportunities", "3", "الفرص الاستثمارية", "Opportunities",
                "فرص استثمارية في شركات ناشئة واعدة.", "Investment opportunities in promising startups.",
                "", null, "", null,
                "سيتم نشر الفرص الاستثمارية قريباً.", "Investment opportunities will be published soon.",
                "الفرص الاستثمارية في الشركات الناشئة", "Startup Investment Opportunities",
                "فرص استثمارية في شركات ناشئة عربية واعدة تنشرها شبكة المستثمرين الملائكيين العرب بعد المراجعة.",
                "Investment opportunities in promising Arab startups, published by the Arab Angel Investors Network after review."],
            ["network.join", "4", "انضم إلى الشبكة", "Join the Network",
                "انضم كمستثمر أو كشركة ناشئة.", "Join as an investor or as a startup.",
                "يمكن للمستثمرين وللشركات الناشئة الانضمام إلى شبكة المستثمرين الملائكيين العرب. تواصل معنا وسيتابع فريقنا طلبك.",
                "Investors and startups can join the Arab Angel Investors Network. Contact us and our team will follow up on your request.",
                "", null, "", null,
                "انضم إلى شبكة المستثمرين الملائكيين العرب", "Join the Arab Angel Investors Network",
                "انضم إلى شبكة إنوفست للمستثمرين الملائكيين كمستثمر يبحث عن الفرص أو كشركة ناشئة تبحث عن التمويل.",
                "Join the Innovest angel investors network as an investor looking for opportunities or as a startup looking for funding."],
            ["tv", "3", "البرنامج التلفزيوني", "TV Program",
                "البرنامج التلفزيوني لإنوفست هولدينغ.", "The Innovest Holding TV program.",
                "", null, "", null,
                "سيتم الإعلان عن تفاصيل البرنامج قريباً.", "Details of the program will be announced soon.",
                "البرنامج التلفزيوني لإنوفست هولدينغ", "The Innovest Holding TV Program",
                "البرنامج التلفزيوني لإنوفست هولدينغ: الحلقات والضيوف وفرص الرعاية.",
                "The Innovest Holding TV program: episodes, guests and sponsorship opportunities."],
            ["tv.episodes", "1", "الحلقات", "Episodes",
                "حلقات البرنامج.", "Episodes of the program.",
                "", null, "", null,
                "ستُنشر الحلقات قريباً.", "Episodes will be published soon.",
                "حلقات البرنامج التلفزيوني", "TV Program Episodes",
                "شاهد حلقات البرنامج التلفزيوني لإنوفست هولدينغ عن ريادة الأعمال والابتكار والاستثمار.",
                "Watch the episodes of the Innovest Holding TV program on entrepreneurship, innovation and investment."],
            ["tv.guests", "2", "الضيوف", "Guests",
                "ضيوف البرنامج.", "Guests of the program.",
                "", null, "", null,
                "سيتم الإعلان عن ضيوف البرنامج قريباً.", "Guests will be announced soon.",
                "ضيوف البرنامج التلفزيوني", "TV Program Guests",
                "تعرّف على ضيوف البرنامج التلفزيوني لإنوفست هولدينغ من رواد الأعمال والمستثمرين والخبراء.",
                "Meet the guests of the Innovest Holding TV program: entrepreneurs, investors and experts."],
            ["tv.sponsorship", "3", "الرعاية", "Sponsorship",
                "فرص رعاية البرنامج.", "Sponsorship opportunities for the program.",
                "", null, "", null,
                "ستُعلن باقات الرعاية قريباً. تواصل معنا للاستفسار.", "Sponsorship packages will be announced soon. Contact us to enquire.",
                "رعاية البرنامج التلفزيوني", "TV Program Sponsorship",
                "فرص رعاية البرنامج التلفزيوني لإنوفست هولدينغ والوصول إلى مجتمع رواد الأعمال والمستثمرين العرب.",
                "Sponsorship opportunities for the Innovest Holding TV program and access to the Arab community of entrepreneurs and investors."],
            ["news", "3", "الأخبار والمستجدات", "News & Updates",
                "آخر أخبار إنوفست هولدينغ.", "The latest news from Innovest Holding.",
                "تابع آخر أخبار إنوفست هولدينغ ومستجدات منتدياتها وبرامجها.", "Follow the latest news from Innovest Holding and updates on its forums and programmes.",
                "", null,
                "لا توجد أخبار منشورة بعد.", "No news has been published yet.",
                "أخبار إنوفست هولدينغ ومستجداتها", "Innovest Holding News & Updates",
                "آخر أخبار إنوفست هولدينغ ومستجدات منتدى إنوفست العربي وشبكة المستثمرين الملائكيين والبرامج.",
                "The latest Innovest Holding news and updates on the Innovest Arab Forum, the angel investors network and our programmes."],
            ["contact", "4", "تواصل معنا", "Contact Us",
                "تواصل مع فريق إنوفست هولدينغ.", "Get in touch with the Innovest Holding team.",
                "فريق إنوفست هولدينغ مستعد دائماً للإجابة على استفساراتكم. تواصلوا معنا عبر البريد الإلكتروني أو الهاتف وواتساب أو من خلال النموذج.",
                "The Innovest Holding team is always ready to answer your questions. Reach us by email, by phone and WhatsApp, or through the form.",
                "", null, "", null,
                "تواصل مع إنوفست هولدينغ", "Contact Innovest Holding",
                "تواصل مع إنوفست هولدينغ عبر البريد الإلكتروني أو الهاتف وواتساب، أو أرسل رسالتك من خلال النموذج.",
                "Contact Innovest Holding by email, by phone and WhatsApp, or send us a message through the form."],
        ];

        // ── ContentItems: group → (TextAr, TextEn, IconKey or null[, DescriptionAr, DescriptionEn]) ──
        private static readonly (string Group, string[][] Rows)[] ItemGroups =
        [
            ("values",
            [
                ["الابتكار", "Innovation", "idea"], ["الشراكة", "Partnership", "handshake"], ["الشفافية", "Transparency", "eye"],
                ["الاستدامة", "Sustainability", "leaf"], ["الاحترافية", "Professionalism", "briefcase"], ["التأثير", "Impact", "target"],
                ["التمكين", "Empowerment", "rocket"],
            ]),
            ("why_points",
            [
                ["التدريب", "Training", "book"], ["الاحتضان", "Incubation", "idea"], ["المسرعات", "Accelerators", "rocket"],
                ["المستثمرون", "Investors", "users"], ["التمويل", "Funding", "coins"], ["الاستشارات", "Consulting", "briefcase"],
                ["التشبيك", "Networking", "link"], ["الفعاليات", "Events", "calendar"], ["المنصة الرقمية", "Digital Platform", "cpu"],
                ["العلاقات الحكومية", "Government Relations", "building"], ["العلاقات العربية", "Arab Relations", "globe"],
            ]),
            ("sectors",
            [
                ["التكنولوجيا المالية", "FinTech", "coins"], ["التكنولوجيا الصحية", "HealthTech", "shield"],
                ["التكنولوجيا التعليمية", "EdTech", "graduation"], ["التكنولوجيا الزراعية", "AgriTech", "leaf"],
                ["التكنولوجيا الخضراء", "GreenTech", "leaf"], ["الذكاء الاصطناعي", "AI", "cpu"],
                ["الأمن السيبراني", "Cyber Security", "shield"], ["الخدمات اللوجستية", "Logistics", "layers"],
                ["المدن الذكية", "Smart Cities", "building"], ["الطاقة المتجددة", "Renewable Energy", "leaf"],
                ["السياحة", "Tourism", "globe"], ["الصناعة", "Industry", "building"],
                ["تجارة التجزئة", "Retail", "briefcase"], ["التجارة الإلكترونية", "E-Commerce", "link"],
            ]),
            ("ecosystem_pillars",
            [
                ["دعم الشركات الناشئة", "Startup Support", "rocket"], ["الاستعداد للاستثمار", "Investment Readiness", "chart"],
                ["شبكة المستثمرين الملائكيين", "Angel Investors Network", "users"], ["روابط رأس المال المخاطر", "Venture Capital Connections", "coins"],
                ["مسرعات الأعمال الحكومية", "Government Accelerators", "building"], ["حاضنات الأعمال", "Business Incubators", "idea"],
                ["الابتكار المؤسسي", "Corporate Innovation", "layers"], ["الشراكات الاستراتيجية", "Strategic Partnerships", "handshake"],
                ["التوفيق بين الشركات", "Business Matching", "link"], ["برامج الابتكار", "Innovation Programs", "award"],
            ]),
            ("programs",
            [
                ["معسكر تدريب الشركات الناشئة", "Startup Bootcamp", "rocket"], ["برنامج تأهيل المستثمرين", "Investor Readiness Program", "chart"],
                ["برنامج القيادة", "Leadership Program", "target"], ["ريادة الأعمال النسائية", "Women Entrepreneurship", "users"],
                ["ريادة الأعمال الشبابية", "Youth Entrepreneurship", "users"], ["مختبرات الابتكار", "Innovation Labs", "idea"],
                ["برامج الاحتضان", "Incubation Programs", "layers"], ["برامج تسريع الأعمال", "Acceleration Programs", "chart"],
            ]),
            ("forum_components",
            [
                ["معرض إنوفست للشركات الناشئة", "Innovest Startup Expo", "rocket"], ["منتدى الابتكار والاستثمار العربي", "Investment Forum Event", "users"],
                ["ملتقى الذكاء الاصطناعي", "AI Summit", "cpu"], ["مسابقة العروض التقديمية", "Pitch Competition", "mic"],
                ["اجتماعات المستثمرين", "Investment Meetings", "briefcase"], ["التشبيك بين الشركات", "Business Matching", "link"],
                ["جلسات حكومية", "Government Sessions", "building"], ["جوائز الابتكار", "Innovation Awards", "award"],
            ]),
            ("investor_services",
            [
                ["تدفق الصفقات", "Deal Flow", "layers"], ["دعم العناية الواجبة", "Due Diligence Support", "shield"],
                ["فحص الشركات الناشئة", "Startup Screening", "search"], ["أيام العروض التقديمية", "Pitch Days", "mic"],
                ["مطابقة الاستثمارات", "Investment Matching", "link"], ["فرص المحفظة الاستثمارية", "Portfolio Opportunities", "chart"],
            ]),
            ("startup_services",
            [
                ["احتضان الأفكار الناشئة", "Incubating early-stage ideas", "idea"], ["تطوير نماذج الأعمال", "Business model development", "layers"],
                ["إعداد العرض التقديمي (Pitch Deck)", "Pitch deck preparation", "mic"], ["دراسة الجدوى", "Feasibility studies", "search"],
                ["نموذج العمل (Business Model)", "Business model", "briefcase"], ["الإرشاد (Mentorship)", "Mentorship", "users"],
                ["التوجيه المهني (Business Coaching)", "Business coaching", "target"],
            ]),
            ("government_services",
            [
                ["تصميم برامج ريادة الأعمال", "Designing entrepreneurship programs", "layers"], ["تنظيم المبادرات", "Organizing initiatives", "calendar"],
                ["بناء المسرعات", "Building accelerators", "rocket"], ["إدارة الحاضنات", "Managing incubators", "idea"],
                ["تنظيم المؤتمرات", "Organizing conferences", "mic"], ["الابتكار الحكومي", "Government innovation", "building"],
            ]),
            ("university_services",
            [
                ["مراكز الابتكار", "Innovation Centers", "idea"], ["مراكز ريادة الأعمال", "Entrepreneurship Centers", "rocket"],
                ["مجتمع الشركات الناشئة", "Startup Clubs", "users"], ["هاكاثونات", "Hackathons", "cpu"],
                ["مسابقات", "Competitions", "award"], ["برامج حاضنات الأعمال", "Incubation Programs", "graduation"],
            ]),
            ("partner_types",
            [
                ["الوزارات", "Ministries", "building"], ["الجامعات", "Universities", "graduation"], ["المستثمرون", "Investors", "users"],
                ["البنوك", "Banks", "coins"], ["صناديق الاستثمار", "Investment Funds", "chart"], ["المستثمرون الملائكيون", "Angel Investors", "handshake"],
                ["المؤسسات الدولية", "International Institutions", "globe"], ["القطاع الخاص", "Private Sector", "briefcase"],
            ]),
            ("partners_in_success",
            [
                ["جامعة الدول العربية", "The League of Arab States", "globe"],
                ["وزارة الشباب والرياضة وغيرها من الوزارات التي تم الاتفاق معها", "The Ministry of Youth and Sports and other ministries we have agreements with", "building"],
                ["الجامعات التكنولوجية والأهلية", "Technological and private universities", "graduation"],
                ["البنوك وشركات الأموال", "Banks and financial companies", "coins"],
                ["شركات القطاع الخاص", "Private-sector companies", "briefcase"],
                ["صناديق الاستثمار", "Investment funds", "chart"],
            ]),
            ("network_steps",
            [
                ["الانضمام إلى الشبكة", "Join the network", "users",
                    "يتقدم المستثمر أو الشركة الناشئة بطلب الانضمام.", "An investor or a startup applies to join."],
                ["فحص الشركات الناشئة", "Startup screening", "search",
                    "يراجع فريقنا الشركات الناشئة ويجهزها للاستثمار.", "Our team reviews startups and prepares them for investment."],
                ["أيام العروض التقديمية", "Pitch days", "mic",
                    "تعرض الشركات الناشئة مشاريعها أمام المستثمرين.", "Startups present their projects to investors."],
                ["مطابقة الاستثمارات", "Investment matching", "link",
                    "نربط المستثمرين بالفرص التي تناسب اهتماماتهم.", "We connect investors with the opportunities that match their interests."],
            ]),
        ];

        private static void UpData(MigrationBuilder mb)
        {
            // 1. Pages: one row per route of the table, inserted only when its key is missing.
            var pageValues = string.Join(",\n", PageRows.Select(r =>
                "(" + L(r[0]).Replace("N'", "'") + ", " + r[1] + ", " + string.Join(", ", r.Skip(2).Select(L)) + ")"));
            mb.Sql("INSERT INTO Pages ([Key], SortOrder, TitleAr, TitleEn, SummaryAr, SummaryEn, IntroAr, IntroEn, BodyAr, BodyEn, " +
                   "EmptyTextAr, EmptyTextEn, MetaTitleAr, MetaTitleEn, MetaDescriptionAr, MetaDescriptionEn, ShowInMenu, UpdatedAt)\n" +
                   "SELECT v.K, v.S, v.TA, v.TE, v.SA, v.SE, v.IA, v.IE, v.BA, v.BE, v.EA, v.EE, v.MTA, v.MTE, v.MDA, v.MDE, 1, SYSUTCDATETIME() " +
                   "FROM (VALUES\n" + pageValues + "\n) v(K, S, TA, TE, SA, SE, IA, IE, BA, BE, EA, EE, MTA, MTE, MDA, MDE)\n" +
                   "WHERE NOT EXISTS (SELECT 1 FROM Pages p WHERE p.[Key] = v.K);");

            // 2. ContentItems: each group only when the table has no row of that group.
            foreach (var (group, rows) in ItemGroups)
            {
                var values = string.Join(",\n", rows.Select((r, i) =>
                    $"({L(r[0])}, {L(r[1])}, {(r[2] is null ? "NULL" : "'" + r[2] + "'")}, " +
                    $"{L(r.Length > 3 ? r[3] : null)}, {L(r.Length > 4 ? r[4] : null)}, {i + 1})"));
                mb.Sql($"IF NOT EXISTS (SELECT 1 FROM ContentItems WHERE [Group] = '{group}')\n" +
                       "INSERT INTO ContentItems ([Group], TextAr, TextEn, IconKey, DescriptionAr, DescriptionEn, SortOrder, IsActive, CreatedAt)\n" +
                       $"SELECT '{group}', v.TA, v.TE, v.I, v.DA, v.DE, v.S, 1, SYSUTCDATETIME() FROM (VALUES\n{values}\n) v(TA, TE, I, DA, DE, S);");
            }
        }
    }
}
