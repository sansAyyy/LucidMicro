# 日志与可观测性约定

本文档记录当前后端日志、追踪和错误响应的约定。

## 错误响应 TraceId

所有由 Lucid 结果映射或全局异常处理生成的 `ProblemDetails` 都会包含 `traceId`：

```json
{
  "status": 500,
  "title": "An unexpected error occurred.",
  "code": "Server.Error",
  "errorType": "Failure",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

`traceId` 优先来自 `Activity.Current.TraceId`，没有当前 Activity 时回退到 `HttpContext.TraceIdentifier`。

## OpenTelemetry

当前 Observability BuildingBlock 支持 OpenTelemetry tracing 和第一版基础 metrics。日志由 Serilog BuildingBlock 承担。

OpenTelemetry 配置节为 `Lucid:Observability:OpenTelemetry`。

```json
{
  "Lucid": {
    "Observability": {
      "OpenTelemetry": {
        "ServiceName": "LucidMicro.Identity",
        "ServiceVersion": "1.0.0",
        "ServiceInstanceId": "",
        "OtlpEndpoint": "",
        "EnableConsoleExporter": false,
        "Metrics": {
          "Enabled": true,
          "EnableConsoleExporter": false
        }
      }
    }
  }
}
```

当前启用：

- ASP.NET Core tracing。
- HttpClient tracing。
- RabbitMQ 事件发布和消费 tracing。
- MQ trace context 传播，使用 `traceparent` 和 `tracestate` 对应的 `TraceParent`、`TraceState` envelope 字段。
- 可选 Console exporter。
- 可选 OTLP exporter。
- ASP.NET Core metrics。
- HttpClient metrics。
- .NET runtime metrics。
- Resource 中写入 `ServiceName`、`ServiceVersion` 和 `ServiceInstanceId`。

`ServiceInstanceId` 未配置时使用 `Environment.MachineName`。在 Docker Compose 多实例场景中，它通常对应容器 hostname，可用于在 metrics 中区分同一服务的不同节点。

ASP.NET Core tracing 默认不采集健康检查端点，避免 `/health`、`/healthz`、`/live` 和 `/ready` 产生高频噪音。HTTP 请求发生未处理异常时，异常会记录到当前 span。

当前暂不启用 EF Core instrumentation，因为 `OpenTelemetry.Instrumentation.EntityFrameworkCore` 当前仍是 beta 包。

Metrics 第一版只采集宿主和运行时基础指标，用于观察请求量、延迟、状态码分布、下游 HTTP 调用和 .NET runtime 状态。暂不在业务服务或 BuildingBlock 中主动定义自定义 `Meter`。

Docker Compose 中，应用容器访问 infra compose 里的 OpenTelemetry Collector：

```env
OTEL_OTLP_ENDPOINT=http://otel-collector:4317
```

Collector 会把 traces 转发到 Tempo，并把 metrics 暴露给 Prometheus 抓取。Metrics pipeline 会把 resource attributes 中的 `service.name`、`service.instance.id` 和 `service.version` 复制到 metric datapoint attributes；Prometheus exporter 会将它们规范化为 `service_name`、`service_instance_id` 和 `service_version` labels，供 Grafana 变量和面板筛选。

服务 traces 可按 `service.name` 查询：

```text
LucidMicro.Identity
LucidMicro.Notification
LucidMicro.Gateway
```

基础 metrics 可在 Prometheus 或 Grafana 中按 `service_name`、`service_instance_id`、`http_request_method`、`http_response_status_code` 等低基数字段筛选。不要把用户 id、租户 id、手机号、trace id 或原始业务对象 id 作为 metrics label。

## 本地联调验证

本段用于验证日志、traces 和 metrics 的基础链路是否打通。它不要求业务流程全部成功，只需要服务能启动并产生几次 HTTP 请求。

### 前置条件

- Docker 可用。
- 已复制 `deploy/compose/infra/.env.example` 到 `deploy/compose/infra/.env`。
- 已复制 `deploy/compose/app/.env.example` 到 `deploy/compose/app/.env`。
- app `.env` 中 `OTEL_OTLP_ENDPOINT` 指向 `http://otel-collector:4317`。
- 如果要验证 Loki 日志，app `.env` 中开启：

