using System;
using LucidMicro.Services.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LucidMicro.Services.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20260528110000_AddAdminUserPhoneNumberUniqueIndex")]
    public partial class AddAdminUserPhoneNumberUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE admin_users
                SET phone_number = '13800138000'
                WHERE id = '9f6a1e15-809b-4caa-a1da-8e7250f68f22';
                """);

            migrationBuilder.CreateIndex(
                name: "ix_admin_users_phone_number",
                table: "admin_users",
                column: "phone_number",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_admin_users_phone_number",
                table: "admin_users");

            migrationBuilder.Sql(
                """
                UPDATE admin_users
                SET phone_number = NULL
                WHERE id = '9f6a1e15-809b-4caa-a1da-8e7250f68f22';
                """);
        }
    }
}
