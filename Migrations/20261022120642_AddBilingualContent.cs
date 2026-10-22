using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovestHolding.Migrations
{
    /// <summary>
    /// Every Arabic text column is RENAMED to …Ar (sp_rename — data, type, length and nullability kept;
    /// never drop + add), English columns are added, three tables are created, and the reference data that
    /// existing databases need is written here (DbSeeder does nothing once a user exists).
    /// </summary>
    public partial class AddBilingualContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Label",
                table: "Stats",
                newName: "LabelAr");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Platforms",
                newName: "TitleAr");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Platforms",
                newName: "DescriptionAr");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Partners",
                newName: "NameAr");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "News",
                newName: "TitleAr");

            migrationBuilder.RenameColumn(
                name: "Summary",
                table: "News",
                newName: "SummaryAr");

            migrationBuilder.RenameColumn(
                name: "NewsDate",
                table: "News",
                newName: "NewsDateAr");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "News",
                newName: "CategoryAr");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "NavSubItems",
                newName: "TitleAr");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "NavItems",
                newName: "TitleAr");

            migrationBuilder.RenameColumn(
                name: "Question",
                table: "Faqs",
                newName: "QuestionAr");

            migrationBuilder.RenameColumn(
                name: "Answer",
                table: "Faqs",
                newName: "AnswerAr");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Events",
                newName: "TitleAr");

            migrationBuilder.RenameColumn(
                name: "Time",
                table: "Events",
                newName: "TimeAr");

            migrationBuilder.RenameColumn(
                name: "Location",
                table: "Events",
                newName: "LocationAr");

            migrationBuilder.RenameColumn(
                name: "EventMonth",
                table: "Events",
                newName: "EventMonthAr");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Events",
                newName: "DescriptionAr");

            migrationBuilder.AddColumn<string>(
                name: "LabelEn",
                table: "Stats",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortLabelAr",
                table: "Stats",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortLabelEn",
                table: "Stats",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Culture",
                table: "Registrations",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "ar");

            migrationBuilder.AddColumn<int>(
                name: "InterestOptionId",
                table: "Registrations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "Platforms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkUrl",
                table: "Platforms",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortTitleAr",
                table: "Platforms",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortTitleEn",
                table: "Platforms",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowInFooter",
                table: "Platforms",
                type: "bit",
                nullable: false,
                defaultValue: true); // every existing platform stays in the footer except the one cleared in UpdateEntityEnglish

            migrationBuilder.AddColumn<string>(
                name: "TitleEn",
                table: "Platforms",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "Partners",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryEn",
                table: "News",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkUrl",
                table: "News",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewsDateEn",
                table: "News",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "News",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SummaryEn",
                table: "News",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleEn",
                table: "News",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleEn",
                table: "NavSubItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowInFooter",
                table: "NavItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TitleEn",
                table: "NavItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnswerEn",
                table: "Faqs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuestionEn",
                table: "Faqs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventMonthEn",
                table: "Events",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationEn",
                table: "Events",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeEn",
                table: "Events",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleEn",
                table: "Events",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Culture",
                table: "ContactMessages",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "ar");

            migrationBuilder.CreateTable(
                name: "InterestOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterestOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegisterBenefits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TextAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TextEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegisterBenefits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SocialLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Network = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialLinks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_CreatedAt",
                table: "Registrations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_InterestOptionId",
                table: "Registrations",
                column: "InterestOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_News_SortOrder",
                table: "News",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessages_CreatedAt",
                table: "ContactMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InterestOptions_IsActive_SortOrder",
                table: "InterestOptions",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_RegisterBenefits_IsActive_SortOrder",
                table: "RegisterBenefits",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SocialLinks_Network",
                table: "SocialLinks",
                column: "Network",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Registrations_InterestOptions_InterestOptionId",
                table: "Registrations",
                column: "InterestOptionId",
                principalTable: "InterestOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // ── Data (frozen SQL in AddBilingualContent.Data.cs). Each step is its own command: SQL Server
            //    compiles a batch at once, so no UPDATE shares a batch with the ALTER that created its column. ──
            UpData(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DownData(migrationBuilder);

            migrationBuilder.DropForeignKey(
                name: "FK_Registrations_InterestOptions_InterestOptionId",
                table: "Registrations");

            migrationBuilder.DropTable(
                name: "InterestOptions");

            migrationBuilder.DropTable(
                name: "RegisterBenefits");

            migrationBuilder.DropTable(
                name: "SocialLinks");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_CreatedAt",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_InterestOptionId",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_News_SortOrder",
                table: "News");

            migrationBuilder.DropIndex(
                name: "IX_ContactMessages_CreatedAt",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "LabelEn",
                table: "Stats");

            migrationBuilder.DropColumn(
                name: "ShortLabelAr",
                table: "Stats");

            migrationBuilder.DropColumn(
                name: "ShortLabelEn",
                table: "Stats");

            migrationBuilder.DropColumn(
                name: "Culture",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "InterestOptionId",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "LinkUrl",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "ShortTitleAr",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "ShortTitleEn",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "ShowInFooter",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "TitleEn",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "CategoryEn",
                table: "News");

            migrationBuilder.DropColumn(
                name: "LinkUrl",
                table: "News");

            migrationBuilder.DropColumn(
                name: "NewsDateEn",
                table: "News");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "News");

            migrationBuilder.DropColumn(
                name: "SummaryEn",
                table: "News");

            migrationBuilder.DropColumn(
                name: "TitleEn",
                table: "News");

            migrationBuilder.DropColumn(
                name: "TitleEn",
                table: "NavSubItems");

            migrationBuilder.DropColumn(
                name: "ShowInFooter",
                table: "NavItems");

            migrationBuilder.DropColumn(
                name: "TitleEn",
                table: "NavItems");

            migrationBuilder.DropColumn(
                name: "AnswerEn",
                table: "Faqs");

            migrationBuilder.DropColumn(
                name: "QuestionEn",
                table: "Faqs");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "EventMonthEn",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "LocationEn",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "TimeEn",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "TitleEn",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Culture",
                table: "ContactMessages");

            migrationBuilder.RenameColumn(
                name: "LabelAr",
                table: "Stats",
                newName: "Label");

            migrationBuilder.RenameColumn(
                name: "TitleAr",
                table: "Platforms",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "DescriptionAr",
                table: "Platforms",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "NameAr",
                table: "Partners",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "TitleAr",
                table: "News",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "SummaryAr",
                table: "News",
                newName: "Summary");

            migrationBuilder.RenameColumn(
                name: "NewsDateAr",
                table: "News",
                newName: "NewsDate");

            migrationBuilder.RenameColumn(
                name: "CategoryAr",
                table: "News",
                newName: "Category");

            migrationBuilder.RenameColumn(
                name: "TitleAr",
                table: "NavSubItems",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "TitleAr",
                table: "NavItems",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "QuestionAr",
                table: "Faqs",
                newName: "Question");

            migrationBuilder.RenameColumn(
                name: "AnswerAr",
                table: "Faqs",
                newName: "Answer");

            migrationBuilder.RenameColumn(
                name: "TitleAr",
                table: "Events",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "TimeAr",
                table: "Events",
                newName: "Time");

            migrationBuilder.RenameColumn(
                name: "LocationAr",
                table: "Events",
                newName: "Location");

            migrationBuilder.RenameColumn(
                name: "EventMonthAr",
                table: "Events",
                newName: "EventMonth");

            migrationBuilder.RenameColumn(
                name: "DescriptionAr",
                table: "Events",
                newName: "Description");
        }
    }
}
