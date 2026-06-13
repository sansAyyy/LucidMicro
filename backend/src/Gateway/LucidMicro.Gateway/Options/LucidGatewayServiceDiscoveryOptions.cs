namespace LucidMicro.Gateway.Options;

public sealed class LucidGatewayServiceDiscoveryOptions
{
    public const string ConfigurationSectionName = "Lucid:Gateway:ServiceDiscovery";

    public bool Enabled { get; set; }

    public int RefreshIntervalSeconds { get; set; } = 10;

    public string LoadBalancingPolicy { get; set; } = "RoundRobin";

    public Dictionary<string, string> Clusters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }

        if (RefreshIntervalSeconds <= 0)
        {
            throw new InvalidOperationException("Gateway service discovery refresh interval seconds must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(LoadBalancingPolicy))
        {
            throw new InvalidOperationException("Gateway service discovery load balancing policy is required.");
        }

        if (Clusters.Count == 0)
        {
            throw new InvalidOperationException("Gateway service discovery clusters are required.");
        }

        foreach (var (clusterId, serviceName) in Clusters)
        {
            if (string.IsNullOrWhiteSpace(clusterId) || string.IsNullOrWhiteSpace(serviceName))
            {
                throw new InvalidOperationException("Gateway service discovery cluster id and service name are required.");
            }
        }
    }
}