```env
LOKI_ENABLED=true
LOKI_URI=http://loki:3100
```

### 启动 infra

从仓库根目录执行：

```powershell
docker network inspect lucid-app *> $null; if ($LASTEXITCODE -ne 0) { docker network create lucid-app }

docker compose `
  --env-file deploy/compose/infra/.env `
  -f deploy/compose/infra/docker-compose.yml `
  up -d
```

检查 infra 容器：

```powershell
docker compose `
  --env-file deploy/compose/infra/.env `
  -f deploy/compose/infra/docker-compose.yml `
  ps
```

Prometheus targets 应能看到 `otel-collector` 为 `UP`：

```text
http://127.0.0.1:9090/targets
```

Grafana 默认地址：

```text
http://127.0.0.1:3000
```

默认账号密码来自 infra `.env`：

```env
GRAFANA_ADMIN_USER=admin
GRAFANA_ADMIN_PASSWORD=admin
```

Grafana 应自动出现：

```text
LucidMicro / LucidMicro Overview
```

### 启动 app 并触发请求

启动 app compose 前，应先按部署文档完成 Consul ACL bootstrap、app token 配置、数据库创建和迁移。

启动 app：

```powershell
docker compose `
  --env-file deploy/compose/app/.env `
  -f deploy/compose/app/docker-compose.yml `
  up -d --build
```

触发几次请求：

```powershell
Invoke-RestMethod http://localhost:49953/health

Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:49953/api/identity/admin-auth/login `
  -ContentType 'application/json' `
  -Body '{"loginName":"admin","password":"Admin@123456"}'

Invoke-RestMethod `
  http://localhost:49953/api/notification/notifications?pageNumber=1&pageSize=10
```

如果服务暂时没有完整依赖，健康检查或业务请求失败也可以用于验证部分 metrics 和 logs。Tracing 是否出现完整链路取决于请求是否进入实际服务管道。

### 验证 metrics

在 Grafana 打开 `LucidMicro Overview`，检查：

- `Service` 下拉能看到 `LucidMicro.Gateway`、`LucidMicro.Identity` 或 `LucidMicro.Notification`。
- `Instance` 下拉能看到对应容器实例。
- `Request Rate`、`Status Codes`、`P95 Latency` 有数据。
- 多实例部署时，`Request Rate By Instance` 和 `P95 Latency By Instance` 能看到不同 `service_instance_id`。

在 Prometheus 中也可以直接查询：

```text
{service_name="LucidMicro.Gateway"}
{service_name="LucidMicro.Identity"}
{service_name="LucidMicro.Notification"}
count by (service_name, service_instance_id) (http_server_request_duration_seconds_count)
```

如果没有数据，优先检查：

- app `.env` 中 `OTEL_OTLP_ENDPOINT` 是否为 `http://otel-collector:4317`。
- `otel-collector` 容器日志是否有 OTLP 接收或导出错误。
- Prometheus targets 中 `otel-collector` 是否为 `UP`。
- app 服务是否真的产生了非空请求。
- Prometheus 查询 `count by (service_name, service_instance_id) (...)` 是否能看到不同实例；如果 `service_name` 或 `service_instance_id` 为空，优先检查 Collector metrics pipeline 的 transform processor 配置是否生效。

### 验证 traces

在 Grafana Explore 中选择 `Tempo`，按服务名查询：

```text
LucidMicro.Gateway
LucidMicro.Identity
LucidMicro.Notification
```

也可以从 Loki 日志中的 `TraceId` 复制 trace id，直接在 Tempo 中查询。

注意：当前 ASP.NET Core tracing 默认不采集 `/health`、`/healthz`、`/live` 和 `/ready`，因此健康检查请求不会产生 trace。请使用登录、通知查询、短信发码等业务入口验证 traces。

