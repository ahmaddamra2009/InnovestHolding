using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovestHolding.Migrations
{
    /// <summary>
    /// The multi-page site: Pages (one row per route of Infrastructure/Site/SiteRoutes), ContentItems (the bilingual
    /// lists of the company profile), IsActive on Platforms and Partners (existing rows stay visible: default 1) and
    /// the (IsActive, SortOrder) indexes of the public lists. Down drops only what Up created.
    /// </summary>
    public partial class AddSiteStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_News_SortOrder",
                table: "News");

            migrationBuilder.DropIndex(
                name: "IX_Events_SortOrder",
                table: "Events");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Platforms",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Partners",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "ContentItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Group = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    TextAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TextEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DescriptionAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IconKey = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SummaryAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SummaryEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IntroAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IntroEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BodyAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BodyEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmptyTextAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EmptyTextEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MetaTitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MetaTitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetaDescriptionAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    MetaDescriptionEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ShowInMenu = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Platforms_IsActive_SortOrder",
                table: "Platforms",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Partners_IsActive_SortOrder",
                table: "Partners",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_News_IsActive_SortOrder",
                table: "News",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_IsActive_SortOrder",
                table: "Events",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_Group_IsActive_SortOrder",
                table: "ContentItems",
                columns: new[] { "Group", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Pages_Key",
                table: "Pages",
                column: "Key",
                unique: true);

            // Reference rows (Pages, ContentItems): AddSiteStructure.Data.cs.
            UpData(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentItems");

            migrationBuilder.DropTable(
                name: "Pages");

            migrationBuilder.DropIndex(
                name: "IX_Platforms_IsActive_SortOrder",
                table: "Platforms");

            migrationBuilder.DropIndex(
                name: "IX_Partners_IsActive_SortOrder",
                table: "Partners");

            migrationBuilder.DropIndex(
                name: "IX_News_IsActive_SortOrder",
                table: "News");

            migrationBuilder.DropIndex(
                name: "IX_Events_IsActive_SortOrder",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Partners");

            migrationBuilder.CreateIndex(
                name: "IX_News_SortOrder",
                table: "News",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Events_SortOrder",
                table: "Events",
                column: "SortOrder");
        }
    }
}
