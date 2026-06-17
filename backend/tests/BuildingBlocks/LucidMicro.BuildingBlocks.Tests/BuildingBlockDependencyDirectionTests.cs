using System.Xml.Linq;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class BuildingBlockDependencyDirectionTests
{
    private static readonly HashSet<string> KnownDomains = new(StringComparer.Ordinal)
    {
        "Communication",
        "Core",
        "Data",
        "Messaging",
        "Operations",
        "Web"
    };

    [Fact]
    public void BuildingBlockProjects_StayUnderKnownCapabilityDomains()
    {
        var projects = LoadProjects();
        var violations = projects
            .Where(project => !KnownDomains.Contains(project.Domain))
            .Select(project => project.RelativePath)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void BuildingBlockProjectReferences_FollowCapabilityDomainRules()
    {
        var projects = LoadProjects();
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

                if (!IsAllowedCapabilityDomainReference(source, target))
                {
                    violations.Add($"{source.Name} ({source.Domain}) -> {target.Name} ({target.Domain})");
                }

                if (source.Domain != "Operations" && target.Domain == "Operations")
                {
                    violations.Add($"{source.Name} must not depend on Operations project {target.Name}");
                }
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void BuildingBlockAbstractions_DoNotReferenceImplementationProjects()
    {
        var projects = LoadProjects();
        var projectsByPath = projects.ToDictionary(
            project => project.FullPath,
            StringComparer.OrdinalIgnoreCase);
        var violations = new List<string>();

        foreach (var source in projects.Where(project => project.IsAbstractions))
        {
            foreach (var targetPath in source.ProjectReferencePaths)
            {
                if (!projectsByPath.TryGetValue(targetPath, out var target))
                {
                    continue;
                }

                if (!target.IsAbstractions && !target.IsCoreFoundation)
                {
                    violations.Add($"{source.Name} -> {target.Name}");
                }
            }
        }

        Assert.Empty(violations);
    }

    private static bool IsAllowedCapabilityDomainReference(BuildingBlockProject source, BuildingBlockProject target)
    {
        return source.Domain switch
        {
            "Core" => target.Domain == "Core",
            "Data" => target.Domain is "Core" or "Data",
            "Messaging" => target.Domain == "Messaging",
            "Communication" => target.Domain is "Core" or "Communication",
            "Web" => target.Domain is "Core" or "Data" or "Web",
            "Operations" => KnownDomains.Contains(target.Domain),
            _ => false
        };
    }

    private static BuildingBlockProject[] LoadProjects()
    {
        var buildingBlocksRoot = Path.Combine(FindBackendRoot(), "src", "BuildingBlocks");

        return Directory
            .EnumerateFiles(buildingBlocksRoot, "*.csproj", SearchOption.AllDirectories)
            .Select(path => CreateProject(buildingBlocksRoot, path))
            .OrderBy(project => project.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static BuildingBlockProject CreateProject(string buildingBlocksRoot, string projectPath)
    {
        var fullPath = Path.GetFullPath(projectPath);
        var relativePath = Path.GetRelativePath(buildingBlocksRoot, fullPath);
        var relativeSegments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var domain = relativeSegments[0];
        var name = Path.GetFileNameWithoutExtension(projectPath);
        var document = XDocument.Load(projectPath);
        var projectReferencePaths = document
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => ResolveProjectReferencePath(fullPath, include!))
            .ToArray();

        return new BuildingBlockProject(
            name,
            domain,
            fullPath,
            relativePath,
            projectReferencePaths);
    }

    private static string ResolveProjectReferencePath(string projectPath, string include)
    {
        var normalizedInclude = include
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectPath)!, normalizedInclude));
    }

    private static string FindBackendRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "LucidMicro.slnx")) &&
                Directory.Exists(Path.Combine(directory.FullName, "src", "BuildingBlocks")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to locate backend root.");
    }

    private sealed record BuildingBlockProject(
        string Name,
        string Domain,
        string FullPath,
        string RelativePath,
        string[] ProjectReferencePaths)
    {
        public bool IsAbstractions => Name.EndsWith(".Abstractions", StringComparison.Ordinal);

        public bool IsCoreFoundation => Domain == "Core" || Name.EndsWith(".Core", StringComparison.Ordinal);
    }
}
