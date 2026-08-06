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

### 微服务授权边界

Identity 负责维护用户、角色和权限关系，并签发身份凭证；每个业务服务负责验证凭证并对自己的 API 执行最终授权。Gateway 可以提前验证 token、限流和拒绝明显非法请求，但不能成为唯一授权边界，避免请求绕过 Gateway 后失去保护。

权限检查按层次划分：

- Gateway：认证、限流和路由级粗粒度策略，不承载最终业务授权。
- API：通过 `RequirePermission` 检查调用者是否具备执行该操作的能力。
- Application：检查数据归属、租户隔离、自操作限制等资源级授权。
- Domain：保证“不能删除最后一个超级管理员”等不依赖调用入口的业务不变量。
- 内部接口：使用服务身份、独立 audience/scope 或 mTLS，不复用普通管理员权限。

各业务服务不得在每次请求中同步查询 Identity，否则 Identity 会成为所有服务的延迟和可用性瓶颈。

### 第一阶段：权限写入 Access Token

当前权限数量较少，第一阶段在 Access Token 中写入重复的 `permission` claim。Role 不写入授权判断；Identity 在登录和刷新 token 时展开 Role，计算最终 Permission 集合。

```json
{
  "sub": "admin-user-id",
  "permission": [
    "identity.admin-users.read",
    "identity.admin-users.create",
    "notification.notifications.read"
  ],
  "auth_ver": 1
}
```

实现要求：

- `AccessTokenClaims` 必须支持多个同名 claim，不能使用只能保存一个同名键的字典表达 permissions。
- 密码登录、短信登录和 refresh 必须复用同一个 claims factory，避免不同登录方式签发出不同权限内容。
- 每个服务独立验证 token，并由 `PermissionAuthorizationHandler` 从 `permission` claim 完成授权。
- Access Token 建议保持 5～15 分钟有效期；Refresh Token 负责续期，并支持轮换和撤销。
- `/me` 继续返回 permissions，供前端控制路由、菜单和按钮，但前端结果不参与后端安全判断。

JWT 经过 Base64Url 编码后会比原始 JSON 更大。几十个权限通常可以直接携带，但随着权限数量和权限 code 长度增长，可能触发 Gateway、Ingress 或 Web Server 的请求头限制。项目将 4 KB 作为 Access Token 的目标体积预算，并通过自动化测试记录序列化后的 token 字节数；实际硬限制仍以部署环境配置为准。

不要为了压缩 token 而改为仅携带 Role。Role 是管理侧的权限集合，不是跨服务稳定的授权契约；让业务服务解释 Identity 的 Role 会造成反向耦合。

### 第二阶段：固定尺寸 Token 与权限快照

当单个用户可能拥有接近 100 个权限、Access Token 接近体积预算，或权限变更需要更快生效时，token 改为只携带稳定身份和授权版本：

```json
{
  "sub": "admin-user-id",
  "tenant": "tenant-id",
  "auth_ver": 12
}
```

授权处理器按以下顺序读取权限：

```text
Access Token (sub + auth_ver)
            |
            v
服务本地内存缓存
            |
            v  miss
分布式权限快照
            |
            v  miss
Identity/Auth 查询并回填缓存
```

建议的缓存结构：

```text
authz:user:{userId}:current-version       -> 12
authz:user:{userId}:v:{authVersion}       -> [permission codes]
```

角色、权限、密码或账号状态变化时，Identity 必须：

1. 在同一个业务操作中递增用户的 `auth_ver`。
2. 更新分布式权限快照并删除旧版本快照。
3. 发布 `UserPermissionsChanged` 或 `UserAuthorizationChanged` 集成事件。
4. 各服务收到事件后清除对应的本地缓存。

授权处理器必须比较 token 中的 `auth_ver` 与当前版本。缓存未命中且无法确认当前版本时应 fail closed，返回无权限或认证失败，不能为了可用性默认放行。

本地缓存会在可用性和立即失效之间产生权衡：普通权限变更可以使用短 TTL 加事件失效；账号停用、密码泄漏等需要立即撤销的场景，应查询分布式当前版本或维护紧急 denylist。

### 第三阶段：面向服务的凭证

当服务数量、权限数量或外部调用方明显增加后，再根据需要引入更复杂的凭证模型：

- Token Exchange：为目标 audience 签发短期 token，只包含目标服务所需的 scopes/permissions。
- Opaque Token + Introspection：token 只保存随机引用，由授权服务返回身份和权限；服务端需要缓存 introspection 结果。
- 服务身份凭证：内部调用使用 client credentials、独立 audience/scope 或 mTLS，与管理员 Access Token 分离。

无论权限来源如何演进，Controller/Endpoint 上的 `RequirePermission` 契约保持不变，只替换 `PermissionAuthorizationHandler` 获取权限集合的实现。

### 演进路线

| 阶段 | 适用条件 | Token 内容 | 权限来源 | 主要代价 |
| --- | --- | --- | --- | --- |
| 第一阶段 | 当前权限较少、服务较少 | `sub + permission[] + auth_ver` | token claim | 权限变更在旧 token 过期前可能延迟生效 |
| 第二阶段 | 权限接近 100 个或 token 接近 4 KB | `sub + tenant + auth_ver` | 本地缓存 + 分布式权限快照 | 增加缓存一致性、版本校验和失效事件 |
| 第三阶段 | 服务和外部调用方明显增多 | 面向 audience 的短期 token 或 opaque token | Token Exchange / Introspection | 增加授权基础设施和运行依赖 |

第一阶段实施时就保留 `auth_ver`，使后续切换到权限快照不需要改变 token 主体身份模型。不要仅按权限条数机械切换阶段，应同时观察序列化 token 大小、权限变更时效和授权服务可用性要求。

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
3. 重构 `AccessTokenClaims` 并增加统一 claims factory，使密码登录、短信登录和 refresh 都签发 `permission` 与 `auth_ver`。
4. 在 Auth BuildingBlock 增加 `RequirePermission`、动态 policy provider、requirement 和 authorization handler。
5. 为 AdminUsers、Roles、Permissions、Notifications 等现有 API 补齐权限常量和权限要求。
6. 为内部 API 增加独立的服务身份认证和 audience/scope，不允许匿名调用，也不复用管理员权限。
7. 将 Access Token 调整为短期 token，补充 Refresh Token 轮换、撤销和账号状态变更后的失效机制。
8. 增加 401/403、跨服务授权、token 体积预算和权限变更时效测试。
9. 前端移除“未返回 permissions 默认放行”的兼容策略。
10. 后续再做角色列表、角色编辑和权限分配页面；达到第二阶段条件后引入权限快照和失效事件。
