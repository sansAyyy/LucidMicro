using SerilogRollingInterval = Serilog.RollingInterval;

namespace LucidMicro.BuildingBlocks.Logging.SerilogIntegration.Options;

public sealed class LucidSerilogOptions
{
    public const string ConfigurationSectionName = "Lucid:Logging:Serilog";

    public const string DefaultOutputTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {ApplicationName} {EnvironmentName} [{TraceId}/{SpanId}] {Message:lj}{NewLine}{Exception}";

    public const string DefaultRequestLoggingMessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

    public string ApplicationName { get; set; } = string.Empty;

    public string OutputTemplate { get; set; } = DefaultOutputTemplate;

    public LucidSerilogFileOptions File { get; set; } = new();

    public LucidSerilogLokiOptions Loki { get; set; } = new();

    public LucidSerilogRequestLoggingOptions RequestLogging { get; set; } = new();

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ApplicationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(OutputTemplate);
        Loki.Validate();
        RequestLogging.Validate();

        if (!File.Enabled)
        {
            return;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(File.Path);

        if (!Enum.TryParse<SerilogRollingInterval>(File.RollingInterval, ignoreCase: true, out _))
        {
            throw new ArgumentException("File.RollingInterval must be a valid Serilog rolling interval.");
        }

        if (File.RetainedFileCountLimit is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(File.RetainedFileCountLimit),
                "File.RetainedFileCountLimit must be greater than zero.");
        }
    }

    public sealed class LucidSerilogFileOptions
    {
        public bool Enabled { get; set; }

        public string Path { get; set; } = "logs/lucid-micro-.log";

        public string RollingInterval { get; set; } = nameof(SerilogRollingInterval.Day);

        public int? RetainedFileCountLimit { get; set; } = 31;
    }

    public sealed class LucidSerilogLokiOptions
    {
        public bool Enabled { get; set; }

        public string Uri { get; set; } = "http://localhost:3100";

        public Dictionary<string, string> Labels { get; set; } = [];

        public void Validate()
        {
            if (!Enabled)
            {
                return;
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(Uri);

            if (!System.Uri.TryCreate(Uri, UriKind.Absolute, out var uri)
                || (uri.Scheme != System.Uri.UriSchemeHttp && uri.Scheme != System.Uri.UriSchemeHttps))
            {
                throw new ArgumentException("Loki.Uri must be an absolute HTTP or HTTPS URI.");
            }

            foreach (var (key, value) in Labels)
            {
                if (!IsValidLabelName(key))
                {
                    throw new ArgumentException("Loki label names must match [a-zA-Z_][a-zA-Z0-9_]*.");
                }

                ArgumentException.ThrowIfNullOrWhiteSpace(value);
            }
        }

        private static bool IsValidLabelName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (!char.IsAsciiLetter(value[0]) && value[0] is not '_')
            {
                return false;
            }

            return value.All(static character => char.IsAsciiLetterOrDigit(character) || character is '_');
        }
    }

    public sealed class LucidSerilogRequestLoggingOptions
    {
        public string MessageTemplate { get; set; } = DefaultRequestLoggingMessageTemplate;

        public void Validate()
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(MessageTemplate);
        }
    }
}
