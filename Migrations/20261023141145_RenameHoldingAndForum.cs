using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovestHolding.Migrations
{
    /// <summary>
    /// N8 — naming. Arabic «هولدينغ» becomes «القابضة» («إنوفست هولدينغ» → «إنوفست القابضة»); the forum is «منتدى إنوفست» /
    /// "Innovest Forum" (was «منتدى إنوفست العربي» / "Innovest Arab Forum"). English "Innovest Holding" is not touched.
    /// Data only; the EF model does not change.
    ///
    /// Rules (Security early checklist, feature/8-naming):
    /// - Every (table, column) pair is a constant below: display-text columns of CONTENT tables only. The lead tables
    ///   (Registrations, ContactMessages, NetworkApplications, SponsorshipEnquiries, OpportunityInterests), Users and
    ///   SlugRedirects are never named, nor any slug / Href / Url / FileKey / key / email / phone column. Lead snapshot
    ///   columns (EventTitle, PackageTitle, OpportunityTitle, Interest) record what the visitor saw and stay as they are.
    /// - SiteContents: every key except the frozen <see cref="NonTextKeys"/> (images, colour, links, counts, icons,
    ///   contact data, system markers); brand_positioning and the ui.* overrides are text and are included.
    /// - The find/replace pairs are frozen literals, applied longest first, matched and replaced with a binary collation.
    ///   Each UPDATE is guarded by a binary LIKE on the old phrases, so rows without them are not written and a second
    ///   run changes nothing. Rows an admin edited are included (CTO decision: brand rule).
    /// - No pair makes a value longer (old length ≥ new length, pinned by a test), so no value can outgrow its column.
    /// - Down (option a): Up records (table, column, id, old value, new value) of every value it changes in the
    ///   migration-owned table <see cref="LogTable"/>. Down restores a value only while it still equals what Up wrote
    ///   (binary comparison), then drops the table. Text that said «القابضة» before Up, or that an admin changed after
    ///   Up, is left alone.
    /// - Raw SQL bypasses the content-cache interceptor; this is safe because migrations run at startup, before the
    ///   in-memory snapshot exists. No runtime copy of these REPLACEs exists.
    /// UpdatedAt is not touched (a rename, not an edit).
    /// </summary>
    public partial class RenameHoldingAndForum : Migration
    {
        private const string Bin = "COLLATE Latin1_General_100_BIN2";

        /// <summary>Migration-owned log of the values Up changed (read by Down, then dropped). Not an EF model table.</summary>
        public const string LogTable = "__RenameHoldingAndForumLog";

        /// <summary>Find → replace, applied in this order (longest phrase first, so no rule consumes part of another).</summary>
        public static readonly (string Find, string Replace)[] Pairs =
        [
            ("منتدى إنوفست العربي", "منتدى إنوفست"),
            ("المنتدى العربي", "المنتدى"),
            ("Innovest Arab Forum", "Innovest Forum"),
            ("INNOVEST ARAB FORUM", "INNOVEST FORUM"),
            ("Arab Forum", "Forum"), // case-sensitive: "the Arab Forum" → "the Forum"; the generic "Arab forum" stays
            ("هولدينغ", "القابضة"),
        ];

        /// <summary>
        /// The display-text columns of the content tables (discovered from AppDbContext / INFORMATION_SCHEMA on the C5
        /// schema, then written out; never discovered at run time). Every pair is applied to every column: the Arabic
        /// phrases do not occur in English text and vice versa, and English "Holding" matches no pair.
        /// </summary>
        public static readonly (string Table, string[] Columns)[] Targets =
        [
            ("SiteContents", ["ContentAr", "ContentEn"]),
            ("Pages", ["TitleAr", "TitleEn", "SummaryAr", "SummaryEn", "IntroAr", "IntroEn", "BodyAr", "BodyEn",
                "EmptyTextAr", "EmptyTextEn", "MetaTitleAr", "MetaTitleEn", "MetaDescriptionAr", "MetaDescriptionEn"]),
            ("NavItems", ["TitleAr", "TitleEn"]),
            ("NavSubItems", ["TitleAr", "TitleEn"]),
            ("FooterColumns", ["TitleAr", "TitleEn"]),
            ("Platforms", ["TitleAr", "TitleEn", "ShortTitleAr", "ShortTitleEn", "DescriptionAr", "DescriptionEn"]),
            ("News", ["TitleAr", "TitleEn", "SummaryAr", "SummaryEn", "BodyAr", "BodyEn", "CategoryAr", "CategoryEn",
                "NewsDateAr", "NewsDateEn", "MetaTitleAr", "MetaTitleEn", "MetaDescriptionAr", "MetaDescriptionEn"]),
            ("Events", ["TitleAr", "TitleEn", "SummaryAr", "SummaryEn", "DescriptionAr", "DescriptionEn", "EventMonthAr",
                "EventMonthEn", "LocationAr", "LocationEn", "TimeAr", "TimeEn", "CityAr", "CityEn", "AddressAr", "AddressEn",
                "MetaTitleAr", "MetaTitleEn", "MetaDescriptionAr", "MetaDescriptionEn"]),
            ("Faqs", ["QuestionAr", "QuestionEn", "AnswerAr", "AnswerEn"]),
            ("Stats", ["LabelAr", "LabelEn", "ShortLabelAr", "ShortLabelEn"]),
            ("Partners", ["NameAr", "NameEn"]),
            ("PartnerGroups", ["NameAr", "NameEn"]),
            ("ContentItems", ["TextAr", "TextEn", "DescriptionAr", "DescriptionEn"]),
            ("MediaFiles", ["AltAr", "AltEn"]),
            ("RegisterBenefits", ["TextAr", "TextEn"]),
            ("InterestOptions", ["NameAr", "NameEn"]),
            ("SocialLinks", ["Label"]),
            ("Speakers", ["NameAr", "NameEn", "JobTitleAr", "JobTitleEn", "OrganizationAr", "OrganizationEn", "BioAr", "BioEn",
                "MetaTitleAr", "MetaTitleEn", "MetaDescriptionAr", "MetaDescriptionEn"]),
            ("TvGuests", ["NameAr", "NameEn", "JobTitleAr", "JobTitleEn", "OrganizationAr", "OrganizationEn", "BioAr", "BioEn",
                "MetaTitleAr", "MetaTitleEn", "MetaDescriptionAr", "MetaDescriptionEn"]),
            ("TvEpisodes", ["TitleAr", "TitleEn", "SummaryAr", "SummaryEn", "BodyAr", "BodyEn", "MetaTitleAr", "MetaTitleEn",
                "MetaDescriptionAr", "MetaDescriptionEn"]),
            ("InvestmentOpportunities", ["TitleAr", "TitleEn", "SummaryAr", "SummaryEn", "BodyAr", "BodyEn", "StageTextAr",
                "StageTextEn", "AskTextAr", "AskTextEn", "MetaTitleAr", "MetaTitleEn", "MetaDescriptionAr", "MetaDescriptionEn"]),
            ("SponsorshipPackages", ["TitleAr", "TitleEn", "PriceTextAr", "PriceTextEn", "BenefitsAr", "BenefitsEn"]),
        ];

        /// <summary>
        /// SiteContents keys that hold no display text (FileKeys, colour, links, counts, icons, page keys, contact data,
        /// system markers). Frozen; a test pins it to cover ContentKeys (Neutral or with a rule), SiteImages and the markers.
        /// </summary>
        public static readonly string[] NonTextKeys =
        [
            // images and colour (SiteImages, BrandImages)
            "brand_favicon", "brand_apple_touch_icon", "brand_og_default", "brand_theme_color",
            "logo_on_dark", "logo_on_light", "hero_image", "founder_photo",
            // system markers
            "sys_timestamps_utc", "sys_media_instance",
            // links, page keys, counts, offsets
            "header_cta_page", "hero_cta_main_link", "hero_cta_secondary_link", "about_more_link", "cta_band_button1_link",
            "cta_band_button2_link", "events_action_link", "tv_action_link", "notfound_pages", "home_news_limit",
            "list_page_size", "hero_stats_limit", "home_events_limit", "forum_events_limit", "default_utc_offset",
            // icons
            "vision_icon", "mission_icon", "values_icon", "ecosystem_icon", "platforms_icon", "events_icon", "news_icon",
            "partners_icon", "faq_icon", "contact_icon", "register_icon",
            // contact and SEO data
            "contact_phone", "contact_email", "contact_whatsapp", "contact_website", "footer_credit_url", "seo_og_image",
            "site_base_url", "org_address_locality", "org_address_country",
        ];

        private static string L(string value) => "N'" + value.Replace("'", "''") + "'";

        /// <summary>The column with every pair applied (binary collation), innermost = first pair.</summary>
        private static string Replaced(string column)
        {
            var expr = $"[{column}] {Bin}";
            foreach (var (find, replace) in Pairs)
                expr = $"REPLACE({expr}, {L(find)}, {L(replace)})";
            return expr;
        }

        /// <summary>Rows whose column holds at least one old phrase (binary LIKE), plus the SiteContents key filter.</summary>
        private static string Guard(string table, string column)
        {
            var any = string.Join(" OR ", Pairs.Select(p => $"[{column}] {Bin} LIKE {L("%" + p.Find + "%")}"));
            var keys = table == "SiteContents"
                ? " AND [SectionKey] NOT IN (" + string.Join(", ", NonTextKeys.Select(L)) + ")"
                : "";
            return $"({any}){keys}";
        }

        /// <summary>The statements of Up, in order (the tests run them a second time to prove a re-run is a no-op).</summary>
        public static IReadOnlyList<string> UpSql()
        {
            var sql = new List<string>
            {
                $"IF OBJECT_ID(N'[{LogTable}]') IS NULL CREATE TABLE [{LogTable}] (" +
                "[Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY, [TableName] varchar(64) NOT NULL, [ColumnName] varchar(64) NOT NULL, " +
                "[RowId] int NOT NULL, [OldValue] nvarchar(max) NOT NULL, [NewValue] nvarchar(max) NOT NULL);",
            };
            foreach (var (table, columns) in Targets)
                foreach (var column in columns)
                {
                    var where = Guard(table, column);
                    sql.Add($"INSERT INTO [{LogTable}] ([TableName], [ColumnName], [RowId], [OldValue], [NewValue]) " +
                            $"SELECT '{table}', '{column}', [Id], [{column}], {Replaced(column)} FROM [{table}] WHERE {where};");
                    sql.Add($"UPDATE [{table}] SET [{column}] = {Replaced(column)} WHERE {where};");
                }
            return sql;
        }

        /// <summary>The statements of Down: restore what Up changed, only where the value is still Up's; drop the log.</summary>
        public static IReadOnlyList<string> DownSql()
        {
            var sql = new List<string>();
            foreach (var (table, columns) in Targets)
                foreach (var column in columns)
                    sql.Add($"UPDATE t SET t.[{column}] = l.[OldValue] FROM [{table}] t " +
                            $"JOIN [{LogTable}] l ON l.[TableName] = '{table}' AND l.[ColumnName] = '{column}' AND l.[RowId] = t.[Id] " +
                            $"WHERE t.[{column}] {Bin} = l.[NewValue] {Bin};");
            sql.Add($"DROP TABLE [{LogTable}];");
            return sql;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var s in UpSql()) migrationBuilder.Sql(s);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var s in DownSql()) migrationBuilder.Sql(s);
        }
    }
}