### 验证 logs

在 Grafana Explore 中选择 `Loki`：

```text
{app="lucid_micro"}
```

按服务筛选：

```text
{app="lucid_micro", service="identity"}
{app="lucid_micro", service="notification"}
{app="lucid_micro", service="gateway"}
```

如果 Loki 没有数据，优先检查：

- app `.env` 是否设置 `LOKI_ENABLED=true`。
- app `.env` 中 `LOKI_URI` 是否为 `http://loki:3100`。
- 服务日志中是否有 Loki sink 初始化或发送失败信息。

## Serilog

Serilog 使用混合配置：

- 顶层 `Serilog` 使用官方配置模型，由 `Serilog.Settings.Configuration` 读取。
- `Lucid:Logging:Serilog` 放 Lucid 自定义配置。

标准 Serilog 配置：

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "Enrich": [ "FromLogContext" ]
  }
}
```

Lucid 自定义配置：

```json
{
  "Lucid": {
    "Logging": {
      "Serilog": {
        "ApplicationName": "LucidMicro.Identity",
        "OutputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {ApplicationName} {EnvironmentName} [{TraceId}/{SpanId}] {Message:lj}{NewLine}{Exception}",
        "File": {
          "Enabled": false,
          "Path": "logs/lucid-micro-identity-.log",
          "RollingInterval": "Day",
          "RetainedFileCountLimit": 31
        },
        "Loki": {
          "Enabled": false,
          "Uri": "http://localhost:3100",
          "Labels": {
            "app": "lucid_micro",
            "service": "identity"
          }
        },
        "RequestLogging": {
          "MessageTemplate": "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms"
        }
      }
    }
  }
}
```

当前 Serilog BuildingBlock 会额外写入：

- `ApplicationName`
- `EnvironmentName`
- `MachineName`
- `TraceId`
- `SpanId`

控制台输出模板：

```text
[{Timestamp:HH:mm:ss} {Level:u3}] {ApplicationName} {EnvironmentName} [{TraceId}/{SpanId}] {Message:lj}{NewLine}{Exception}
```

该模板来自 `Lucid:Logging:Serilog:OutputTemplate`，未配置时使用 BuildingBlock 内置默认值。

## File Sink

File sink 由 `Lucid:Logging:Serilog:File` 控制，默认关闭。

开启后会使用与 Console 相同的文本输出模板，按 `RollingInterval` 滚动文件，并由 `RetainedFileCountLimit` 控制保留文件数量。

`RollingInterval` 使用 Serilog 原生枚举值，例如 `Day`、`Hour`、`Month`。

## Loki Sink

Loki sink 由 `Lucid:Logging:Serilog:Loki` 控制，默认关闭。

启用示例：

```json
{
  "Lucid": {
    "Logging": {
      "Serilog": {
        "Loki": {
          "Enabled": true,
          "Uri": "http://localhost:3100",
          "Labels": {
            "app": "lucid_micro",
            "service": "identity"
          }
        }
      }
    }
  }
}
```

`Uri` 必须是绝对 HTTP 或 HTTPS 地址。`Labels` 只放低基数字段，例如：

- `app`
- `service`

Serilog BuildingBlock 会额外写入默认 Loki labels：

- `application`：来自 `ApplicationName`
- `environment`：来自 ASP.NET Core environment

不要把 `TraceId`、`SpanId`、`RequestPath`、用户 id、租户 id 或其他高基数字段配置为 Loki label。这些值应作为结构化日志属性保留，在 Grafana 中通过 JSON 字段过滤。

Docker Compose 中，应用容器访问 infra compose 里的 Loki 时使用：

```env
LOKI_ENABLED=true
LOKI_URI=http://loki:3100
```

## 请求日志

API 应用通过 `UseLucidSerilogRequestLogging()` 启用 Serilog request logging。

请求日志消息模板：

```text
HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms
```

该模板来自 `Lucid:Logging:Serilog:RequestLogging:MessageTemplate`，未配置时使用 BuildingBlock 内置默认值。

日志级别策略：

- 有异常或响应状态码 >= 500：`Error`。
- 响应状态码 >= 400：`Warning`。
- `/health`、`/healthz`、`/live`、`/ready` 成功请求：`Debug`。
- 其他请求：`Information`。

请求日志会额外写入：

- `TraceId`
- `RequestHost`
- `RequestScheme`

## Health Checks

API 应用通过 Health Checks BuildingBlock 暴露统一健康检查端点：

- `/health`：整体健康状态。
- `/live`：进程存活检查，不检查外部依赖。
- `/ready`：就绪检查，只运行带 `ready` tag 的依赖检查。

Health check tags 统一使用 `LucidHealthCheckTags`：

- `Ready`：参与 `/ready`。
- `Database`：数据库依赖分类。
- `PostgreSql`：PostgreSQL 依赖分类，也作为默认 PostgreSQL check name。
- `Cache`：缓存依赖分类。
- `Redis`：Redis 依赖分类，也作为默认 Redis check name。

响应格式：

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0012345",
  "entries": {}
}
```

