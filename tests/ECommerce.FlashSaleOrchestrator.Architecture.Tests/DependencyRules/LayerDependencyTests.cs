using System.Xml.Linq;

namespace ECommerce.FlashSaleOrchestrator.Architecture.Tests.DependencyRules;

public sealed class LayerDependencyTests
{
    private const string SolutionFileName =
        "ECommerce.FlashSaleOrchestrator.slnx";

    private static readonly string DomainProjectPath =
        Path.Combine(
            "src",
            "ECommerce.FlashSaleOrchestrator.Domain",
            "ECommerce.FlashSaleOrchestrator.Domain.csproj");

    private static readonly string ApplicationProjectPath =
        Path.Combine(
            "src",
            "ECommerce.FlashSaleOrchestrator.Application",
            "ECommerce.FlashSaleOrchestrator.Application.csproj");

    private static readonly string InfrastructureProjectPath =
        Path.Combine(
            "src",
            "ECommerce.FlashSaleOrchestrator.Infrastructure",
            "ECommerce.FlashSaleOrchestrator.Infrastructure.csproj");

    public static IEnumerable<object[]> ForbiddenProjectReferences()
    {
        yield return
        [
            DomainProjectPath,
            "ECommerce.FlashSaleOrchestrator.Application.csproj"
        ];

        yield return
        [
            DomainProjectPath,
            "ECommerce.FlashSaleOrchestrator.Infrastructure.csproj"
        ];

        yield return
        [
            DomainProjectPath,
            "ECommerce.FlashSaleOrchestrator.Api.csproj"
        ];

        yield return
        [
            DomainProjectPath,
            "ECommerce.FlashSaleOrchestrator.Worker.csproj"
        ];

        yield return
        [
            ApplicationProjectPath,
            "ECommerce.FlashSaleOrchestrator.Infrastructure.csproj"
        ];

        yield return
        [
            ApplicationProjectPath,
            "ECommerce.FlashSaleOrchestrator.Api.csproj"
        ];

        yield return
        [
            ApplicationProjectPath,
            "ECommerce.FlashSaleOrchestrator.Worker.csproj"
        ];

        yield return
        [
            InfrastructureProjectPath,
            "ECommerce.FlashSaleOrchestrator.Api.csproj"
        ];

        yield return
        [
            InfrastructureProjectPath,
            "ECommerce.FlashSaleOrchestrator.Worker.csproj"
        ];
    }

    [Theory]
    [MemberData(nameof(ForbiddenProjectReferences))]
    public void Project_ShouldNotReferenceForbiddenProject(
        string sourceProjectPath,
        string forbiddenProjectFileName)
    {
        var projectReferences =
            GetProjectReferences(sourceProjectPath);

        Assert.DoesNotContain(
            projectReferences,
            projectReference =>
                string.Equals(
                    projectReference,
                    forbiddenProjectFileName,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyCollection<string> GetProjectReferences(
        string relativeProjectPath)
    {
        var repositoryRoot =
            FindRepositoryRoot();

        var projectPath =
            Path.Combine(
                repositoryRoot,
                relativeProjectPath);

        var document =
            XDocument.Load(projectPath);

        return document
            .Descendants()
            .Where(element =>
                element.Name.LocalName == "ProjectReference")
            .Select(element =>
                element.Attribute("Include")?.Value)
            .Where(include =>
                !string.IsNullOrWhiteSpace(include))
            .Select(include =>
                Path.GetFileName(include!))
            .ToArray();
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory =
            new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var solutionPath =
                Path.Combine(
                    directory.FullName,
                    SolutionFileName);

            if (File.Exists(solutionPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate repository root containing " +
            $"'{SolutionFileName}'.");
    }
}