# Admin 前端

Admin 是 LucidMicro 的 Vue 3 管理端应用，位于：

```text
frontend/apps/admin/
```

它只面向 Gateway，不直接依赖 Identity、Notification 等内部服务地址。

## 当前骨架

当前 Admin 使用 Vue 3、TypeScript、Vite、Vue Router、Pinia 和 Element Plus。

第一版保持轻量，不引入复杂后台模板。基础结构包括：

```text
src/
  app/
  layouts/
  pages/
  shared/
    api/
    auth/
    styles/
```

UI 约定：

- 使用 Element Plus 作为基础组件库。
- 不使用现成 admin template，路由、鉴权、请求封装和布局边界保持在项目内。
- Element Plus 组件通过 `unplugin-vue-components` 自动导入，并由 resolver 自动按需引入样式。
- Element Plus 函数式 API 通过 `unplugin-auto-import` 自动导入。
- Vue、Vue Router 和 Pinia API 保持显式导入，避免业务代码出现过多隐式全局。
- 图标组件保持显式导入，便于从文件顶部看出页面使用了哪些图标。

本地开发前复制环境变量：

```powershell
Copy-Item frontend/apps/admin/.env.example frontend/apps/admin/.env
```

常用命令：

```powershell
cd frontend
corepack pnpm install
corepack pnpm --filter @lucid-micro/admin dev
corepack pnpm --filter @lucid-micro/admin build
```

## 登录闭环

Admin 登录面向 Gateway 下的 Identity 路由：

```text
POST /api/identity/admin-auth/login
POST /api/identity/admin-auth/refresh
GET  /api/identity/admin-auth/me
```

当前前端行为：

- 登录成功后保存 access token、refresh token 和过期时间。
- 登录成功后立即请求 `/me` 获取当前管理员信息。
- 刷新页面后，路由守卫会用本地 token 恢复当前用户。
- `/me` 返回 401 时会尝试 refresh token，刷新失败才清理登录态并回到登录页。
- `VITE_API_BASE_URL` 应指向 Gateway，不指向 Identity 内部服务地址。
