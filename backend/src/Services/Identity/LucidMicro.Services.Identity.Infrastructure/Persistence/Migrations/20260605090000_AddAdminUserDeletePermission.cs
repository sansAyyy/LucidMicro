using System;
using LucidMicro.Services.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LucidMicro.Services.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20260605090000_AddAdminUserDeletePermission")]
    public partial class AddAdminUserDeletePermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO permissions (
                    id,
                    code,
                    name,
                    description,
                    group_code,
                    group_name,
                    resource_code,
                    resource_name,
                    action,
                    sort_order,
                    is_enabled,
                    created_at,
                    created_by,
                    last_modified_at,
                    last_modified_by
                )
                VALUES (
                    '0008ba2d-cc7a-40eb-9768-7df7ba91d27f',
                    'identity.admin-users.delete',
                    '删除管理员',
                    NULL,
                    'identity',
                    '身份认证',
                    'admin-users',
                    '管理员',
                    'delete',
                    1070,
                    TRUE,
                    TIMESTAMPTZ '2026-06-05 00:00:00+00',
                    'migration',
                    NULL,
                    NULL
                )
                ON CONFLICT (code) DO UPDATE
                SET name = EXCLUDED.name,
                    description = EXCLUDED.description,
                    group_code = EXCLUDED.group_code,
                    group_name = EXCLUDED.group_name,
                    resource_code = EXCLUDED.resource_code,
                    resource_name = EXCLUDED.resource_name,
                    action = EXCLUDED.action,
                    sort_order = EXCLUDED.sort_order,
                    is_enabled = EXCLUDED.is_enabled,
                    last_modified_at = TIMESTAMPTZ '2026-06-05 00:00:00+00',
                    last_modified_by = 'migration';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO role_permissions (role_id, permission_id)
                SELECT roles.id, permissions.id
                FROM roles
                JOIN permissions ON permissions.code = 'identity.admin-users.delete'
                WHERE roles.code = 'super-admin'
                  AND roles.is_deleted = FALSE
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM permissions
                WHERE code = 'identity.admin-users.delete';
                """);
        }
    }
}
