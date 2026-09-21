using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Architecture.Tests.DomainRules;

public sealed class DomainArchitectureTests
{
    private const string SolutionFileName =
        "ECommerce.FlashSaleOrchestrator.slnx";

    private static readonly string DomainProjectPath =
        Path.Combine(
            "src",
            "ECommerce.FlashSaleOrchestrator.Domain",
            "ECommerce.FlashSaleOrchestrator.Domain.csproj");

    [Fact]
    public void DomainProject_ShouldNotReferenceInfrastructureTechnologies()
    {
        var references =
            GetPackageAndFrameworkReferences(
                DomainProjectPath);

        var forbiddenReferences =
            references
                .Where(IsForbiddenDomainDependency)
                .OrderBy(
                    reference => reference,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        Assert.Empty(forbiddenReferences);
    }

    [Fact]
    public void DomainTypes_ShouldNotExposePublicMutableSetters()
    {
        var domainAssembly =
            typeof(Product).Assembly;

        var mutableProperties =
            domainAssembly
                .GetExportedTypes()
                .Where(type => type.IsClass)
                .SelectMany(
                    type => type
                        .GetProperties(
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.DeclaredOnly)
                        .Select(
                            property =>
                                new
                                {
                                    Type = type,
                                    Property = property
                                }))
                .Where(candidate =>
                    HasPublicMutableSetter(
                        candidate.Property))
                .Select(candidate =>
                    $"{candidate.Type.FullName}.{candidate.Property.Name}")
                .OrderBy(
                    property => property,
                    StringComparer.Ordinal)
                .ToArray();

        Assert.Empty(mutableProperties);
    }

    private static IReadOnlyCollection<string>
        GetPackageAndFrameworkReferences(
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
                element.Name.LocalName is
                    "PackageReference" or
                    "FrameworkReference")
            .Select(element =>
                element.Attribute("Include")?.Value)
            .Where(reference =>
                !string.IsNullOrWhiteSpace(reference))
            .Select(reference => reference!)
            .ToArray();
    }

    private static bool IsForbiddenDomainDependency(
        string reference)
    {
        return
            reference.StartsWith(
                "Microsoft.EntityFrameworkCore",
                StringComparison.OrdinalIgnoreCase) ||
            reference.StartsWith(
                "Confluent.Kafka",
                StringComparison.OrdinalIgnoreCase) ||
            reference.StartsWith(
                "NRedisStack",
                StringComparison.OrdinalIgnoreCase) ||
            reference.StartsWith(
                "StackExchange.Redis",
                StringComparison.OrdinalIgnoreCase) ||
            reference.StartsWith(
                "Microsoft.AspNetCore",
                StringComparison.OrdinalIgnoreCase) ||
            reference.StartsWith(
                "Microsoft.SemanticKernel",
                StringComparison.OrdinalIgnoreCase) ||
            reference.StartsWith(
                "Microsoft.Extensions.AI.OpenAI",
                StringComparison.OrdinalIgnoreCase) ||
            reference.Equals(
                "OpenAI",
                StringComparison.OrdinalIgnoreCase) ||
            reference.StartsWith(
                "OpenAI.",
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasPublicMutableSetter(
        PropertyInfo property)
    {
        var setter =
            property.GetSetMethod(
                nonPublic: true);

        if (setter is null ||
            !setter.IsPublic)
        {
            return false;
        }

        return !IsInitOnly(setter);
    }

    private static bool IsInitOnly(
        MethodInfo setter)
    {
        return setter
            .ReturnParameter
            .GetRequiredCustomModifiers()
            .Contains(typeof(IsExternalInit));
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