using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovestHolding.Migrations
{
    /// <summary>
    /// F10 — the forum content of the event profile (FORUM-CONTENT.md), all admin-editable. Schema: ForumEditions +
    /// ForumEditionPhotos (new), Events.DateIsMonth and PartnerGroups.ShowOnForumHub (bit NOT NULL DEFAULT 0: existing rows
    /// render as before). Data: AddForumContent.Data.cs (inserts only where missing; one guarded swap of the untouched
    /// forum_components rows). The bundled photos and logos are NOT here: Infrastructure/Content/ForumMediaImport imports
    /// them once at startup. Runs after N8's RenameHoldingAndForum; no literal holds an old name (a test pins it).
    /// </summary>
    public partial class AddForumContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowOnForumHub",
                table: "PartnerGroups",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DateIsMonth",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ForumEditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<short>(type: "smallint", nullable: false),
                    EditionLabelAr = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false, defaultValue: ""),
                    EditionLabelEn = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    TitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PlaceAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    PlaceEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TextAr = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false, defaultValue: ""),
                    TextEn = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumEditions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ForumEditionPhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ForumEditionId = table.Column<int>(type: "int", nullable: false),
                    ImageId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumEditionPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForumEditionPhotos_ForumEditions_ForumEditionId",
                        column: x => x.ForumEditionId,
                        principalTable: "ForumEditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ForumEditionPhotos_MediaFiles_ImageId",
                        column: x => x.ImageId,
                        principalTable: "MediaFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForumEditionPhotos_ForumEditionId_SortOrder",
                table: "ForumEditionPhotos",
                columns: new[] { "ForumEditionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ForumEditionPhotos_ImageId",
                table: "ForumEditionPhotos",
                column: "ImageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ForumEditions_IsActive_SortOrder",
                table: "ForumEditions",
                columns: new[] { "IsActive", "SortOrder" });

            UpData(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DownData(migrationBuilder);

            migrationBuilder.DropTable(
                name: "ForumEditionPhotos");

            migrationBuilder.DropTable(
                name: "ForumEditions");

            migrationBuilder.DropColumn(
                name: "ShowOnForumHub",
                table: "PartnerGroups");

            migrationBuilder.DropColumn(
                name: "DateIsMonth",
                table: "Events");
        }
    }
}
