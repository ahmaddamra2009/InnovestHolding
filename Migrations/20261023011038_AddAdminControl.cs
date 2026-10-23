using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovestHolding.Migrations
{
    /// <summary>
    /// C5 — full admin control. Schema: PageSections, FooterColumns, FooterLinks (new); Pages.IsPublished /
    /// ShowInFooter / EmptyActionPage, NavItems.ShowInHeader, Platforms.IconKey, Stats.IsActive, SocialLinks.Label /
    /// IconKey (new columns whose defaults reproduce the C4 rendering: existing rows get 1 / NULL); the unique index on
    /// SocialLinks.Network becomes filtered (custom rows may repeat — keep SET QUOTED_IDENTIFIER ON in scripts). Data:
    /// AddAdminControl.Data.cs (inserts only). Nothing is dropped.
    /// </summary>
    public partial class AddAdminControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SocialLinks_Network",
                table: "SocialLinks");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Stats",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "IconKey",
                table: "SocialLinks",
                type: "varchar(40)",
                unicode: false,
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "SocialLinks",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IconKey",
                table: "Platforms",
                type: "varchar(40)",
                unicode: false,
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmptyActionPage",
                table: "Pages",
                type: "varchar(60)",
                unicode: false,
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Pages",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowInFooter",
                table: "Pages",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowInHeader",
                table: "NavItems",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "FooterColumns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TitleAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FooterColumns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PageSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PageKey = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    SectionKey = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageSections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FooterLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FooterColumnId = table.Column<int>(type: "int", nullable: false),
                    PageKey = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    NavItemId = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FooterLinks", x => x.Id);
                    table.CheckConstraint("CK_FooterLinks_Target", "([PageKey] IS NULL AND [NavItemId] IS NOT NULL) OR ([PageKey] IS NOT NULL AND [NavItemId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_FooterLinks_FooterColumns_FooterColumnId",
                        column: x => x.FooterColumnId,
                        principalTable: "FooterColumns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FooterLinks_NavItems_NavItemId",
                        column: x => x.NavItemId,
                        principalTable: "NavItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stats_IsActive_SortOrder",
                table: "Stats",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SocialLinks_Network",
                table: "SocialLinks",
                column: "Network",
                unique: true,
                filter: "[Network] <> N'custom'");

            migrationBuilder.CreateIndex(
                name: "IX_FooterLinks_FooterColumnId_SortOrder",
                table: "FooterLinks",
                columns: new[] { "FooterColumnId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_FooterLinks_NavItemId",
                table: "FooterLinks",
                column: "NavItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PageSections_PageKey_SectionKey",
                table: "PageSections",
                columns: new[] { "PageKey", "SectionKey" },
                unique: true);

            UpData(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DownData(migrationBuilder);

            migrationBuilder.DropTable(
                name: "FooterLinks");

            migrationBuilder.DropTable(
                name: "PageSections");

            migrationBuilder.DropTable(
                name: "FooterColumns");

            migrationBuilder.DropIndex(
                name: "IX_Stats_IsActive_SortOrder",
                table: "Stats");

            migrationBuilder.DropIndex(
                name: "IX_SocialLinks_Network",
                table: "SocialLinks");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Stats");

            migrationBuilder.DropColumn(
                name: "IconKey",
                table: "SocialLinks");

            migrationBuilder.DropColumn(
                name: "Label",
                table: "SocialLinks");

            migrationBuilder.DropColumn(
                name: "IconKey",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "EmptyActionPage",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "ShowInFooter",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "ShowInHeader",
                table: "NavItems");

            // The previous index is unique over every row: custom networks (which did not exist before) leave first.
            // Down is a local rollback only — never run on production (restore the backup instead).
            migrationBuilder.Sql("DELETE FROM SocialLinks WHERE Network = N'custom';");

            migrationBuilder.CreateIndex(
                name: "IX_SocialLinks_Network",
                table: "SocialLinks",
                column: "Network",
                unique: true);
        }
    }
}
