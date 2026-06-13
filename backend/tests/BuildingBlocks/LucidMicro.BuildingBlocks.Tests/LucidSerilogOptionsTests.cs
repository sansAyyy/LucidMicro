using LucidMicro.BuildingBlocks.Logging.SerilogIntegration.Options;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class LucidSerilogOptionsTests
{
    [Fact]
    public void Validate_DoesNotRequireFilePath_WhenFileSinkIsDisabled()
    {
        var options = new LucidSerilogOptions
        {
            ApplicationName = "TestService",
            File =
            {
                Enabled = false,
                Path = string.Empty
            }
        };

        options.Validate();
    }

    [Fact]
    public void Validate_Throws_WhenFileSinkIsEnabledAndRollingIntervalIsInvalid()
    {
        var options = new LucidSerilogOptions
        {
            ApplicationName = "TestService",
            File =
            {
                Enabled = true,
                Path = "logs/test-.log",
                RollingInterval = "Sometimes"
            }
        };

        Assert.Throws<ArgumentException>(options.Validate);
    }

    [Fact]
    public void Validate_Throws_WhenFileSinkIsEnabledAndPathIsMissing()
    {
        var options = new LucidSerilogOptions
        {
            ApplicationName = "TestService",
            File =
            {
                Enabled = true,
                Path = string.Empty
            }
        };

        Assert.Throws<ArgumentException>(options.Validate);
    }

    [Fact]
    public void Validate_Throws_WhenOutputTemplateIsMissing()
    {
        var options = new LucidSerilogOptions
        {
            ApplicationName = "TestService",
            OutputTemplate = string.Empty
        };

        Assert.Throws<ArgumentException>(options.Validate);
    }

    [Fact]
    public void Validate_DoesNotRequireLokiUri_WhenLokiSinkIsDisabled()
    {
        var options = new LucidSerilogOptions
        {
            ApplicationName = "TestService",
            Loki =
            {
                Enabled = false,
                Uri = string.Empty
            }
        };

        options.Validate();
    }

    [Fact]
    public void Validate_Throws_WhenLokiSinkIsEnabledAndUriIsInvalid()
    {
        var options = new LucidSerilogOptions
        {
            ApplicationName = "TestService",
            Loki =
            {
                Enabled = true,
                Uri = "localhost:3100"
            }
        };

        Assert.Throws<ArgumentException>(options.Validate);
    }

    [Fact]
    public void Validate_Throws_WhenLokiSinkIsEnabledAndLabelNameIsInvalid()
    {
        var options = new LucidSerilogOptions
        {
            ApplicationName = "TestService",
            Loki =
            {
                Enabled = true,
                Uri = "http://localhost:3100",
                Labels =
                {
                    ["service-name"] = "identity"
                }
            }
        };

        Assert.Throws<ArgumentException>(options.Validate);
    }

    [Fact]
    public void Validate_Throws_WhenRequestLoggingMessageTemplateIsMissing()
    {
        var options = new LucidSerilogOptions
        {
            ApplicationName = "TestService",
            RequestLogging =
            {
                MessageTemplate = string.Empty
            }
        };

        Assert.Throws<ArgumentException>(options.Validate);
    }
}
