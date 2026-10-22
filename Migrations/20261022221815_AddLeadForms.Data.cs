using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovestHolding.Migrations
{
    /// <summary>
    /// The reference rows of AddLeadForms. Every literal is FROZEN here (never read from application code).
    /// Guard polarity (ANTIGRAVITY §3), so a second run — or rows an admin already edited — make each step do nothing:
    /// a choice list is inserted only while the table has no row of that group; a content key only while it is missing;
    /// a page intro is changed only while it still holds the value AddSiteStructure wrote (binary comparison). Down undoes
    /// a row only while it still equals what Up wrote. No existing row is deleted by Up.
    /// </summary>
    public partial class AddLeadForms
    {
        private const string Bin = "COLLATE Latin1_General_100_BIN2";

        /// <summary>N'…' with quotes doubled; NULL for null.</summary>
        private static string L(string value) => value is null ? "NULL" : "N'" + value.Replace("'", "''") + "'";

        /// <summary>The choices of the «Join the Network» form: group → (Arabic, English).</summary>
        private static readonly (string Group, string[][] Rows)[] OptionGroups =
        [
            ("investor_types",
            [
                ["مستثمر ملائكي", "Angel investor"],
                ["صندوق استثمار", "Investment fund"],
                ["مكتب عائلي", "Family office"],
                ["مستثمر مؤسسي (شركة)", "Corporate investor"],
                ["أخرى", "Other"],
            ]),
            ("ticket_sizes",
            [
                ["أقل من 25 ألف دولار", "Under $25K"],
                ["من 25 إلى 100 ألف دولار", "$25K – $100K"],
                ["من 100 إلى 500 ألف دولار", "$100K – $500K"],
                ["أكثر من 500 ألف دولار", "Over $500K"],
            ]),
            ("startup_stages",
            [
                ["فكرة", "Idea"],
                ["نموذج أولي", "Prototype / MVP"],
                ["إيرادات أولية", "Early revenue"],
                ["نمو", "Growth"],
                ["توسّع", "Scale-up"],
            ]),
            ("funding_ranges",
            [
                ["أقل من 50 ألف دولار", "Under $50K"],
                ["من 50 إلى 250 ألف دولار", "$50K – $250K"],
                ["من 250 ألفاً إلى مليون دولار", "$250K – $1M"],
                ["أكثر من مليون دولار", "Over $1M"],
            ]),
        ];

        /// <summary>New SiteContents keys: key, Arabic, English.</summary>
        private static readonly string[][] NewKeys =
        [
            ["forms_consent",
                "أوافق على أن تتواصل معي إنوفست هولدينغ بخصوص طلبي، وأن تحتفظ ببياناتي لهذا الغرض فقط.",
                "I agree that Innovest Holding may contact me about my request and keep my details for that purpose only."],
            ["join_kind_title", "أرغب في الانضمام بصفتي", "I would like to join as"],
            ["join_side_title", "كيف {تنضم} إلى الشبكة", "How to {join} the network"],
            ["join_side_text",
                "أرسل طلبك وسيراجعه فريق الشبكة، ثم نتواصل معك لاستكمال الخطوات.",
                "Send your application: the network team reviews it and contacts you to complete the next steps."],
            ["sponsorship_form_title", "{اطلب} عرض الرعاية", "{Request} a sponsorship offer"],
            ["sponsorship_form_text",
                "أخبرنا عن شركتك وأهدافك من الرعاية، ونرسل لك عرضاً يناسبها.",
                "Tell us about your company and your sponsorship goals, and we will send you an offer that fits them."],
            ["interest_form_title", "{أبدِ اهتمامك} بالفرص الاستثمارية", "{Express interest} in investment opportunities"],
            ["interest_form_text",
                "سجّل اهتمامك وسيتواصل معك فريق الشبكة عند نشر فرص تناسبك.",
                "Register your interest and the network team will contact you when matching opportunities are published."],
        ];

        /// <summary>Page key, column, value AddSiteStructure wrote (null = NULL), new value.</summary>
        private static readonly string[][] PageChanges =
        [
            ["network.join", "IntroAr",
                "يمكن للمستثمرين وللشركات الناشئة الانضمام إلى شبكة المستثمرين الملائكيين العرب. تواصل معنا وسيتابع فريقنا طلبك.",
                "يمكن للمستثمرين وللشركات الناشئة الانضمام إلى شبكة المستثمرين الملائكيين العرب. املأ النموذج أدناه وسيتابع فريقنا طلبك."],
            ["network.join", "IntroEn",
                "Investors and startups can join the Arab Angel Investors Network. Contact us and our team will follow up on your request.",
                "Investors and startups can join the Arab Angel Investors Network. Fill in the form below and our team will follow up on your request."],
            ["tv.sponsorship", "IntroAr", "",
                "سواء كنت ترغب في رعاية البرنامج التلفزيوني أو منتدى إنوفست العربي، أرسل استفسارك وسيتواصل معك فريقنا بالباقات المتاحة."],
            ["tv.sponsorship", "IntroEn", null,
                "Whether you want to sponsor the TV Program or the Innovest Arab Forum, send your enquiry and our team will contact you with the available packages."],
        ];

        private static string PageUpdate(string key, string column, string from, string to) =>
            $"UPDATE Pages SET [{column}] = {L(to)}, UpdatedAt = SYSUTCDATETIME() WHERE [Key] = {L(key)} AND " +
            (from is null ? $"[{column}] IS NULL;" : $"[{column}] = {L(from)} {Bin};");

        private static void UpData(MigrationBuilder mb)
        {
            // 1. The Join form's choice lists, each only when the table has no row of that group.
            foreach (var (group, rows) in OptionGroups)
            {
                var values = string.Join(",\n", rows.Select((r, i) => $"({L(r[0])}, {L(r[1])}, {i + 1})"));
                mb.Sql($"IF NOT EXISTS (SELECT 1 FROM ContentItems WHERE [Group] = '{group}')\n" +
                       "INSERT INTO ContentItems ([Group], TextAr, TextEn, SortOrder, IsActive, CreatedAt)\n" +
                       $"SELECT '{group}', v.TA, v.TE, v.S, 1, SYSUTCDATETIME() FROM (VALUES\n{values}\n) v(TA, TE, S);");
            }

            // 2. New content keys, each only when missing (IX_SiteContents_SectionKey is unique).
            var keys = string.Join(",\n", NewKeys.Select(r => $"({L(r[0])}, {L(r[1])}, {L(r[2])})"));
            mb.Sql("INSERT INTO SiteContents (SectionKey, ContentAr, ContentEn, UpdatedAt)\n" +
                   $"SELECT v.K, v.Ar, v.En, SYSUTCDATETIME() FROM (VALUES\n{keys}\n) v(K, Ar, En) " +
                   "WHERE NOT EXISTS (SELECT 1 FROM SiteContents s WHERE s.SectionKey = v.K);");

            // 3. Page intros that pointed to the contact form, or were empty, now introduce the form.
            foreach (var c in PageChanges)
                mb.Sql(PageUpdate(c[0], c[1], c[2], c[3]));
        }

        private static void DownData(MigrationBuilder mb)
        {
            foreach (var c in PageChanges)
                mb.Sql(PageUpdate(c[0], c[1], c[3], c[2]));

            // Rows Up inserted, only while they still hold Up's values (an admin edit stays).
            foreach (var r in NewKeys)
                mb.Sql($"DELETE FROM SiteContents WHERE SectionKey = {L(r[0])} AND ContentAr = {L(r[1])} {Bin} AND ContentEn = {L(r[2])} {Bin};");
            foreach (var (group, rows) in OptionGroups)
                foreach (var r in rows)
                    mb.Sql($"DELETE FROM ContentItems WHERE [Group] = '{group}' AND TextAr = {L(r[0])} {Bin} AND TextEn = {L(r[1])} {Bin};");
        }
    }
}
