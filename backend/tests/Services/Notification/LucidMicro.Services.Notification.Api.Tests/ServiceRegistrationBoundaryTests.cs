namespace LucidMicro.Services.Notification.Api.Tests;

public sealed class ServiceRegistrationBoundaryTests
{
    private static readonly string[] ForbiddenApiProjectReferences =
    [
        "LucidMicro.BuildingBlocks.EventBus.RabbitMQ.csproj",
        "LucidMicro.BuildingBlocks.HealthChecks.RabbitMQ.csproj"
    ];

    private static readonly string[] ForbiddenProgramRegistrations =
    [
        "AddLucidRabbitMqEventBus",
        "AddLucidRabbitMqConsumer",
        "AddLucidRabbitMqHealthCheck",
        "NotificationSendRequestedIntegrationEvent",
        "NotificationSendRequestedIntegrationEventHandler"
    ];

    [Fact]
    public void ApiProject_DoesNotReferenceMessagingImplementationBuildingBlocks()
    {
        var projectFile = ReadRepositoryFile(
            "backend",
            "src",
            "Services",
            "Notification",
            "LucidMicro.Services.Notification.Api",
            "LucidMicro.Services.Notification.Api.csproj");

        foreach (var forbiddenReference in ForbiddenApiProjectReferences)
        {
            Assert.DoesNotContain(forbiddenReference, projectFile, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Program_DoesNotRegisterMessagingInfrastructure()
    {
        var program = ReadRepositoryFile(
            "backend",
            "src",
            "Services",
            "Notification",
            "LucidMicro.Services.Notification.Api",
            "Program.cs");

        foreach (var forbiddenRegistration in ForbiddenProgramRegistrations)
        {
            Assert.DoesNotContain(forbiddenRegistration, program, StringComparison.Ordinal);
        }
    }

    private static string ReadRepositoryFile(params string[] relativePathParts)
    {
        return File.ReadAllText(Path.Combine([FindRepositoryRoot(), .. relativePathParts]));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "backend", "src"))
                && Directory.Exists(Path.Combine(directory.FullName, "docs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be found.");
    }
}
