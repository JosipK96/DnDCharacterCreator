using System.Text.Json.Nodes;

using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.ValueObjects;
using CharacterCreatorApp.Infrastructure.IntegrationTests.Fixtures;
using CharacterCreatorApp.Infrastructure.Seed;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

using Npgsql;

namespace CharacterCreatorApp.Infrastructure.IntegrationTests.Seed;

/// <summary>
/// Startup replaces the reference data whenever the seed files change. These run against a
/// copy of the real files, so each test can change one without touching the others.
/// </summary>
public class SeedServiceTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 9, 28, 8, 0, 0, TimeSpan.Zero));
    private readonly DirectoryInfo _dataPath = Directory.CreateTempSubdirectory("seed-data-");

    public SeedServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        CopyDirectory(SeedService.DataPath, _dataPath.FullName);
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _dataPath.Delete(recursive: true);
        await _fixture.ResetDatabaseAsync();
    }

    [Fact]
    public async Task SeedData_EmptyDatabase_FillsTheTablesAndRecordsWhichFiles()
    {
        // Arrange
        var classesInFile = (await ReadArrayAsync("Classes.json")).Count;
        var expectedHash = await ReferenceDataHash.ComputeAsync(_dataPath.FullName, TestContext.Current.CancellationToken);

        // Act
        await SeedAsync();

        // Assert
        await using var context = _fixture.CreateNewContext();
        (await context.Classes.CountAsync(TestContext.Current.CancellationToken)).Should().Be(classesInFile);
        var versions = await context.ReferenceDataVersions.ToListAsync(TestContext.Current.CancellationToken);
        versions.Should().ContainSingle().Which.Hash.Should().Be(expectedHash);
        versions[0].AppliedAt.Should().Be(_timeProvider.GetUtcNow());
    }

    [Fact]
    public async Task SeedData_FilesUnchanged_LeavesTheTablesAlone()
    {
        // Arrange - a change made in the database stays only if the second run replaces nothing
        await SeedAsync();
        await using (var context = _fixture.CreateNewContext())
        {
            await context.Classes.Where(c => c.Id == "wizard")
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Name, "Marked"), TestContext.Current.CancellationToken);
        }

        // Act
        await SeedAsync();

        // Assert
        await using var check = _fixture.CreateNewContext();
        (await check.Classes.SingleAsync(c => c.Id == "wizard", TestContext.Current.CancellationToken)).Name.Should().Be("Marked");
    }

    [Fact]
    public async Task SeedData_FilesChanged_ReplacesTheDataAndKeepsTheCharacters()
    {
        // Arrange
        await SeedAsync();
        var characterId = await AddCharacterAsync();
        await ChangeArrayAsync("Classes.json", classes => WithId(classes, "wizard")["Name"] = "Wizard (renamed)");

        // Act
        await SeedAsync();

        // Assert
        await using var context = _fixture.CreateNewContext();
        (await context.Classes.SingleAsync(c => c.Id == "wizard", TestContext.Current.CancellationToken)).Name.Should().Be("Wizard (renamed)");
        (await context.Characters.SingleAsync(c => c.Id == characterId, TestContext.Current.CancellationToken)).ClassId.Should().Be("wizard");
    }

    [Fact]
    public async Task SeedData_ChangedFilesDropABackgroundACharacterHas_ThrowsAndKeepsTheOldData()
    {
        // Arrange
        await SeedAsync();
        await AddCharacterAsync();
        var hashBefore = await ReferenceDataHash.ComputeAsync(_dataPath.FullName, TestContext.Current.CancellationToken);
        await ChangeArrayAsync("Backgrounds.json", backgrounds => backgrounds.Remove(WithId(backgrounds, "acolyte")));

        // Act
        var act = SeedAsync;

        // Assert - the deferred foreign key fails the commit, which rolls the whole replace back
        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        await using var context = _fixture.CreateNewContext();
        (await context.Backgrounds.AnyAsync(b => b.Id == "acolyte", TestContext.Current.CancellationToken)).Should().BeTrue();
        (await context.ReferenceDataVersions.SingleAsync(TestContext.Current.CancellationToken)).Hash.Should().Be(hashBefore);
    }

    private async Task SeedAsync()
    {
        await using var context = _fixture.CreateNewContext();
        await using var services = new ServiceCollection()
            .AddLogging()
            .AddSeeders()
            .AddSingleton(context)
            .AddSingleton<IUnitOfWork>(new UnitOfWork(context))
            .AddSingleton<TimeProvider>(_timeProvider)
            .AddSingleton<SeedService>()
            .BuildServiceProvider();

        await services.GetRequiredService<SeedService>().SeedData(_dataPath.FullName, TestContext.Current.CancellationToken);
    }

    private async Task<Guid> AddCharacterAsync()
    {
        await using var context = _fixture.CreateNewContext();
        var character = Character.Create(
            Guid.NewGuid(), "Elminster", 5, RulesEdition.Edition2014, "dwarf", "hill_dwarf", "wizard", "evocation", "acolyte", [], _timeProvider);

        context.Characters.Add(character);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return character.Id;
    }

    private async Task<JsonArray> ReadArrayAsync(string file)
    {
        var text = await File.ReadAllTextAsync(Path.Combine(_dataPath.FullName, file), TestContext.Current.CancellationToken);
        return JsonNode.Parse(text)!.AsArray();
    }

    private async Task ChangeArrayAsync(string file, Action<JsonArray> change)
    {
        var array = await ReadArrayAsync(file);
        change(array);
        await File.WriteAllTextAsync(Path.Combine(_dataPath.FullName, file), array.ToJsonString(), TestContext.Current.CancellationToken);
    }

    private static JsonNode WithId(JsonArray array, string id) =>
        array.Single(node => node!["Id"]!.GetValue<string>() == id)!;

    private static void CopyDirectory(string source, string target)
    {
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }
    }
}
