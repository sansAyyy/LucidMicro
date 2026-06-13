using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LucidMicro.Services.Notification.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationFailureDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "last_error",
                table: "notification_messages",
                newName: "failure_reason");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "failed_at",
                table: "notification_messages",
                type: "timestamp(3) with time zone",
                precision: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "failed_at",
                table: "notification_messages");

            migrationBuilder.RenameColumn(
                name: "failure_reason",
                table: "notification_messages",
                newName: "last_error");
        }
    }
}
