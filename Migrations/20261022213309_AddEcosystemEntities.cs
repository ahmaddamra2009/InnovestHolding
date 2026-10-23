using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovestHolding.Migrations
{
    /// <inheritdoc />
    public partial class AddEcosystemEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvestmentOpportunities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slug = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SummaryAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SummaryEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BodyAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BodyEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SectorItemId = table.Column<int>(type: "int", nullable: true),
                    StageTextAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StageTextEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AskTextAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AskTextEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImageId = table.Column<int>(type: "int", nullable: true),
                    IsOpen = table.Column<bool>(type: "bit", nullable: false),
                    MetaTitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MetaTitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetaDescriptionAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    MetaDescriptionEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentOpportunities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvestmentOpportunities_ContentItems_SectorItemId",
                        column: x => x.SectorItemId,
                        principalTable: "ContentItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvestmentOpportunities_MediaFiles_ImageId",
                        column: x => x.ImageId,
                        principalTable: "MediaFiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Speakers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slug = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    JobTitleAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    JobTitleEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    OrganizationAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    OrganizationEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    BioAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BioEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhotoId = table.Column<int>(type: "int", nullable: true),
                    LinkedInUrl = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    XUrl = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    MetaTitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MetaTitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetaDescriptionAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    MetaDescriptionEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Speakers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Speakers_MediaFiles_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "MediaFiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SponsorshipPackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Unit = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PriceTextAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PriceTextEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BenefitsAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    BenefitsEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsorshipPackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TvEpisodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slug = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Season = table.Column<int>(type: "int", nullable: true),
                    Number = table.Column<int>(type: "int", nullable: true),
                    TitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SummaryAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SummaryEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BodyAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BodyEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiredOn = table.Column<DateOnly>(type: "date", nullable: true),
                    VideoUrl = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    ThumbnailId = table.Column<int>(type: "int", nullable: true),
                    MetaTitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MetaTitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetaDescriptionAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    MetaDescriptionEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TvEpisodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TvEpisodes_MediaFiles_ThumbnailId",
                        column: x => x.ThumbnailId,
                        principalTable: "MediaFiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TvGuests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slug = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    JobTitleAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    JobTitleEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    OrganizationAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    OrganizationEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    BioAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BioEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhotoId = table.Column<int>(type: "int", nullable: true),
                    LinkedInUrl = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    XUrl = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    MetaTitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MetaTitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetaDescriptionAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    MetaDescriptionEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TvGuests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TvGuests_MediaFiles_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "MediaFiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EventSpeakers",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false),
                    SpeakerId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSpeakers", x => new { x.EventId, x.SpeakerId });
                    table.ForeignKey(
                        name: "FK_EventSpeakers_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventSpeakers_Speakers_SpeakerId",
                        column: x => x.SpeakerId,
                        principalTable: "Speakers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TvEpisodeGuests",
                columns: table => new
                {
                    TvEpisodeId = table.Column<int>(type: "int", nullable: false),
                    TvGuestId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TvEpisodeGuests", x => new { x.TvEpisodeId, x.TvGuestId });
                    table.ForeignKey(
                        name: "FK_TvEpisodeGuests_TvEpisodes_TvEpisodeId",
                        column: x => x.TvEpisodeId,
                        principalTable: "TvEpisodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TvEpisodeGuests_TvGuests_TvGuestId",
                        column: x => x.TvGuestId,
                        principalTable: "TvGuests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventSpeakers_SpeakerId",
                table: "EventSpeakers",
                column: "SpeakerId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentOpportunities_ImageId",
                table: "InvestmentOpportunities",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentOpportunities_IsActive_SortOrder",
                table: "InvestmentOpportunities",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentOpportunities_SectorItemId",
                table: "InvestmentOpportunities",
                column: "SectorItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentOpportunities_Slug",
                table: "InvestmentOpportunities",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Speakers_IsActive_SortOrder",
                table: "Speakers",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Speakers_PhotoId",
                table: "Speakers",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_Speakers_Slug",
                table: "Speakers",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipPackages_Unit_IsActive_SortOrder",
                table: "SponsorshipPackages",
                columns: new[] { "Unit", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TvEpisodeGuests_TvGuestId",
                table: "TvEpisodeGuests",
                column: "TvGuestId");

            migrationBuilder.CreateIndex(
                name: "IX_TvEpisodes_IsActive_AiredOn",
                table: "TvEpisodes",
                columns: new[] { "IsActive", "AiredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_TvEpisodes_Slug",
                table: "TvEpisodes",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TvEpisodes_ThumbnailId",
                table: "TvEpisodes",
                column: "ThumbnailId");

            migrationBuilder.CreateIndex(
                name: "IX_TvGuests_IsActive_SortOrder",
                table: "TvGuests",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TvGuests_PhotoId",
                table: "TvGuests",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_TvGuests_Slug",
                table: "TvGuests",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventSpeakers");

            migrationBuilder.DropTable(
                name: "InvestmentOpportunities");

            migrationBuilder.DropTable(
                name: "SponsorshipPackages");

            migrationBuilder.DropTable(
                name: "TvEpisodeGuests");

            migrationBuilder.DropTable(
                name: "Speakers");

            migrationBuilder.DropTable(
                name: "TvEpisodes");

            migrationBuilder.DropTable(
                name: "TvGuests");
        }
    }
}
