namespace LucidMicro.Services.Identity.Api.Tests;

public sealed class ServiceRegistrationBoundaryTests
{
    private static readonly string[] ForbiddenApiProjectReferences =
    [
        "LucidMicro.BuildingBlocks.Auth.AspNetCore.csproj",
        "LucidMicro.BuildingBlocks.EventBus.RabbitMQ.csproj",
        "LucidMicro.BuildingBlocks.HealthChecks.RabbitMQ.csproj",
        "LucidMicro.BuildingBlocks.Outbox.Core.csproj"
    ];

    private static readonly string[] ForbiddenProgramRegistrations =
    [
        "AddLucidAspNetCorePasswordHashing",
        "AddLucidCurrentUser",
        "AddLucidJwtAuthentication",
        "AddLucidRabbitMqEventBus",
        "AddLucidRabbitMqHealthCheck",
        "AddLucidOutboxPublisherHostedService"
    ];

    [Fact]
    public void ApiProject_DoesNotReferenceInfrastructureImplementationBuildingBlocks()
    {
        var projectFile = ReadRepositoryFile(
            "backend",
            "src",
            "Services",
            "Identity",
            "LucidMicro.Services.Identity.Api",
            "LucidMicro.Services.Identity.Api.csproj");

        foreach (var forbiddenReference in ForbiddenApiProjectReferences)
        {
            Assert.DoesNotContain(forbiddenReference, projectFile, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Program_DoesNotRegisterInfrastructureImplementations()
    {
        var program = ReadRepositoryFile(
            "backend",
            "src",
            "Services",
            "Identity",
            "LucidMicro.Services.Identity.Api",
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
