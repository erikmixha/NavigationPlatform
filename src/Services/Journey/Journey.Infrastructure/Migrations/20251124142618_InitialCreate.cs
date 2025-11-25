using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Journey.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Journeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StartLocation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArrivalLocation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ArrivalTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TransportType = table.Column<string>(type: "text", nullable: false),
                    DistanceKm = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsDailyGoalAchieved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Journeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyDistanceReadModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    TotalDistanceKm = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    JourneyCount = table.Column<int>(type: "integer", nullable: false),
                    LastUpdatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyDistanceReadModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShareAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JourneyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PerformedByUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TargetUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JourneyFavorites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JourneyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FavoritedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JourneyFavorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JourneyFavorites_Journeys_JourneyId",
                        column: x => x.JourneyId,
                        principalTable: "Journeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JourneyShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JourneyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedWithUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SharedByUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SharedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JourneyShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JourneyShares_Journeys_JourneyId",
                        column: x => x.JourneyId,
                        principalTable: "Journeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PublicLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JourneyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RevokedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicLinks_Journeys_JourneyId",
                        column: x => x.JourneyId,
                        principalTable: "Journeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JourneyFavorites_JourneyId",
                table: "JourneyFavorites",
                column: "JourneyId");

            migrationBuilder.CreateIndex(
                name: "IX_JourneyFavorites_JourneyId_UserId",
                table: "JourneyFavorites",
                columns: new[] { "JourneyId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JourneyFavorites_UserId",
                table: "JourneyFavorites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Journeys_StartTime",
                table: "Journeys",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_Journeys_UserId",
                table: "Journeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Journeys_UserId_StartTime",
                table: "Journeys",
                columns: new[] { "UserId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_JourneyShares_JourneyId",
                table: "JourneyShares",
                column: "JourneyId");

            migrationBuilder.CreateIndex(
                name: "IX_JourneyShares_JourneyId_SharedWithUserId",
                table: "JourneyShares",
                columns: new[] { "JourneyId", "SharedWithUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JourneyShares_SharedWithUserId",
                table: "JourneyShares",
                column: "SharedWithUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyDistanceReadModels_UserId_Year_Month",
                table: "MonthlyDistanceReadModels",
                columns: new[] { "UserId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyDistanceReadModels_Year_Month",
                table: "MonthlyDistanceReadModels",
                columns: new[] { "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_OccurredOnUtc",
                table: "OutboxMessages",
                column: "OccurredOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc",
                table: "OutboxMessages",
                column: "ProcessedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PublicLinks_IsRevoked_Token",
                table: "PublicLinks",
                columns: new[] { "IsRevoked", "Token" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicLinks_JourneyId",
                table: "PublicLinks",
                column: "JourneyId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicLinks_Token",
                table: "PublicLinks",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShareAudits_JourneyId",
                table: "ShareAudits",
                column: "JourneyId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareAudits_PerformedByUserId",
                table: "ShareAudits",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareAudits_Timestamp",
                table: "ShareAudits",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JourneyFavorites");

            migrationBuilder.DropTable(
                name: "JourneyShares");

            migrationBuilder.DropTable(
                name: "MonthlyDistanceReadModels");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "PublicLinks");

            migrationBuilder.DropTable(
                name: "ShareAudits");

            migrationBuilder.DropTable(
                name: "Journeys");
        }
    }
}
