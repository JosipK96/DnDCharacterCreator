using CharacterCreatorApp.Infrastructure;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.IntegrationTests.Migrations;

public class MigrationsTests
{
    [Fact]
    public void Model_MatchesTheLatestMigrationSnapshot()
    {
        // Arrange - compares the model with the snapshot only, so no database is opened
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=unused")
            .Options;
        using var context = new ApplicationDbContext(options);

        // Act
        var hasPendingChanges = context.Database.HasPendingModelChanges();

        // Assert
        hasPendingChanges.Should().BeFalse(
            "a change to the EF model needs a migration: dotnet ef migrations add <Name> from the Infrastructure project");
    }
}
