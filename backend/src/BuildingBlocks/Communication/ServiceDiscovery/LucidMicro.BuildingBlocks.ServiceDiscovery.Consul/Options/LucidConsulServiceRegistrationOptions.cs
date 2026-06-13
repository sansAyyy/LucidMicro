namespace LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.Options;

public sealed class LucidConsulServiceRegistrationOptions
{
    public const string ConfigurationSectionName = "Lucid:ServiceDiscovery:Consul:Registration";

    public string ServiceName { get; set; } = string.Empty;

    public string ServiceId { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public bool UseInstanceDefaults { get; set; }

    public int Port { get; set; }

    public string Scheme { get; set; } = Uri.UriSchemeHttp;

    public string HealthCheckPath { get; set; } = "/ready";

    public int HealthCheckIntervalSeconds { get; set; } = 10;

    public int DeregisterCriticalServiceAfterSeconds { get; set; } = 60;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ServiceName))
        {
            throw new InvalidOperationException("Consul service registration service name is required.");
        }

        if (!UseInstanceDefaults && string.IsNullOrWhiteSpace(ServiceId))
        {
            throw new InvalidOperationException("Consul service registration service id is required.");
        }

        if (!UseInstanceDefaults && string.IsNullOrWhiteSpace(Address))
        {
            throw new InvalidOperationException("Consul service registration address is required.");
        }

        if (Port is <= 0 or > 65535)
        {
            throw new InvalidOperationException("Consul service registration port must be between 1 and 65535.");
        }

        if (Scheme != Uri.UriSchemeHttp && Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Consul service registration scheme must be HTTP or HTTPS.");
        }

        if (string.IsNullOrWhiteSpace(HealthCheckPath) || !HealthCheckPath.StartsWith('/'))
        {
            throw new InvalidOperationException("Consul service registration health check path must start with '/'.");
        }

        if (HealthCheckIntervalSeconds <= 0)
        {
            throw new InvalidOperationException("Consul service registration health check interval seconds must be greater than zero.");
        }

        if (DeregisterCriticalServiceAfterSeconds <= 0)
        {
            throw new InvalidOperationException("Consul service registration deregister critical service after seconds must be greater than zero.");
        }
    }
}
