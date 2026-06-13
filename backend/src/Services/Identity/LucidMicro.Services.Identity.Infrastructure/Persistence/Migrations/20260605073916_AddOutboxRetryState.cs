using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LucidMicro.Services.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxRetryState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_outbox_messages_pending",
                table: "outbox_messages");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "dead_at",
                table: "outbox_messages",
                type: "timestamp(3) with time zone",
                precision: 3,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "next_retry_at",
                table: "outbox_messages",
                type: "timestamp(3) with time zone",
                precision: 3,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_pending",
                table: "outbox_messages",
                column: "created_at",
                filter: "published_at is null and dead_at is null");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_outbox_messages_pending",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "dead_at",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "next_retry_at",
                table: "outbox_messages");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_pending",
                table: "outbox_messages",
                column: "created_at",
                filter: "published_at is null");
        }
    }
}
