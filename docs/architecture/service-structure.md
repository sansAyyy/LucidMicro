# 服务模板结构规则

本文档定义 LucidMicro 后端微服务的默认分层、目录结构、依赖方向和创建规则。

当前服务模板不采用 CQRS。Application 层按 Feature 组织少量 ApplicationService，Controller 直接调用应用服务完成用例编排。

## 服务项目结构

每个服务放在 `backend/src/Services/{ServiceName}` 下，并默认拆分为四个项目：

```text
backend/src/Services/{ServiceName}/
  LucidMicro.Services.{ServiceName}.Api/
  LucidMicro.Services.{ServiceName}.Application/
  LucidMicro.Services.{ServiceName}.Domain/
  LucidMicro.Services.{ServiceName}.Infrastructure/
```

四层职责：

```text
Api              HTTP 入口、Controller、认证授权入口、OpenAPI、异常响应映射
Application      用例编排、应用服务、请求响应模型、验证、应用层抽象
Domain           实体、枚举、业务规则、业务异常
Infrastructure   EF Core、仓储实现、外部服务、缓存、消息、文件存储等技术细节
```

推荐依赖方向：

```text
Api -> Application
Api -> Infrastructure
Application -> Domain
Infrastructure -> Application
Infrastructure -> Domain
```

Domain 不依赖其他服务层，也不依赖 EF Core、HTTP、缓存、消息等基础设施。

Application 不依赖 Infrastructure。

Infrastructure 负责实现 Application 定义的抽象，并接入具体技术栈。

Api 负责组合 Application 和 Infrastructure，并选择具体 BuildingBlock 实现。

服务层依赖矩阵：

```text
Source          Allowed service targets              Allowed shared targets
Domain          -                                    BuildingBlocks/Core/Domain
Application     same service Domain                  Contracts, BuildingBlocks .Abstractions / .Core
Infrastructure  same service Application, Domain     Contracts, BuildingBlocks concrete implementations
Api             same service Application, Infra      Contracts, BuildingBlocks Web / Operations
```

具体规则：

- 服务项目之间只允许引用同一个服务内的项目，不允许服务之间直接项目引用。
- `Domain` 保持最纯，只依赖领域基础类型，不引用 Contracts、Application、Infrastructure、Api 或具体基础设施。
- `Application` 定义用例和端口，可以依赖 Contracts 与 BuildingBlock 抽象/核心项目，不依赖 `.Redis`、`.EFCore`、`.RabbitMQ`、`.AspNetCore` 等具体实现。
- `Infrastructure` 负责实现 Application 端口，可以依赖具体 BuildingBlock provider。
- `Api` 只负责宿主和 HTTP 入口，可以依赖 Web/Operations 类 BuildingBlock，不直接依赖 Data、Messaging、Communication 的具体实现。
- 这些规则由服务层架构测试保护。新增服务项目或新增项目引用时，应先确认依赖方向能被矩阵解释。

## Api 项目结构

Api 层默认不放 `Contracts` 目录。请求/响应模型优先放在 Application 的 Feature 目录下。

只有当 HTTP 契约和 Application 契约开始明显分化时，才在 Api 层新增 `Contracts`。

常见触发条件：

- HTTP 入参和应用用例入参明显不同。
- 需要绑定文件上传、Header、Route、Query 的复杂组合。
- 需要对外 API 版本兼容，不能直接暴露 Application 模型。
- 管理端 API、开放平台 API、内部 API 需要不同响应形态。
- 需要隐藏 Application 内部字段或组合多个应用响应。

默认目录：

```text
LucidMicro.Services.{ServiceName}.Api/
  Controllers/
  DependencyInjection/
  Filters/
  Middleware/
  OpenApi/
  Program.cs
  appsettings.json
  appsettings.Development.json
  Properties/
    launchSettings.json
```

第一阶段可以只创建：

```text
LucidMicro.Services.{ServiceName}.Api/
  Controllers/
  Program.cs
  appsettings.json
  appsettings.Development.json
```

## Application 项目结构

Application 层不设置全局 `Contracts` 目录作为默认结构。

请求、响应、验证器和应用服务优先按 Feature 放在一起，避免形成 DTO 大桶。

默认结构：

