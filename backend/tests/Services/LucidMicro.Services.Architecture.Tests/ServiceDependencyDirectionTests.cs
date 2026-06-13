using System.Xml.Linq;

namespace LucidMicro.Services.Architecture.Tests;

public sealed class ServiceDependencyDirectionTests
{
    private static readonly HashSet<string> KnownLayers = new(StringComparer.Ordinal)
    {
        "Api",
        "Application",
        "Domain",
        "Infrastructure"
    };

    [Fact]
    public void ServiceProjects_UseKnownLayers()
    {
        var projects = LoadServiceProjects();
        var violations = projects
            .Where(project => !KnownLayers.Contains(project.Layer))
            .Select(project => project.RelativePath)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ServiceProjectReferences_DoNotCrossServiceBoundaries()
    {
        var projects = LoadServiceProjects();
        var projectsByPath = projects.ToDictionary(
            project => project.FullPath,
            StringComparer.OrdinalIgnoreCase);
        var violations = new List<string>();

        foreach (var source in projects)
        {
            foreach (var targetPath in source.ProjectReferencePaths)
            {
                if (!projectsByPath.TryGetValue(targetPath, out var target))
                {
                    continue;
                }

                if (!string.Equals(source.ServiceName, target.ServiceName, StringComparison.Ordinal))
                {
                    violations.Add($"{source.Name} -> {target.Name}");
                }
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void ServiceProjectReferences_FollowLayerRules()
    {
        var backendRoot = FindBackendRoot();
        var projects = LoadServiceProjects();
        var violations = new List<string>();

        foreach (var source in projects)
        {
            foreach (var targetPath in source.ProjectReferencePaths)
            {
                var target = ClassifyTarget(backendRoot, source, targetPath);

                if (!IsAllowedReference(source, target))
                {
                    violations.Add($"{source.Name} ({source.Layer}) -> {target.DisplayName} ({target.Kind})");
                }
            }
        }

        Assert.Empty(violations);
    }

    private static bool IsAllowedReference(ServiceProject source, ReferenceTarget target)
    {
        return source.Layer switch
        {
            "Domain" => target.Kind == ReferenceKind.BuildingBlockCoreDomain,
            "Application" => target.Kind is
                ReferenceKind.ServiceDomain or
                ReferenceKind.Contracts or
                ReferenceKind.BuildingBlockAbstractionsOrCore or
                ReferenceKind.BuildingBlockCoreDomain,
            "Infrastructure" => target.Kind is
                ReferenceKind.ServiceApplication or
                ReferenceKind.ServiceDomain or
                ReferenceKind.Contracts or
                ReferenceKind.BuildingBlock or
                ReferenceKind.BuildingBlockAbstractionsOrCore or
                ReferenceKind.BuildingBlockCoreDomain or
                ReferenceKind.BuildingBlockWebOrOperations,
            "Api" => target.Kind is
                ReferenceKind.ServiceApplication or
                ReferenceKind.ServiceInfrastructure or
                ReferenceKind.Contracts or
                ReferenceKind.BuildingBlockWebOrOperations,
            _ => false
        };
    }

    private static ReferenceTarget ClassifyTarget(string backendRoot, ServiceProject source, string targetPath)
    {
        var srcRoot = Path.Combine(backendRoot, "src");
        var relativePath = Path.GetRelativePath(srcRoot, targetPath);
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var displayName = Path.GetFileNameWithoutExtension(targetPath);

        if (segments is ["Services", _, ..])
        {
            var targetService = segments[1];
            var targetLayer = GetLayer(displayName);

            if (!string.Equals(source.ServiceName, targetService, StringComparison.Ordinal))
            {
                return new ReferenceTarget(ReferenceKind.Forbidden, displayName);
            }

            return targetLayer switch
            {
                "Application" => new ReferenceTarget(ReferenceKind.ServiceApplication, displayName),
                "Domain" => new ReferenceTarget(ReferenceKind.ServiceDomain, displayName),
                "Infrastructure" => new ReferenceTarget(ReferenceKind.ServiceInfrastructure, displayName),
                _ => new ReferenceTarget(ReferenceKind.Forbidden, displayName)
            };
        }

        if (segments is ["Contracts", ..])
        {
            return new ReferenceTarget(ReferenceKind.Contracts, displayName);
        }

        if (segments is ["BuildingBlocks", ..])
        {
            var domain = segments[1];
            var capability = segments[2];

            if (domain == "Core" && capability == "Domain")
            {
                return new ReferenceTarget(ReferenceKind.BuildingBlockCoreDomain, displayName);
            }

            if (displayName.EndsWith(".Abstractions", StringComparison.Ordinal))
            {
                return new ReferenceTarget(ReferenceKind.BuildingBlockAbstractionsOrCore, displayName);
            }

            if (domain is "Web" or "Operations")
            {
                return new ReferenceTarget(ReferenceKind.BuildingBlockWebOrOperations, displayName);
            }

            if (domain == "Core" || displayName.EndsWith(".Core", StringComparison.Ordinal))
            {
                return new ReferenceTarget(ReferenceKind.BuildingBlockAbstractionsOrCore, displayName);
            }

            return new ReferenceTarget(ReferenceKind.BuildingBlock, displayName);
        }

        return new ReferenceTarget(ReferenceKind.Forbidden, displayName);
    }

    private static ServiceProject[] LoadServiceProjects()
    {
        var servicesRoot = Path.Combine(FindBackendRoot(), "src", "Services");

        return Directory
            .EnumerateFiles(servicesRoot, "*.csproj", SearchOption.AllDirectories)
            .Select(path => CreateProject(servicesRoot, path))
            .OrderBy(project => project.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ServiceProject CreateProject(string servicesRoot, string projectPath)
    {
        var fullPath = Path.GetFullPath(projectPath);
        var relativePath = Path.GetRelativePath(servicesRoot, fullPath);
        var relativeSegments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var serviceName = relativeSegments[0];
        var name = Path.GetFileNameWithoutExtension(projectPath);
        var layer = GetLayer(name);
        var document = XDocument.Load(projectPath);
        var projectReferencePaths = document
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(fullPath)!, include!)))
            .ToArray();

        return new ServiceProject(
            serviceName,
            name,
            layer,
            fullPath,
            relativePath,
            projectReferencePaths);
    }

    private static string GetLayer(string projectName)
    {
        return KnownLayers.FirstOrDefault(layer =>
            projectName.EndsWith($".{layer}", StringComparison.Ordinal)) ?? string.Empty;
    }

    private static string FindBackendRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "LucidMicro.slnx")) &&
                Directory.Exists(Path.Combine(directory.FullName, "src", "Services")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to locate backend root.");
    }

    private sealed record ServiceProject(
        string ServiceName,
        string Name,
        string Layer,
        string FullPath,
        string RelativePath,
        string[] ProjectReferencePaths);

    private sealed record ReferenceTarget(ReferenceKind Kind, string DisplayName);

    private enum ReferenceKind
    {
        Forbidden,
        ServiceApplication,
        ServiceDomain,
        ServiceInfrastructure,
        Contracts,
        BuildingBlock,
        BuildingBlockAbstractionsOrCore,
        BuildingBlockCoreDomain,
        BuildingBlockWebOrOperations
    }
}
