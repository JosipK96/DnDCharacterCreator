using System.Reflection;
using System.Reflection.Emit;

using CharacterCreatorApp.Contracts.Common;

using FluentAssertions;

namespace CharacterCreatorApp.Client.UnitTests.Services;

public class BuildVersionTests
{
    [Fact]
    public void Of_AnAssemblyBuiltAsAReleaseVersion_ReturnsThatVersion()
    {
        // Arrange
        var assembly = AssemblyWithInformationalVersion("1.2.3");

        // Act
        var version = BuildVersion.Of(assembly);

        // Assert
        version.Should().Be("1.2.3");
    }

    [Fact]
    public void Of_AnAssemblyWithoutAVersion_ReturnsTheDevelopmentVersion()
    {
        // Arrange
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("NoVersion"), AssemblyBuilderAccess.Run);

        // Act
        var version = BuildVersion.Of(assembly);

        // Assert
        version.Should().Be(BuildVersion.Unversioned);
    }

    [Fact]
    public void Of_AnOrdinaryBuild_IsADevelopmentVersion()
    {
        // Act
        var version = BuildVersion.Of(typeof(BuildVersion).Assembly);

        // Assert
        version.Should().StartWith(BuildVersion.Unversioned);
    }

    private static AssemblyBuilder AssemblyWithInformationalVersion(string version)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Versioned"), AssemblyBuilderAccess.Run);
        var constructor = typeof(AssemblyInformationalVersionAttribute).GetConstructor([typeof(string)])!;
        assembly.SetCustomAttribute(new CustomAttributeBuilder(constructor, [version]));
        return assembly;
    }
}
