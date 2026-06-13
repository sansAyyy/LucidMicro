using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LucidMicro.Services.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    group_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    group_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    resource_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    resource_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp(3) with time zone", precision: 3, nullable: false),
                    created_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    last_modified_at = table.Column<DateTimeOffset>(type: "timestamp(3) with time zone", precision: 3, nullable: true),
                    last_modified_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp(3) with time zone", precision: 3, nullable: false),
                    created_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    last_modified_at = table.Column<DateTimeOffset>(type: "timestamp(3) with time zone", precision: 3, nullable: true),
                    last_modified_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "admin_user_roles",
                columns: table => new
                {
                    admin_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_user_roles", x => new { x.admin_user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_admin_user_roles_admin_users_admin_user_id",
                        column: x => x.admin_user_id,
                        principalTable: "admin_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_admin_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_admin_user_roles_role_id",
                table: "admin_user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_permissions_code",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_permissions_group_resource_action",
                table: "permissions",
                columns: new[] { "group_code", "resource_code", "action" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_code",
                table: "roles",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

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
                VALUES
                    ('91531114-d670-4f73-9a21-34388aee6dcc', 'identity.admin-users.read', '查看管理员', NULL, 'identity', '身份认证', 'admin-users', '管理员', 'read', 1010, TRUE, TIMESTAMPTZ '2026-06-02 00:00:00+00', 'migration', NULL, NULL),
                    ('130f0701-46a2-45f2-b00c-28467e695aa5', 'identity.admin-users.create', '创建管理员', NULL, 'identity', '身份认证', 'admin-users', '管理员', 'create', 1020, TRUE, TIMESTAMPTZ '2026-06-02 00:00:00+00', 'migration', NULL, NULL),
                    ('23897c19-6833-410d-b120-66c605251262', 'identity.admin-users.update', '更新管理员', NULL, 'identity', '身份认证', 'admin-users', '管理员', 'update', 1030, TRUE, TIMESTAMPTZ '2026-06-02 00:00:00+00', 'migration', NULL, NULL),
                    ('04bdd7cd-6f46-4f30-a8dd-8a6715c6b5d6', 'identity.admin-users.enable', '启用管理员', NULL, 'identity', '身份认证', 'admin-users', '管理员', 'enable', 1040, TRUE, TIMESTAMPTZ '2026-06-02 00:00:00+00', 'migration', NULL, NULL),
                    ('c2228ad8-15fd-4852-b239-470586797fdd', 'identity.admin-users.disable', '禁用管理员', NULL, 'identity', '身份认证', 'admin-users', '管理员', 'disable', 1050, TRUE, TIMESTAMPTZ '2026-06-02 00:00:00+00', 'migration', NULL, NULL),
                    ('f6f1d7b0-4d94-45e1-aa8a-b03fc4c291ef', 'identity.admin-users.reset-password', '重置管理员密码', NULL, 'identity', '身份认证', 'admin-users', '管理员', 'reset-password', 1060, TRUE, TIMESTAMPTZ '2026-06-02 00:00:00+00', 'migration', NULL, NULL),
                    ('19cd2146-a694-4bed-b565-943e811f3ad9', 'identity.roles.read', '查看角色', NULL, 'identity', '身份认证', 'roles', '角色', 'read', 2010, TRUE, TIMESTAMPTZ '2026-06-02 00:00:00+00', 'migration', NULL, NULL),
                    ('c8acadc9-7ff9-465f-bc12-e63d44e920ad', 'identity.roles.manage', '管理角色', NULL, 'identity', '身份认证', 'roles', '角色', 'manage', 2020, TRUE, TIMESTAMPTZ '2026-06-02 00:00:00+00', 'migration', NULL, NULL),
                    ('388a8481-2b22-4bf5-b3a3-4076484df306', 'identity.roles.assign-permissions', '分配角色权限', NULL, 'identity', '身份认证', 'roles', '角色', 'assign-permissions', 2030, TRUE, TIMESTAMPTZ '2026-06-02 00:00:00+00', 'migration', NULL, NULL),
                    ('10d75485-fb96-4669-b006-f660399a4f74', 'notification.notifications.read', '查看通知', NULL, 'notification', '通知中心', 'notifications', '通知', 'read', 3010, TRUE, TIMESTAMPTZ '2026-06-02 00:00:00+00', 'migration', NULL, NULL),
                    ('f66852de-8c62-4edd-b6ac-d7edc9f7be68', 'notification.notifications.manage', '管理通知', NULL, 'notification', '通知中心', 'notifications', '通知', 'manage', 3020, TRUE, TIMESTAMPTZ '2026-06-02 00:00:00+00', 'migration', NULL, NULL),
                    ('758a9678-a3e5-4d6f-94db-9786608ab87e', 'admin.settings.read', '查看设置', NULL, 'admin', '系统管理', 'settings', '设置', 'read', 4010, TRUE, TIMESTAMPTZ '2026-06-02 00:00:00+00', 'migration', NULL, NULL)
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
                    last_modified_at = TIMESTAMPTZ '2026-06-02 00:00:00+00',
                    last_modified_by = 'migration';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO roles (
                    id,
                    code,
                    name,
                    description,
                    is_system,
                    is_enabled,
                    created_at,
                    created_by,
                    last_modified_at,
                    last_modified_by,
                    is_deleted
                )
                VALUES (
                    '48cebc20-4be4-4b44-98da-e6897dc441d8',
                    'super-admin',
                    'SuperAdmin',
                    '内置超级管理员角色，默认拥有全部内置权限。',
                    TRUE,
                    TRUE,
                    TIMESTAMPTZ '2026-06-02 00:00:00+00',
                    'migration',
                    NULL,
                    NULL,
                    FALSE
                )
                ON CONFLICT (code) WHERE is_deleted = FALSE DO UPDATE
                SET name = EXCLUDED.name,
                    description = EXCLUDED.description,
                    is_system = TRUE,
                    is_enabled = TRUE,
                    last_modified_at = TIMESTAMPTZ '2026-06-02 00:00:00+00',
                    last_modified_by = 'migration';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO role_permissions (role_id, permission_id)
                SELECT roles.id, permissions.id
                FROM roles
                CROSS JOIN permissions
                WHERE roles.code = 'super-admin'
                  AND roles.is_deleted = FALSE
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO admin_user_roles (admin_user_id, role_id)
                SELECT '9f6a1e15-809b-4caa-a1da-8e7250f68f22', roles.id
                FROM roles
                WHERE roles.code = 'super-admin'
                  AND roles.is_deleted = FALSE
                  AND EXISTS (
                      SELECT 1
                      FROM admin_users
                      WHERE id = '9f6a1e15-809b-4caa-a1da-8e7250f68f22'
                  )
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_user_roles");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
