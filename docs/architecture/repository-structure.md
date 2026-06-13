# 仓库结构

本文档描述 LucidMicro 期望采用的仓库布局。

仓库应渐进式创建。主要边界需要保持稳定，但不要在没有实际代码前过早创建真实项目。

```text
lucid-micro/
  README.md
  global.json
  .editorconfig
  .gitignore

  backend/
    LucidMicro.slnx
    Directory.Build.props
    Directory.Packages.props
    NuGet.config

    src/
      Contracts/
        LucidMicro.Contracts.Notification/

      BuildingBlocks/
        Core/
          Application/
          Domain/
        Web/
          AspNetCore/
          Auth/
          Cors/
          OpenApi/
          RateLimiting/
        Communication/
          Http/
          Resilience/
          ServiceDiscovery/
        Messaging/
          EventBus/
          Outbox/
          Inbox/
          Serialization/
        Data/
          Caching/
          Persistence/
          DistributedLock/
        Operations/
          HealthChecks/
          Logging/
          Observability/

      Services/
        Identity/
        System/
        FileStorage/

      Gateway/
        LucidMicro.Gateway/
      AppHost/

    tests/
      LucidMicro.ArchitectureTests/
      LucidMicro.SharedIntegrationTests/

  frontend/
    package.json
    pnpm-workspace.yaml
    tsconfig.base.json

    apps/
      admin/
      mobile/                 # 当前为占位

    packages/
      api-client/             # 规划
      shared-types/           # 规划
      shared-ui/              # 规划
      shared-config/          # 规划

  deploy/
    caddy/
      Caddyfile

    docker/
    compose/
      app/
      infra/
      caddy/
    k8s/                      # 规划

  templates/
    service-template/         # 规划
    crud-module-template/
    vue-feature-template/     # 规划
    uni-page-template/        # 规划

  scripts/
    dev.ps1                  # 规划
    build.ps1                # 规划
    test.ps1                 # 规划
    new-service.ps1          # 规划
    new-crud.ps1

  docs/
    architecture/
      principles.md
      building-blocks.md
      service-structure.md
      repository-structure.md
    deployment/
    conventions/
    frontend/
    adr/
```

## 创建策略

先创建稳定的目录骨架：

- `backend/src/BuildingBlocks`
- `backend/src/Services`
- `backend/src/Contracts`
- `frontend/apps`
- `frontend/packages`
- `deploy/caddy`
- `deploy/compose`
- `templates`
- `scripts`
- `docs`

当前仓库已经落地后端解决方案、核心 BuildingBlock、Identity、Notification、Gateway、Admin、Compose、Caddy 和 CRUD 生成脚本。后续新增真实项目时仍按以下顺序推进：

1. 后端解决方案和共享构建文件。
2. 立即有价值的核心 BuildingBlock。
3. 按 [服务模板结构规则](service-structure.md) 创建第一个真实服务。
4. 管理端前端应用。
5. 移动端 uni-app 应用。
6. compose 本地开发环境。
7. 模板和生成器脚本。

## 空目录

Git 不跟踪空目录，因此规划中的目录可以使用 `.gitkeep` 占位。当目录中加入真实文件后，可以移除对应的 `.gitkeep`。
