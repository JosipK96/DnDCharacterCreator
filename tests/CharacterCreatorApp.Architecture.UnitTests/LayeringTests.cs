using System.Reflection;

using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Infrastructure;

using FluentAssertions;

using NetArchTest.Rules;

namespace CharacterCreatorApp.Architecture.UnitTests;

// The layering rules in the root CLAUDE.md ("Where code goes"), checked against the built
// assemblies. Project references already stop some of them; these catch the rest, and a
// reference added by mistake, so a change that breaks the layering fails CI.
public class LayeringTests
{
    private static readonly Assembly Domain = typeof(CharacterCreatorApp.Domain.DomainModels.Characters.Character).Assembly;
    private static readonly Assembly Application = typeof(ICharacterRepository).Assembly;
    private static readonly Assembly Infrastructure = typeof(ApplicationDbContext).Assembly;
    private static readonly Assembly Contracts = typeof(CharacterCreatorApp.Contracts.Common.FilterRequest<>).Assembly;
    private static readonly Assembly Api = typeof(CharacterCreatorApp.Api.DependencyInjection).Assembly;

    [Fact]
    public void Domain_References_NoPackageAndNoOtherProject()
    {
        // Arrange
        var allowed = new[] { "System", "netstandard", "mscorlib" };

        // Act
        var references = Domain.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => !allowed.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        // Assert
        references.Should().BeEmpty("the domain model is plain C#, with no package and no other project");
    }

    [Fact]
    public void Application_References_NeitherAspNetCoreNorContractsNorInfrastructure()
    {
        // Arrange
        var forbidden = new[]
        {
            "Microsoft.AspNetCore",
            Contracts.GetName().Name!,
            Infrastructure.GetName().Name!,
        };

        // Act
        var references = Application.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => forbidden.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        // Assert
        references.Should().BeEmpty("Application depends on the domain and on abstractions only");
    }

    [Fact]
    public void TypesOutsideInfrastructure_DoNotUseTheDbContext()
    {
        // Arrange
        var outside = new[] { Domain, Application, Contracts, Api };

        // Act
        // Program is the composition root: it applies the migrations at startup, which needs
        // the context itself, so it is the one type outside Infrastructure allowed to name it.
        var result = Types.InAssemblies(outside)
            .That()
            .DoNotHaveName("Program")
            .ShouldNot()
            .HaveDependencyOnAny(typeof(ApplicationDbContext).FullName!, "Microsoft.EntityFrameworkCore.DbContext")
            .GetResult();

        // Assert
        result.FailingTypeNames.Should().BeNullOrEmpty("the DbContext stays in Infrastructure behind repositories");
    }

    [Fact]
    public void Repositories_NeverReturnIQueryable()
    {
        // Arrange
        var repositories = Application.GetTypes()
            .Concat(Infrastructure.GetTypes())
            .Where(t => t.Name.EndsWith("Repository", StringComparison.Ordinal))
            .ToList();

        // Act
        var offenders = repositories
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(m => ExposesQueryable(m.ReturnType))
            .Select(m => $"{m.DeclaringType!.Name}.{m.Name}")
            .ToList();

        // Assert
        repositories.Should().NotBeEmpty("the rule would pass vacuously if no repository were found");
        offenders.Should().BeEmpty("a repository returns a domain entity or a read model, never an IQueryable");
    }

    private static bool ExposesQueryable(Type type)
    {
        if (type.IsArray)
        {
            return ExposesQueryable(type.GetElementType()!);
        }

        if (type == typeof(IQueryable) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>)))
        {
            return true;
        }

        return type.IsGenericType && type.GetGenericArguments().Any(ExposesQueryable);
    }
}
