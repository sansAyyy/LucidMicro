using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LucidMicro.Services.Notification.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropProcessedIntegrationEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "processed_integration_events");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "processed_integration_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp(3) with time zone", precision: 3, nullable: false),
                    type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_integration_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_processed_integration_events_processed_at",
                table: "processed_integration_events",
                column: "processed_at");
        }
    }
}
