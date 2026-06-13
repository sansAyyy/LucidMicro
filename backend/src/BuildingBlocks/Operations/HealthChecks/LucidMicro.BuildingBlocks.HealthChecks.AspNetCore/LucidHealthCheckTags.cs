namespace LucidMicro.BuildingBlocks.HealthChecks.AspNetCore;

public static class LucidHealthCheckTags
{
    public const string Ready = "ready";

    public const string Database = "database";

    public const string PostgreSql = "postgresql";

    public const string Cache = "cache";

    public const string Redis = "redis";

    public const string Messaging = "messaging";

    public const string RabbitMq = "rabbitmq";

    public const string ServiceDiscovery = "service-discovery";

    public const string Consul = "consul";
}
