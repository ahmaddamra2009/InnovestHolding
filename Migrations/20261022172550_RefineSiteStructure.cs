using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovestHolding.Migrations
{
    /// <summary>
    /// Drops the single-column Platforms(SortOrder) index (the (IsActive, SortOrder) index serves the same reads) and shortens
    /// five English meta titles to fit 60 characters with the " | site name" suffix, and two English meta descriptions to
    /// 160 characters. Each column changes only while it still equals the value AddSiteStructure wrote (binary comparison),
    /// so an admin edit is never overwritten; Down puts a value back only while it still equals what Up wrote.
    /// </summary>
    public partial class RefineSiteStructure : Migration
    {
        private const string Bin = "COLLATE Latin1_General_100_BIN2";

        private static string L(string value) => "N'" + value.Replace("'", "''") + "'";

        /// <summary>Page key, column, value written by AddSiteStructure, new value.</summary>
        private static readonly string[][] Changes =
        [
            ["home", "MetaTitleEn", "The Arab Platform for Entrepreneurship, Innovation & Investment", "Arab Platform for Innovation & Investment"],
            ["about", "MetaTitleEn", "About Innovest Holding: Vision, Mission and Values", "About Us: Vision, Mission and Values"],
            ["ecosystem", "MetaTitleEn", "The Innovest Entrepreneurship & Investment Ecosystem", "Entrepreneurship & Investment Ecosystem"],
            ["forum", "MetaTitleEn", "Innovest Arab Forum for Innovation & Investment", "Innovest Arab Forum for Investment"],
            ["network.investors", "MetaTitleEn", "Services for Investors in the Innovest Network", "Services for Angel Investors"],
            ["ecosystem", "MetaDescriptionEn",
                "The Innovest Arab Forum, the Arab Angel Investors Network and the TV Program, with our services and programmes for startups, investors, governments and universities.",
                "The Innovest Arab Forum, the Arab Angel Investors Network and the TV Program, with services for startups, investors, governments and universities."],
            ["forum", "MetaDescriptionEn",
                "The largest Arab forum for innovation, investment and entrepreneurship: a startup expo, an investment forum, an AI summit, a pitch competition and innovation awards.",
                "The largest Arab forum for innovation and investment: a startup expo, an investment forum, an AI summit, a pitch competition and innovation awards."],
        ];

        private static string Update(string key, string column, string from, string to) =>
            $"UPDATE Pages SET [{column}] = {L(to)}, UpdatedAt = SYSUTCDATETIME() WHERE [Key] = {L(key)} AND [{column}] = {L(from)} {Bin};";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Platforms_SortOrder",
                table: "Platforms");

            foreach (var c in Changes)
                migrationBuilder.Sql(Update(c[0], c[1], c[2], c[3]));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var c in Changes)
                migrationBuilder.Sql(Update(c[0], c[1], c[3], c[2]));

            migrationBuilder.CreateIndex(
                name: "IX_Platforms_SortOrder",
                table: "Platforms",
                column: "SortOrder");
        }
    }
}