健康检查响应不返回异常 message。失败细节应写入日志，HTTP 响应只暴露状态、描述、耗时和附加数据。

PostgreSQL 依赖检查通过 `LucidMicro.BuildingBlocks.HealthChecks.Npgsql` 注册：

```csharp
services.AddLucidNpgsqlDbContextHealthCheck<IdentityDbContext>();
```

Identity 服务当前注册了默认名为 `postgresql` 的 ready check，用于检查 Identity PostgreSQL 数据库是否可连接。

Redis 依赖检查通过 `LucidMicro.BuildingBlocks.HealthChecks.Redis` 注册：

```csharp
services.AddLucidRedisHealthCheck();
```

Redis health check 依赖容器中已有 `IConnectionMultiplexer`，默认名为 `redis`，并带有 `ready`、`cache`、`redis` tags。

API 合约测试不应依赖真实 PostgreSQL、Redis、MQ 等外部依赖；测试健康检查端点时可以替换或清空 `HealthCheckServiceOptions.Registrations`。

## 当前边界

当前暂不支持：

- `CorrelationId`。现阶段先使用 `TraceId` 串联 HTTP 请求、日志和 OpenTelemetry trace。
- 业务自定义 metrics。
- Prometheus `/metrics` 端点直出。应用只通过 OTLP 上报给 Collector。
- OpenTelemetry logs。日志由 Serilog BuildingBlock 负责。
- Seq sink。
- JSON console 输出。

后续接入 Gateway 时，优先传播 W3C Trace Context：`traceparent` 和 `tracestate`。

## MQ Trace Context

RabbitMQ 事件发布和消费使用 `LucidMicro.EventBus.RabbitMQ` ActivitySource。

发布事件时，当前 `Activity` 的 W3C trace context 会写入集成事件 envelope：

- `TraceParent`：来自 `Activity.Current.Id`。
- `TraceState`：来自 `Activity.Current.TraceStateString`。

RabbitMQ producer span 会写入：

- `messaging.system`：`rabbitmq`
- `messaging.operation`：`publish`
- `messaging.destination.name`
- `messaging.rabbitmq.routing_key`
- `messaging.message.type`

RabbitMQ consumer span 会写入：

- `messaging.system`：`rabbitmq`
- `messaging.operation`：`process`
- `messaging.message.type`
- `lucid.consumer.handler`

发布或消费失败时，对应 span 会标记为 `Error`，并记录轻量 exception event。

消费事件时，如果 envelope 中存在有效的 `TraceParent` 和 `TraceState`，Consumer Activity 会使用该 context 作为远程父级，从而把 HTTP 请求、Outbox 发布和 Notification 消费串联到同一条 trace。

如果 envelope 中没有有效 trace context，Consumer Activity 会作为新的 trace root 启动。