```text
LucidMicro.Services.{ServiceName}.Application/
  Abstractions/
  DependencyInjection/
  Features/
    {FeatureName}/
      Abstractions/
        I{FeatureName}ApplicationService.cs
      Services/
        {FeatureName}ApplicationService.cs
      Dtos/
        Requests/
        Responses/
      Validators/
      Specifications/
      Errors/
      Models/
  Shared/
```

示例：

```text
LucidMicro.Services.Identity.Application/
  Features/
    Users/
      Abstractions/
        IUserApplicationService.cs
      Services/
        UserApplicationService.cs
      Dtos/
        Requests/
          CreateUserRequest.cs
          UpdateUserRequest.cs
          ChangeUserPasswordRequest.cs
        Responses/
          UserResponse.cs
          UserDetailResponse.cs
      Validators/
        CreateUserRequestValidator.cs
        UpdateUserRequestValidator.cs
      Specifications/
        UsersListSpecification.cs
        UserByEmailSpecification.cs
      Errors/
        UserErrors.cs
```

ApplicationService 默认按 Feature 组织，不按 CRUD 或单个用例机械拆分。

Feature 内部目录职责：

```text
Abstractions/   Feature 对外暴露的应用服务接口
Services/       Feature 的应用服务实现
Dtos/Requests/  用例输入模型
Dtos/Responses/ 用例输出模型
Validators/     请求验证器
Specifications/ 仓储查询条件、排序、Include、NoTracking 等查询表达
Errors/         Feature 内复用的应用错误工厂，例如 NotFound、Conflict、InvalidCredentials
Models/         Feature 内部使用的应用层模型
```

推荐：

```text
UserApplicationService
RoleApplicationService
PermissionApplicationService
```

不推荐：

```text
CreateUserService
UpdateUserService
DeleteUserService
```

当一个 Feature 内部明显分成多个子能力，或单个 ApplicationService 文件过大时，可以按能力拆分：

```text
Features/
  Users/
    Account/
      Abstractions/
        IUserAccountApplicationService.cs
      Services/
        UserAccountApplicationService.cs
      Dtos/
        Requests/
        Responses/
    Profile/
      Abstractions/
        IUserProfileApplicationService.cs
      Services/
        UserProfileApplicationService.cs
      Dtos/
        Requests/
        Responses/
    Security/
      Abstractions/
        IUserSecurityApplicationService.cs
      Services/
        UserSecurityApplicationService.cs
      Dtos/
        Requests/
        Responses/
```

`Shared/` 只放确实跨 Feature 复用的应用层模型，例如分页请求、分页响应或通用选择项模型。

## Domain 项目结构

Domain 层表达核心实体和业务约束，不放 EF Core 配置、HTTP 模型、缓存实现或消息实现。

默认结构：

```text
LucidMicro.Services.{ServiceName}.Domain/
  Entities/
  Enums/
  Exceptions/
  Constants/
```

第一阶段可以只创建实际需要的实体目录：

```text
LucidMicro.Services.{ServiceName}.Domain/
  Entities/
    User.cs
```

业务规则优先放在实体或 Application 用例编排中。只有当规则确实需要跨多个用例复用时，才提取为清晰命名的业务组件。

## Infrastructure 项目结构

Infrastructure 层负责技术实现和外部系统接入。

默认结构：

```text
LucidMicro.Services.{ServiceName}.Infrastructure/
  DependencyInjection/
  Persistence/
    {ServiceName}DbContext.cs
    Configurations/
    Migrations/
    SeedData/
  Repositories/
  ExternalServices/
  Messaging/
  Caching/
  Options/
```

`Persistence/Configurations` 放 EF Core 实体配置。

`Repositories/` 放服务内专用 Repository 实现。通用仓储能满足时，不必为每个实体创建空仓储。

业务持久化端口统一使用 Repository 命名，不按 DDD 聚合根决定是否可以创建 Repository。关系表或状态表如果需要封装一组持久化操作，例如整体替换角色权限、整体替换用户角色、批量标记处理状态，也可以创建专用 Repository；但不要为了套用通用 Repository 给纯关系表增加无业务意义的 `Id`。

专用 Repository 应按读写能力拆分接口。只读接口使用 `IReadOnlyXxxRepository`，读写接口使用 `IXxxRepository` 并继承对应只读接口，和 BuildingBlock 的 `IReadOnlyRepository<TEntity, TId>` / `IRepository<TEntity, TId>` 保持一致。只读实现默认使用 no-tracking，为后续数据库读写分离预留替换空间。

`ExternalServices/` 放第三方 HTTP/RPC/SDK 客户端适配。

