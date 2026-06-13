namespace LucidMicro.BuildingBlocks.ServiceDiscovery.Static.Options;

public sealed class LucidStaticServiceDiscoveryOptions
{
    public const string ConfigurationSectionName = "Lucid:ServiceDiscovery";

    public Dictionary<string, string[]> Services { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public void Validate()
    {
        foreach (var (serviceName, endpoints) in Services)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                throw new InvalidOperationException("Static service discovery service name cannot be empty.");
            }

            if (endpoints.Length == 0)
            {
                throw new InvalidOperationException($"Static service discovery service '{serviceName}' must have at least one endpoint.");
            }

            foreach (var endpoint in endpoints)
            {
                if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    throw new InvalidOperationException(
                        $"Static service discovery endpoint '{endpoint}' for service '{serviceName}' must be an absolute HTTP or HTTPS URI.");
                }
            }
        }
    }
}
