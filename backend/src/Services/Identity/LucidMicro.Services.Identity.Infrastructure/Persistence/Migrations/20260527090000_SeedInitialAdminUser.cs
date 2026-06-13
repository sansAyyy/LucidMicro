using System;
using LucidMicro.Services.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LucidMicro.Services.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20260527090000_SeedInitialAdminUser")]
    public partial class SeedInitialAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO admin_users (
                    id,
                    user_name,
                    email,
                    display_name,
                    phone_number,
                    password_hash,
                    is_active,
                    last_login_at,
                    created_at,
                    created_by,
                    last_modified_at,
                    last_modified_by,
                    is_deleted
                )
                VALUES (
                    '9f6a1e15-809b-4caa-a1da-8e7250f68f22',
                    'admin',
                    'admin@lucidmicro.local',
                    'Administrator',
                    NULL,
                    'AQAAAAIAAYagAAAAENFI0a6NNmWtPDm/8DWksJzvqV/3mjn+OH0y4xnkAVtvwDTjEYf+uYsVJID2acDH4g==',
                    TRUE,
                    NULL,
                    TIMESTAMPTZ '2026-05-27 00:00:00+00',
                    'migration',
                    NULL,
                    NULL,
                    FALSE
                )
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM admin_users
                WHERE id = '9f6a1e15-809b-4caa-a1da-8e7250f68f22';
                """);
        }
    }
}