`Messaging/` 放消息发布、订阅、集成事件处理适配。

`Caching/` 放服务内缓存策略或缓存适配。

`Options/` 放基础设施相关配置对象。

## 测试结构

服务测试放在 `backend/tests/Services/{ServiceName}` 下。

跨测试项目复用的测试工具放在 `backend/tests/Shared` 下。

推荐结构：

```text
backend/tests/Shared/
  LucidMicro.Tests.Shared/

backend/tests/Services/{ServiceName}/
  LucidMicro.Services.{ServiceName}.Domain.Tests/
  LucidMicro.Services.{ServiceName}.Application.Tests/
  LucidMicro.Services.{ServiceName}.Infrastructure.Tests/
  LucidMicro.Services.{ServiceName}.Api.Tests/
```

如果测试规模还很小，可以先放在 `backend/tests/LucidMicro.Services.{ServiceName}.Api.Tests/`，后续再按服务目录归档。

测试覆盖优先级：

1. Domain：业务规则和实体行为。
2. Application：用例编排、校验、事务边界。
3. Infrastructure：持久化配置、外部系统适配。
4. Api：路由、状态码、请求/响应契约、认证授权入口。

`LucidMicro.Tests.Shared` 只放跨测试项目复用的基础测试工具，例如：

- 可控 `TimeProvider`
- 测试用审计用户提供器
- SQLite in-memory DbContext scope
- 通用断言辅助方法

不要把某个服务的业务对象、业务断言或专属测试数据工厂放进 `Shared`。这些内容应留在对应服务的测试项目内，避免测试工具项目演变成隐式业务依赖。

EF Core 集成测试优先使用 SQLite in-memory。只有需要验证具体数据库方言行为、迁移 SQL 或 provider 特性时，才接入真实数据库。

## 创建策略

不要一次性创建所有空目录。

文档定义完整目标结构，真实项目按需创建。

服务模板第一阶段建议只创建：

```text
LucidMicro.Services.{ServiceName}.Api/
  Controllers/
  Program.cs
  appsettings.json
  appsettings.Development.json

LucidMicro.Services.{ServiceName}.Application/
  DependencyInjection/

LucidMicro.Services.{ServiceName}.Domain/

LucidMicro.Services.{ServiceName}.Infrastructure/
  DependencyInjection/
  Persistence/
    {ServiceName}DbContext.cs
```

当某个目录中出现真实代码需求时再创建对应目录。

## CRUD 模块生成

后端 CRUD 模块模板放在：

```text
templates/crud-module-template/
```

生成脚本放在：

```text
scripts/new-crud.ps1
```

示例：

```powershell
.\scripts\new-crud.ps1 `
  -ServiceName Identity `
  -FeatureName Roles `
  -EntityName Role `
  -Route api/roles `
  -TableName roles
```

生成前可以先使用 `-DryRun` 查看将创建或更新的文件：

```powershell
.\scripts\new-crud.ps1 `
  -ServiceName Identity `
  -FeatureName Roles `
  -EntityName Role `
  -DryRun
```

如果目标文件已存在，脚本默认停止。需要覆盖模板生成文件时显式使用 `-Force`。

脚本会生成 Api、Application、Domain、Infrastructure 的基础 CRUD 文件，并自动在 Application 项目的 `DependencyInjection/ServiceCollectionExtensions.cs` 中注册：

```csharp
services.AddScoped<I{FeatureName}ApplicationService, {FeatureName}ApplicationService>();
```

第一版模板只生成一个 `Name` 字段和 `IsActive` 状态字段。复杂字段、认证授权、数据权限和特殊业务规则应在生成后按模块需求补充。

## 命名规则

项目命名：

```text
LucidMicro.Services.{ServiceName}.Api
LucidMicro.Services.{ServiceName}.Application
LucidMicro.Services.{ServiceName}.Domain
LucidMicro.Services.{ServiceName}.Infrastructure
```

Controller 命名：

```text
UsersController
RolesController
SystemController
```

ApplicationService 命名：

```text
I{FeatureName}ApplicationService
{FeatureName}ApplicationService
```

请求/响应命名：

```text
CreateUserRequest
UpdateUserRequest
UserResponse
UserDetailResponse
```

DbContext 命名：

```text
{ServiceName}DbContext
```

依赖注入入口命名：

```text
Add{ServiceName}Application
Add{ServiceName}Infrastructure
```
