# 权限模型设计

权限模型用于统一后端授权、前端菜单/按钮控制和后续角色管理。第一版目标是够清晰、够稳定、方便扩展，不把菜单树和权限数据模型绑死在一起。

## 设计结论

- Permission 是扁平能力点，不设计父子节点。
- Permission Code 是稳定契约，格式为 `{service}.{resource}.{action}`。
- Role 聚合 Permission，AdminUser 绑定 Role。
- 前端菜单分组只是一种展示结构，不等同于权限树。
- 按钮级控制使用 action 权限，不额外设计“按钮权限子节点”。
- 前端权限只负责体验控制，后端 API 授权才是安全边界。
- 后端 `/me` 或等价接口需要返回当前用户 permissions，前端据此控制路由和操作入口。

## 为什么不做父子权限树

父子节点看起来适合菜单，但放进授权模型后容易混淆：

- 父节点是否自动包含子节点会产生歧义。
- 菜单分组、页面访问、按钮操作是三种不同概念。
- 后续某个按钮可能不属于单一页面，树结构会变得别扭。
- 权限变更时父子继承会增加排查成本。

因此，权限本体保持扁平；如果 UI 需要树形展示，用 `group/resource/action` 元数据组装即可。若未来确实需要表达依赖关系，单独增加 `permission_dependencies`，不要用 `ParentId` 承载隐式继承。

## 权限编码

权限编码使用小写短横线，保持长期稳定：

```text
identity.admin-users.read
identity.admin-users.create
identity.admin-users.update
identity.admin-users.enable
identity.admin-users.disable
identity.admin-users.reset-password
identity.admin-users.delete

identity.roles.read
identity.roles.manage
identity.roles.assign-permissions

notification.notifications.read
notification.notifications.manage

admin.settings.read
```

命名规则：

- `service`：服务或领域边界，例如 `identity`、`notification`。
- `resource`：资源名，使用复数，例如 `admin-users`、`roles`。
- `action`：能力动作，例如 `read`、`create`、`update`、`disable`。

不要为纯前端交互创建权限，例如打开弹窗、展开行、关闭抽屉、切换 tab。只有读取敏感数据、修改数据、执行高风险操作时才定义权限。

## 第一版权限清单

| 分组 | 资源 | 动作 | Code |
| --- | --- | --- | --- |
| Identity | 管理员 | 查看 | `identity.admin-users.read` |
| Identity | 管理员 | 创建 | `identity.admin-users.create` |
| Identity | 管理员 | 更新 | `identity.admin-users.update` |
| Identity | 管理员 | 启用 | `identity.admin-users.enable` |
| Identity | 管理员 | 禁用 | `identity.admin-users.disable` |
| Identity | 管理员 | 重置密码 | `identity.admin-users.reset-password` |
| Identity | 管理员 | 删除 | `identity.admin-users.delete` |
| Identity | 角色 | 查看 | `identity.roles.read` |
| Identity | 角色 | 管理 | `identity.roles.manage` |
| Identity | 角色 | 分配权限 | `identity.roles.assign-permissions` |
| Notification | 通知 | 查看 | `notification.notifications.read` |
| Notification | 通知 | 管理 | `notification.notifications.manage` |
| Admin | 设置 | 查看 | `admin.settings.read` |

当前前端已有 `AdminPermissions` 常量，后续应与这份清单对齐，页面中不直接写权限字符串。

## 数据模型

建议第一版表结构：

### permissions

- `id`
- `code`
- `name`
- `description`
- `group_code`
- `group_name`
- `resource_code`
- `resource_name`
- `action`
- `sort_order`
- `is_enabled`
- `created_at`
- `last_modified_at`

`code` 是稳定唯一键，不能随展示文案变化而变化。

### roles

- `id`
- `code`
- `name`
- `description`
- `is_system`
- `is_enabled`
- `created_at`
- `last_modified_at`

系统角色不可删除，必要时限制修改范围。

### role_permissions

- `role_id`
- `permission_id`

联合主键：`role_id + permission_id`。

### admin_user_roles

- `admin_user_id`
- `role_id`

联合主键：`admin_user_id + role_id`。

## Repository 边界

权限模块的业务持久化端口统一使用 `Repository` 命名。项目不按 DDD 聚合根决定是否可以创建 Repository；只要 Application 层需要稳定表达某组业务持久化能力，就可以定义专用 Repository。

`Permission`、`Role` 和 `AdminUser` 当前使用 BuildingBlock 提供的通用 `IReadOnlyRepository<TEntity, TId>` / `IRepository<TEntity, TId>` 完成查询、分页、创建、更新和删除。

`RolePermission` 和 `AdminUserRole` 这类关系表不需要为了套用通用仓储而增加没有业务意义的 `Id`。它们通过业务专用 Repository 表达明确的持久化能力，例如：

