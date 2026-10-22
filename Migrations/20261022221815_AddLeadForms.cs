using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovestHolding.Migrations
{
    /// <summary>
    /// The lead forms: Registrations gains Kind (existing rows: quick), the chosen event (FK SET NULL + title snapshot),
    /// organisation, job title, country and ConsentAt; three lead tables (NetworkApplications, SponsorshipEnquiries,
    /// OpportunityInterests); on all four the admin's workflow Status (int, 0 = New for every existing row) and AdminNote;
    /// read-state indexes for the inbox counts, filtered to unread rows (Kind included where the counts split by kind).
    /// SponsorshipEnquiries.PackageId → SponsorshipPackages and OpportunityInterests.OpportunityId →
    /// InvestmentOpportunities are foreign keys ON DELETE SET NULL (the title snapshots stay). Data steps in
    /// AddLeadForms.Data.cs.
    /// </summary>
    public partial class AddLeadForms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "Registrations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentAt",
                table: "Registrations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Registrations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "Registrations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventTitle",
                table: "Registrations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "Registrations",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "Registrations",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "quick");

            migrationBuilder.AddColumn<string>(
                name: "Organization",
                table: "Registrations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Registrations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "NetworkApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Organization = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InvestorType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TicketSize = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Sectors = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Stage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FundingSought = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LinkedInUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Pitch = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ConsentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Culture = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AdminNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpportunityInterests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OpportunityId = table.Column<int>(type: "int", nullable: true),
                    OpportunityTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ConsentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Culture = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AdminNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpportunityInterests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpportunityInterests_InvestmentOpportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalTable: "InvestmentOpportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SponsorshipEnquiries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Unit = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    PackageId = table.Column<int>(type: "int", nullable: true),
                    PackageTitle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ConsentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Culture = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AdminNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsorshipEnquiries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SponsorshipEnquiries_SponsorshipPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "SponsorshipPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_EventId",
                table: "Registrations",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_IsRead",
                table: "Registrations",
                column: "IsRead",
                filter: "[IsRead] = 0")
                .Annotation("SqlServer:Include", new[] { "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessages_IsRead",
                table: "ContactMessages",
                column: "IsRead",
                filter: "[IsRead] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkApplications_CreatedAt",
                table: "NetworkApplications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkApplications_IsRead",
                table: "NetworkApplications",
                column: "IsRead",
                filter: "[IsRead] = 0")
                .Annotation("SqlServer:Include", new[] { "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_NetworkApplications_Kind_CreatedAt",
                table: "NetworkApplications",
                columns: new[] { "Kind", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityInterests_CreatedAt",
                table: "OpportunityInterests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityInterests_IsRead",
                table: "OpportunityInterests",
                column: "IsRead",
                filter: "[IsRead] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityInterests_OpportunityId",
                table: "OpportunityInterests",
                column: "OpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipEnquiries_CreatedAt",
                table: "SponsorshipEnquiries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipEnquiries_IsRead",
                table: "SponsorshipEnquiries",
                column: "IsRead",
                filter: "[IsRead] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipEnquiries_PackageId",
                table: "SponsorshipEnquiries",
                column: "PackageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Registrations_Events_EventId",
                table: "Registrations",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            UpData(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DownData(migrationBuilder);

            migrationBuilder.DropForeignKey(
                name: "FK_Registrations_Events_EventId",
                table: "Registrations");

            migrationBuilder.DropTable(
                name: "NetworkApplications");

            migrationBuilder.DropTable(
                name: "OpportunityInterests");

            migrationBuilder.DropTable(
                name: "SponsorshipEnquiries");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_EventId",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_IsRead",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_ContactMessages_IsRead",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "ConsentAt",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "EventTitle",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "Organization",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Registrations");
        }
    }
}
