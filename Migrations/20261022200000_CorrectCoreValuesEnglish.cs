using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovestHolding.Migrations
{
    /// <summary>
    /// Data only. The first text of ApplyCompanyProfile wrote the English core values as one line ending "impact and
    /// empowerment", so the home page showed 6 chips for the 7 Arabic values; that migration now writes the corrected text.
    /// This migration corrects a database that already ran the first text, only while <c>values_text</c>'s English is still
    /// exactly that text (binary comparison) — an admin edit is never replaced. Down does nothing: ApplyCompanyProfile's
    /// own Down restores the earlier English from the corrected value.
    /// </summary>
    public partial class CorrectCoreValuesEnglish : Migration
    {
        private const string Bin = "COLLATE Latin1_General_100_BIN2";
        private const string First = "Innovation, partnership, transparency, sustainability, professionalism, impact and empowerment";
        private const string Corrected = "Innovation, Partnership, Transparency, Sustainability, Professionalism, Impact, Empowerment";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"UPDATE SiteContents SET ContentEn = N'{Corrected}', UpdatedAt = SYSUTCDATETIME() " +
                                 $"WHERE SectionKey = N'values_text' AND ContentEn = N'{First}' {Bin};");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