- 查询某个角色已绑定的权限 id。
- 整体替换某个角色的权限集合。
- 查询某个管理员已绑定的角色 id。
- 整体替换某个管理员的角色集合。
- 查询某个管理员经由角色获得的权限 code。

专用 Repository 按读写能力拆分接口。只读接口使用 `IReadOnlyXxxRepository` 命名，读写接口使用 `IXxxRepository` 命名并继承对应只读接口，保持和 BuildingBlock 的 `IReadOnlyRepository` / `IRepository` 一致。

当前权限模块的专用 Repository：

```text
IReadOnlyAdminUserPermissionRepository

IReadOnlyAdminUserRoleRepository
IAdminUserRoleRepository

IReadOnlyRolePermissionRepository
IRolePermissionRepository
```

只读 Repository 查询默认使用 no-tracking；后续接入数据库读写分离时，只读接口可以替换为读库实现，读写接口继续走主库。需要注意读库复制延迟：写入后立即读取的事务内或强一致场景，应继续使用写库上下文或读写 Repository。

Application 层依赖这些窄端口，不直接操作 EF Core，也不把删除旧关系、去重、插入新关系等持久化细节散落在应用服务中。

### permission_dependencies

第一版不做。未来如果需要表达“拥有某操作必须同时拥有读取权限”，再增加：

- `permission_code`
- `required_permission_code`

依赖关系只做校验或授权辅助，不改变 Permission 扁平模型。

## 内置角色

第一版至少内置：

- `SuperAdmin`：拥有全部权限。

后续可按业务需要增加：

- `AdminUserViewer`：只读管理员。
- `AdminUserOperator`：管理员日常操作。
- `NotificationViewer`：只读通知。

初始管理员用户应绑定 `SuperAdmin`。迁移或种子数据必须幂等，重复执行不会产生重复角色、权限或绑定。

## 后端授权

后端授权应基于 Permission，而不是直接基于 Role。

推荐用法：

```csharp
[RequirePermission("identity.admin-users.read")]
public Task<IActionResult> GetAdminUsers(...)
```

原则：

- Controller/Endpoint 标注所需权限。
- 授权处理器从当前用户身份解析 permission codes。
- 按钮背后的 API 必须有同等后端权限保护。
- Role 只是权限集合，不直接写进业务授权判断。

权限来源可以分阶段演进：

- 第一版：登录后 `/me` 返回 permissions，前端用于体验控制；后端可从 token claim 或服务端查询中完成 API 授权。
- 后续：如果 permissions 放入 access token，角色变更后要考虑 token 刷新或版本失效。

## 前端权限

前端继续使用常量承载权限契约：

```ts
export const AdminPermissions = {
  AdminUsersRead: "identity.admin-users.read",
  AdminUsersCreate: "identity.admin-users.create"
} as const;
```

路由使用 `requiredPermissions` 控制页面访问：

```ts
meta: {
  requiresAuth: true,
  requiredPermissions: [AdminPermissions.AdminUsersRead]
}
```

按钮使用同一套权限常量控制显示或禁用：

```vue
<ActionButton v-if="auth.hasPermissions([AdminPermissions.AdminUsersCreate])">
  新建管理员
</ActionButton>
```

当前前端为了兼容后端尚未返回 permissions，存在“未返回 permissions 时默认放行”的过渡策略。后端权限功能完成后，需要移除该兼容逻辑，改为未返回权限即无权限。

## 菜单与权限的关系

左侧导航可以按模块展示：

- Identity
  - 管理员
  - 角色
- Notification
  - 通知

菜单结构来自路由元数据或前端配置，不来自权限父子树。菜单项是否显示由对应路由权限决定，例如“管理员”菜单需要 `identity.admin-users.read`。

## API 规划

后续角色/权限管理建议接口：

```text
GET /api/identity/permissions

GET /api/identity/roles
POST /api/identity/roles
GET /api/identity/roles/{id}
PUT /api/identity/roles/{id}
PUT /api/identity/roles/{id}/permissions

PUT /api/identity/admin-users/{id}/roles
```

`GET /api/identity/permissions` 返回权限展示元数据，前端可以按 `group_code/resource_code/action` 组装权限选择界面。

## 实施顺序

1. 在 Identity 服务增加 Permission、Role、RolePermission、AdminUserRole 模型与迁移。
2. 种子写入内置权限、`SuperAdmin` 角色和初始管理员绑定。
3. 登录态接口返回当前用户 permissions。
4. 增加后端 `RequirePermission` 授权能力。
5. 为 AdminUsers、Notifications 等现有 API 补齐权限要求。
6. 前端移除“未返回 permissions 默认放行”的兼容策略。
7. 后续再做角色列表、角色编辑和权限分配页面。
