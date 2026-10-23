// .editorconfig treats every file in a Migrations folder as generated, which turns nullable off
#nullable enable

using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.ValueObjects;
using CharacterCreatorApp.Infrastructure.IntegrationTests.Fixtures;
using CharacterCreatorApp.Infrastructure.Migrations;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace CharacterCreatorApp.Infrastructure.IntegrationTests.Migrations;

/// <summary>
/// Runs the EditCharacter migration's backfill over characters and the completed drafts that
/// made them, as the hosted database holds them: drafts completed but linked to nothing.
/// </summary>
public class EditCharacterMigrationTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = fixture;
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly Guid _userId = Guid.NewGuid();

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await _fixture.ResetDatabaseAsync();

    [Fact]
    public async Task LinkCharactersToTheirDrafts_LinksEachCharacterToTheDraftThatMadeIt()
    {
        // Arrange - Thorin made twice a week apart, each from its own draft; Gimli from one
        // whose race a reseed later took away, so nothing matches him
        await using var transaction = await BeginWithoutReferenceDataAsync();
        var firstThorin = await AddCharacterWithDraftAsync("Thorin", "dwarf");
        _timeProvider.Advance(TimeSpan.FromDays(7));
        var secondThorin = await AddCharacterWithDraftAsync("Thorin", "dwarf");
        var gimli = await AddCharacterWithDraftAsync("Gimli", "dwarf", draftRaceId: "human");
        await UnlinkEveryDraftAsync();

        // Act
        await _fixture.DbContext.Database.ExecuteSqlRawAsync(EditCharacter.LinkCharactersToTheirDraftsSql, TestContext.Current.CancellationToken);

        // Assert
        (await CharacterOfAsync(firstThorin.DraftId)).Should().Be(firstThorin.CharacterId);
        (await CharacterOfAsync(secondThorin.DraftId)).Should().Be(secondThorin.CharacterId);
        (await CharacterOfAsync(gimli.DraftId)).Should().BeNull();
    }

    [Fact]
    public async Task LinkCharactersToTheirDrafts_LeavesAnUnfinishedDraftAlone()
    {
        // Arrange - the same name and choices, but still in the wizard
        await using var transaction = await BeginWithoutReferenceDataAsync();
        var character = Character.Create(_userId, "Thorin", 1, RulesEdition.Edition2014, "dwarf", null, "fighter", null, "soldier", Scores(), _timeProvider);
        _fixture.DbContext.Characters.Add(character);
        var draft = Draft("Thorin", "dwarf");
        _fixture.DbContext.CharacterDrafts.Add(draft);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _fixture.DbContext.Database.ExecuteSqlRawAsync(EditCharacter.LinkCharactersToTheirDraftsSql, TestContext.Current.CancellationToken);

        // Assert
        (await CharacterOfAsync(draft.Id)).Should().BeNull();
    }

    // Rolled back when disposed; the character keys to the reference data wait for that commit
    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginWithoutReferenceDataAsync()
    {
        var transaction = await _fixture.DbContext.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await _fixture.DbContext.Database.ExecuteSqlRawAsync("SET CONSTRAINTS ALL DEFERRED", TestContext.Current.CancellationToken);
        return transaction;
    }

    // As finalize saved them: the character and its draft at the same moment
    private async Task<(Guid CharacterId, Guid DraftId)> AddCharacterWithDraftAsync(string name, string raceId, string? draftRaceId = null)
    {
        var character = Character.Create(_userId, name, 1, RulesEdition.Edition2014, raceId, null, "fighter", null, "soldier", Scores(), _timeProvider);
        var draft = Draft(name, draftRaceId ?? raceId);
        draft.MarkAsCompleted(character.Id);

        _fixture.DbContext.Characters.Add(character);
        _fixture.DbContext.CharacterDrafts.Add(draft);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (character.Id, draft.Id);
    }

    private CharacterDraft Draft(string name, string raceId)
    {
        var draft = CharacterDraft.CreateEmpty(_userId, _timeProvider);
        draft.ChooseEdition(RulesEdition.Edition2014);
        draft.SetNameAndLevel(name, 1);
        draft.SetRace(raceId);
        draft.SetClass("fighter");
        draft.SetBackground("soldier");
        return draft;
    }

    // Before this migration finalize kept the draft but not which character it made
    private async Task UnlinkEveryDraftAsync()
    {
        await _fixture.DbContext.Database.ExecuteSqlRawAsync("""UPDATE "CharacterDrafts" SET "CharacterId" = NULL""", TestContext.Current.CancellationToken);
        _fixture.DbContext.ChangeTracker.Clear();
    }

    private async Task<Guid?> CharacterOfAsync(Guid draftId) =>
        await _fixture.DbContext.CharacterDrafts.AsNoTracking()
            .Where(d => d.Id == draftId)
            .Select(d => d.CharacterId)
            .SingleAsync(TestContext.Current.CancellationToken);

    private static Dictionary<string, int> Scores() =>
        new() { ["STR"] = 15, ["DEX"] = 14, ["CON"] = 13, ["INT"] = 12, ["WIS"] = 10, ["CHA"] = 8 };
}
