namespace LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.Options;

public sealed class LucidConsulServiceDiscoveryOptions
{
    public const string ConfigurationSectionName = "Lucid:ServiceDiscovery:Consul";

    public string Address { get; set; } = "http://localhost:8500";

    public string Datacenter { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public bool OnlyPassing { get; set; } = true;

    public int CacheDurationSeconds { get; set; } = 10;

    public int RequestTimeoutSeconds { get; set; } = 5;

    public void Validate()
    {
        if (!Uri.TryCreate(Address, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Consul service discovery address must be an absolute HTTP or HTTPS URI.");
        }

        if (CacheDurationSeconds <= 0)
        {
            throw new InvalidOperationException("Consul service discovery cache duration seconds must be greater than zero.");
        }

        if (RequestTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("Consul service discovery request timeout seconds must be greater than zero.");
        }
    }
}
