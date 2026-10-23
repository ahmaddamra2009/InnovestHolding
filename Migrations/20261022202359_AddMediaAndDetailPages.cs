using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovestHolding.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaAndDetailPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Partners",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<int>(
                name: "LogoId",
                table: "Partners",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PartnerGroupId",
                table: "Partners",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowOnHome",
                table: "Partners",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Partners",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Partners",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageId",
                table: "Pages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyAr",
                table: "News",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BodyEn",
                table: "News",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageId",
                table: "News",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDescriptionAr",
                table: "News",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MetaDescriptionEn",
                table: "News",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaTitleAr",
                table: "News",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MetaTitleEn",
                table: "News",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PublishedOn",
                table: "News",
                type: "date",
                nullable: true);

            // Slug: added NULL, every existing row gets news-{Id} (unique by construction), then NOT NULL. The unique
            // index is created below, after the backfill.
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "News",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            // EXEC: compiled when it runs, so a generated script (one batch) can reference the column added just above.
            migrationBuilder.Sql("EXEC(N'UPDATE News SET Slug = ''news-'' + CAST(Id AS varchar(12)) WHERE Slug IS NULL;');");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "News",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "News",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "AddressAr",
                table: "Events",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AddressEn",
                table: "Events",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "AttendanceMode",
                table: "Events",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "CityAr",
                table: "Events",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CityEn",
                table: "Events",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndsAt",
                table: "Events",
                type: "datetimeoffset(0)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageId",
                table: "Events",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDescriptionAr",
                table: "Events",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MetaDescriptionEn",
                table: "Events",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaTitleAr",
                table: "Events",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MetaTitleEn",
                table: "Events",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnlineUrl",
                table: "Events",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RegistrationOpen",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: true);

            // Slug: added NULL, every existing row gets event-{Id} (unique by construction), then NOT NULL. The unique
            // index is created below, after the backfill.
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Events",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            // EXEC: compiled when it runs, so a generated script (one batch) can reference the column added just above.
            migrationBuilder.Sql("EXEC(N'UPDATE Events SET Slug = ''event-'' + CAST(Id AS varchar(12)) WHERE Slug IS NULL;');");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Events",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartsAt",
                table: "Events",
                type: "datetimeoffset(0)",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "Events",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "SummaryAr",
                table: "Events",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SummaryEn",
                table: "Events",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Events",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.CreateTable(
                name: "MediaFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileKey = table.Column<string>(type: "char(32)", unicode: false, fixedLength: true, maxLength: 32, nullable: false),
                    Format = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    Bytes = table.Column<int>(type: "int", nullable: false),
                    AltAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    AltEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartnerGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlugRedirects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    OldSlug = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlugRedirects", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Partners_LogoId",
                table: "Partners",
                column: "LogoId");

            migrationBuilder.CreateIndex(
                name: "IX_Partners_PartnerGroupId",
                table: "Partners",
                column: "PartnerGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Pages_ImageId",
                table: "Pages",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_News_ImageId",
                table: "News",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_News_Slug",
                table: "News",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_ImageId",
                table: "Events",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_IsActive_StartsAt",
                table: "Events",
                columns: new[] { "IsActive", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_Slug",
                table: "Events",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_FileKey",
                table: "MediaFiles",
                column: "FileKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartnerGroups_IsActive_SortOrder",
                table: "PartnerGroups",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SlugRedirects_EntityType_EntityId",
                table: "SlugRedirects",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SlugRedirects_EntityType_OldSlug",
                table: "SlugRedirects",
                columns: new[] { "EntityType", "OldSlug" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_MediaFiles_ImageId",
                table: "Events",
                column: "ImageId",
                principalTable: "MediaFiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_News_MediaFiles_ImageId",
                table: "News",
                column: "ImageId",
                principalTable: "MediaFiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Pages_MediaFiles_ImageId",
                table: "Pages",
                column: "ImageId",
                principalTable: "MediaFiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Partners_MediaFiles_LogoId",
                table: "Partners",
                column: "LogoId",
                principalTable: "MediaFiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Partners_PartnerGroups_PartnerGroupId",
                table: "Partners",
                column: "PartnerGroupId",
                principalTable: "PartnerGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Reference data for existing databases (DbSeeder never runs there): the default UTC offset of new events.
            migrationBuilder.Sql("INSERT INTO SiteContents (SectionKey, ContentAr, ContentEn, UpdatedAt) " +
                                 "SELECT N'default_utc_offset', N'+02:00', NULL, SYSUTCDATETIME() " +
                                 "WHERE NOT EXISTS (SELECT 1 FROM SiteContents WHERE SectionKey = N'default_utc_offset');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only the value Up inserted, and only while unchanged. (A founder_photo row written by the app stays: the
            // older code never reads it.) Rolling back production is a restore of the backup, not this Down.
            migrationBuilder.Sql("DELETE FROM SiteContents WHERE SectionKey = N'default_utc_offset' AND ContentAr = N'+02:00' AND ContentEn IS NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_MediaFiles_ImageId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_News_MediaFiles_ImageId",
                table: "News");

            migrationBuilder.DropForeignKey(
                name: "FK_Pages_MediaFiles_ImageId",
                table: "Pages");

            migrationBuilder.DropForeignKey(
                name: "FK_Partners_MediaFiles_LogoId",
                table: "Partners");

            migrationBuilder.DropForeignKey(
                name: "FK_Partners_PartnerGroups_PartnerGroupId",
                table: "Partners");

            migrationBuilder.DropTable(
                name: "MediaFiles");

            migrationBuilder.DropTable(
                name: "PartnerGroups");

            migrationBuilder.DropTable(
                name: "SlugRedirects");

            migrationBuilder.DropIndex(
                name: "IX_Partners_LogoId",
                table: "Partners");

            migrationBuilder.DropIndex(
                name: "IX_Partners_PartnerGroupId",
                table: "Partners");

            migrationBuilder.DropIndex(
                name: "IX_Pages_ImageId",
                table: "Pages");

            migrationBuilder.DropIndex(
                name: "IX_News_ImageId",
                table: "News");

            migrationBuilder.DropIndex(
                name: "IX_News_Slug",
                table: "News");

            migrationBuilder.DropIndex(
                name: "IX_Events_ImageId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_IsActive_StartsAt",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_Slug",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "LogoId",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "PartnerGroupId",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "ShowOnHome",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "ImageId",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "BodyAr",
                table: "News");

            migrationBuilder.DropColumn(
                name: "BodyEn",
                table: "News");

            migrationBuilder.DropColumn(
                name: "ImageId",
                table: "News");

            migrationBuilder.DropColumn(
                name: "MetaDescriptionAr",
                table: "News");

            migrationBuilder.DropColumn(
                name: "MetaDescriptionEn",
                table: "News");

            migrationBuilder.DropColumn(
                name: "MetaTitleAr",
                table: "News");

            migrationBuilder.DropColumn(
                name: "MetaTitleEn",
                table: "News");

            migrationBuilder.DropColumn(
                name: "PublishedOn",
                table: "News");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "News");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "News");

            migrationBuilder.DropColumn(
                name: "AddressAr",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "AddressEn",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "AttendanceMode",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CityAr",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CityEn",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "EndsAt",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ImageId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "MetaDescriptionAr",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "MetaDescriptionEn",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "MetaTitleAr",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "MetaTitleEn",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "OnlineUrl",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RegistrationOpen",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "StartsAt",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "SummaryAr",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "SummaryEn",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Events");
        }
    }
}
