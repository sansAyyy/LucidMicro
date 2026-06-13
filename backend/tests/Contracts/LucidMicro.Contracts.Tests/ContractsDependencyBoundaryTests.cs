using System.Xml.Linq;

namespace LucidMicro.Contracts.Tests;

public sealed class ContractsDependencyBoundaryTests
{
    [Fact]
    public void ContractProjects_DoNotReferenceServices()
    {
        var backendRoot = FindBackendRoot();
        var projects = LoadContractProjects(backendRoot);
        var violations = projects
            .SelectMany(project => project.ProjectReferencePaths.Select(reference => new
            {
                Project = project,
                Reference = ClassifyReference(backendRoot, reference)
            }))
            .Where(item => item.Reference.Kind == ReferenceKind.Service)
            .Select(item => $"{item.Project.Name} -> {item.Reference.DisplayName}")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ContractProjects_OnlyReferenceContractsOrBuildingBlockAbstractions()
    {
        var backendRoot = FindBackendRoot();
        var projects = LoadContractProjects(backendRoot);
        var violations = projects
            .SelectMany(project => project.ProjectReferencePaths.Select(reference => new
            {
                Project = project,
                Reference = ClassifyReference(backendRoot, reference)
            }))
            .Where(item => item.Reference.Kind is not ReferenceKind.Contract and not ReferenceKind.BuildingBlockAbstractions)
            .Select(item => $"{item.Project.Name} -> {item.Reference.DisplayName}")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ContractProjects_DoNotReferencePackagesDirectly()
    {
        var backendRoot = FindBackendRoot();
        var projects = LoadContractProjects(backendRoot);
        var violations = projects
            .Where(project => project.PackageReferences.Length > 0)
            .Select(project => $"{project.Name}: {string.Join(", ", project.PackageReferences)}")
            .ToArray();

        Assert.Empty(violations);
    }

    private static ReferenceTarget ClassifyReference(string backendRoot, string referencePath)
    {
        var srcRoot = Path.Combine(backendRoot, "src");
        var relativePath = Path.GetRelativePath(srcRoot, referencePath);
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var displayName = Path.GetFileNameWithoutExtension(referencePath);

        if (segments is ["Contracts", ..])
        {
            return new ReferenceTarget(ReferenceKind.Contract, displayName);
        }

        if (segments is ["Services", ..])
        {
            return new ReferenceTarget(ReferenceKind.Service, displayName);
        }

        if (segments is ["BuildingBlocks", ..] && displayName.EndsWith(".Abstractions", StringComparison.Ordinal))
        {
            return new ReferenceTarget(ReferenceKind.BuildingBlockAbstractions, displayName);
        }

        return new ReferenceTarget(ReferenceKind.Forbidden, displayName);
    }

    private static ContractProject[] LoadContractProjects(string backendRoot)
    {
        var contractsRoot = Path.Combine(backendRoot, "src", "Contracts");

        return Directory
            .EnumerateFiles(contractsRoot, "*.csproj", SearchOption.AllDirectories)
            .Select(path => CreateProject(contractsRoot, path))
            .OrderBy(project => project.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ContractProject CreateProject(string contractsRoot, string projectPath)
    {
        var fullPath = Path.GetFullPath(projectPath);
        var document = XDocument.Load(projectPath);
        var projectReferencePaths = document
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(fullPath)!, include!)))
            .ToArray();
        var packageReferences = document
            .Descendants("PackageReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!)
            .ToArray();

        return new ContractProject(
            Path.GetFileNameWithoutExtension(projectPath),
            Path.GetRelativePath(contractsRoot, fullPath),
            projectReferencePaths,
            packageReferences);
    }

    private static string FindBackendRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "LucidMicro.slnx")) &&
                Directory.Exists(Path.Combine(directory.FullName, "src", "Contracts")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to locate backend root.");
    }

    private sealed record ContractProject(
        string Name,
        string RelativePath,
        string[] ProjectReferencePaths,
        string[] PackageReferences);

    private sealed record ReferenceTarget(ReferenceKind Kind, string DisplayName);

    private enum ReferenceKind
    {
        Forbidden,
        Contract,
        BuildingBlockAbstractions,
        Service
    }
}
