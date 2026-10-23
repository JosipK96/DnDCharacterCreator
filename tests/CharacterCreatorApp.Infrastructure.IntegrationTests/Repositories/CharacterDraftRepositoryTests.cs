using CharacterCreatorApp.Infrastructure.IntegrationTests.Fixtures;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Infrastructure.Repositories;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Infrastructure.IntegrationTests.Repositories;

public class CharacterDraftRepositoryTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly CharacterDraftRepository _repository;
    private readonly FakeTimeProvider _timeProvider;

    public CharacterDraftRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new CharacterDraftRepository(_fixture.DbContext);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero));
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        // Clean database after each test
        await _fixture.ResetDatabaseAsync();
    }

    [Fact]
    public async Task CreateAsync_SavesDraftToDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var draft = CharacterDraft.CreateEmpty(userId, _timeProvider);

        // Act
        await _repository.CreateAsync(draft, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var savedDraft = await _repository.GetByIdAsync(draft.Id, userId, TestContext.Current.CancellationToken);
        savedDraft.Should().NotBeNull();
        savedDraft!.Id.Should().Be(draft.Id);
        savedDraft.CreatedByUserId.Should().Be(userId);
        savedDraft.CreatedAt.Should().Be(_timeProvider.GetUtcNow());
        savedDraft.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingDraft_ReturnsDraft()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var draft = CharacterDraft.CreateEmpty(userId, _timeProvider);
        draft.SetNameAndLevel("Thorin", 5);

        await _repository.CreateAsync(draft, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByIdAsync(draft.Id, userId, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(draft.Id);
        result.Name.Should().Be("Thorin");
        result.Level.Should().Be(5);
    }

    [Fact]
    public async Task SaveChanges_AfterAJsonListChangedInPlace_SavesTheChange()
    {
        // Arrange - the columns HasJsonConversion stores as jsonb (these improvements, and the
        // character's lists) once had no value comparer, so EF compared the list reference and
        // an Add to the list it had loaded was never written. The draft's other lists are
        // PostgreSQL arrays, which EF compares by content on its own.
        var userId = Guid.NewGuid();
        var draft = CharacterDraft.CreateEmpty(userId, _timeProvider);
        draft.SetAbilityScoreImprovements([new Dictionary<string, int> { ["STR"] = 2 }]);
        await _repository.CreateAsync(draft, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var editing = _fixture.CreateNewContext();
        var loaded = await editing.CharacterDrafts.SingleAsync(d => d.Id == draft.Id, TestContext.Current.CancellationToken);
        loaded.AbilityScoreImprovements.Add(new Dictionary<string, int> { ["DEX"] = 2 });

        // Act
        await editing.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var reading = _fixture.CreateNewContext();
        var saved = await reading.CharacterDrafts.AsNoTracking().SingleAsync(d => d.Id == draft.Id, TestContext.Current.CancellationToken);
        saved.AbilityScoreImprovements.Should().BeEquivalentTo(
            [new Dictionary<string, int> { ["STR"] = 2 }, new Dictionary<string, int> { ["DEX"] = 2 }]);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentDraft_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId, Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDetachedEntity()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var draft = CharacterDraft.CreateEmpty(userId, _timeProvider);

        await _repository.CreateAsync(draft, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByIdAsync(draft.Id, userId, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        _fixture.DbContext.Entry(result!).State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task GetByIdForUpdateAsync_WithCorrectUser_ReturnsDraft()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var draft = CharacterDraft.CreateEmpty(userId, _timeProvider);

        await _repository.CreateAsync(draft, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByIdForUpdateAsync(draft.Id, userId, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(draft.Id);
        result.CreatedByUserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetByIdForUpdateAsync_WithWrongUser_ReturnsNull()
    {
        // Arrange - Create draft for user1
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var draft = CharacterDraft.CreateEmpty(user1Id, _timeProvider);

        await _repository.CreateAsync(draft, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - Try to get draft as user2
        var result = await _repository.GetByIdForUpdateAsync(draft.Id, user2Id, TestContext.Current.CancellationToken);

        // Assert - Should not return draft (authorization check)
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdForUpdateAsync_ReturnsTrackedEntity()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var draft = CharacterDraft.CreateEmpty(userId, _timeProvider);

        await _repository.CreateAsync(draft, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByIdForUpdateAsync(draft.Id, userId, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        _fixture.DbContext.Entry(result!).State.Should().Be(EntityState.Unchanged);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesDraftInDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var draft = CharacterDraft.CreateEmpty(userId, _timeProvider);

        await _repository.CreateAsync(draft, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Get draft for update
        var draftToUpdate = await _repository.GetByIdForUpdateAsync(draft.Id, userId, TestContext.Current.CancellationToken);
        draftToUpdate!.SetNameAndLevel("Updated Name", 10);

        // Act
        await _repository.UpdateAsync(draftToUpdate, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - Verify with fresh context
        using var newContext = _fixture.CreateNewContext();
        var updatedDraft = await newContext.CharacterDrafts.FindAsync([draft.Id], TestContext.Current.CancellationToken);
        updatedDraft.Should().NotBeNull();
        updatedDraft!.Name.Should().Be("Updated Name");
        updatedDraft.Level.Should().Be(10);
    }

    [Fact]
    public async Task UpdateAsync_WithAbilityScores_PersistsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var draft = CharacterDraft.CreateEmpty(userId, _timeProvider);

        await _repository.CreateAsync(draft, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var draftToUpdate = await _repository.GetByIdForUpdateAsync(draft.Id, userId, TestContext.Current.CancellationToken);
        var abilityScores = new Dictionary<string, int>
        {
            ["STR"] = 15,
            ["DEX"] = 14,
            ["CON"] = 13,
            ["INT"] = 12,
            ["WIS"] = 10,
            ["CHA"] = 8
        };
        draftToUpdate!.SetBaseAbilityScores(abilityScores, AbilityScoreMethod.Rolled);

        // Act
        await _repository.UpdateAsync(draftToUpdate, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        using var newContext = _fixture.CreateNewContext();
        var updatedDraft = await newContext.CharacterDrafts.FindAsync([draft.Id], TestContext.Current.CancellationToken);
        updatedDraft.Should().NotBeNull();
        updatedDraft!.BaseAbilityScores.Should().BeEquivalentTo(abilityScores);
    }

    [Fact]
    public async Task DeleteAsync_RemovesDraftFromDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var draft = CharacterDraft.CreateEmpty(userId, _timeProvider);

        await _repository.CreateAsync(draft, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _repository.DeleteAsync(draft, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var deletedDraft = await _repository.GetByIdAsync(draft.Id, userId, TestContext.Current.CancellationToken);
        deletedDraft.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WithComplexData_PersistsAllFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var draft = CharacterDraft.CreateEmpty(userId, _timeProvider);
        draft.SetNameAndLevel("Gandalf", 10);
        draft.SetRace("elf", "high-elf");
        draft.SetClass("wizard", "evocation");
        draft.SetBackground("sage");
        draft.SetBaseAbilityScores(new Dictionary<string, int>
        {
            ["STR"] = 8,
            ["DEX"] = 14,
            ["CON"] = 13,
            ["INT"] = 18,
            ["WIS"] = 15,
            ["CHA"] = 12
        }, AbilityScoreMethod.Rolled);
        draft.SetClassProficiencies(new List<string> { "arcana", "history" });
        draft.SetSpells(new List<string> { "fireball", "magic-missile" });

        // Act
        await _repository.CreateAsync(draft, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        using var newContext = _fixture.CreateNewContext();
        var savedDraft = await newContext.CharacterDrafts.FindAsync([draft.Id], TestContext.Current.CancellationToken);

        savedDraft.Should().NotBeNull();
        savedDraft!.Name.Should().Be("Gandalf");
        savedDraft.Level.Should().Be(10);
        savedDraft.RaceId.Should().Be("elf");
        savedDraft.SubraceId.Should().Be("high-elf");
        savedDraft.ClassId.Should().Be("wizard");
        savedDraft.SubclassId.Should().Be("evocation");
        savedDraft.BackgroundId.Should().Be("sage");
        savedDraft.BaseAbilityScores.Should().HaveCount(6);
        savedDraft.SelectedClassProficiencyIds.Should().Contain(new[] { "arcana", "history" });
        savedDraft.SelectedSpellIds.Should().Contain(new[] { "fireball", "magic-missile" });
    }

    [Fact]
    public async Task CreateAsync_WithTheRaceAndFeatIncreases_PersistsThem()
    {
        // Arrange - both are jsonb: ability ids to points, and a list of them in the feats' order
        var userId = Guid.NewGuid();
        var draft = CharacterDraft.CreateEmpty(userId, _timeProvider);
        draft.SetRace("half_elf", abilityIncreases: new Dictionary<string, int> { ["DEX"] = 1, ["WIS"] = 1 });
        draft.SetFeats(["athlete_2014", "alert_2014"], [new() { ["STR"] = 1 }, []]);

        // Act
        await _repository.CreateAsync(draft, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var reading = _fixture.CreateNewContext();
        var saved = await reading.CharacterDrafts.AsNoTracking().SingleAsync(d => d.Id == draft.Id, TestContext.Current.CancellationToken);
        saved.RaceAbilityIncreases.Should().BeEquivalentTo(new Dictionary<string, int> { ["DEX"] = 1, ["WIS"] = 1 });
        saved.FeatAbilityIncreases.Should().BeEquivalentTo(
            [new Dictionary<string, int> { ["STR"] = 1 }, []], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Repository_SupportsMultipleDraftsPerUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var draft1 = CharacterDraft.CreateEmpty(userId, _timeProvider);
        draft1.SetNameAndLevel("Character 1", 1);

        var draft2 = CharacterDraft.CreateEmpty(userId, _timeProvider);
        draft2.SetNameAndLevel("Character 2", 5);

        // Act
        await _repository.CreateAsync(draft1, TestContext.Current.CancellationToken);
        await _repository.CreateAsync(draft2, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var savedDraft1 = await _repository.GetByIdAsync(draft1.Id, userId, TestContext.Current.CancellationToken);
        var savedDraft2 = await _repository.GetByIdAsync(draft2.Id, userId, TestContext.Current.CancellationToken);

        savedDraft1.Should().NotBeNull();
        savedDraft2.Should().NotBeNull();
        savedDraft1!.Name.Should().Be("Character 1");
        savedDraft2!.Name.Should().Be("Character 2");
    }

    [Fact]
    public async Task GetByIdForUpdateAsync_ACompletedDraft_ReturnsNull()
    {
        // Arrange - a completed draft is a character's build, which no step may change
        await using var transaction = await BeginWithoutReferenceDataAsync();
        var userId = Guid.NewGuid();
        var character = await AddCharacterAsync(userId);
        var build = await AddBuildAsync(userId, character);

        // Act
        var result = await _repository.GetByIdForUpdateAsync(build.Id, userId, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBuildAndGetEdit_FindTheCharactersCompletedDraftAndItsUnfinishedEdit()
    {
        // Arrange
        await using var transaction = await BeginWithoutReferenceDataAsync();
        var userId = Guid.NewGuid();
        var character = await AddCharacterAsync(userId);
        var build = await AddBuildAsync(userId, character);
        var edit = CharacterDraft.StartEdit(build, _timeProvider);
        await _repository.CreateAsync(edit, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var foundBuild = await _repository.GetBuildAsync(character.Id, userId, TestContext.Current.CancellationToken);
        var foundEdit = await _repository.GetEditAsync(character.Id, userId, TestContext.Current.CancellationToken);
        var someoneElses = await _repository.GetBuildAsync(character.Id, Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        foundBuild!.Id.Should().Be(build.Id);
        foundEdit!.Id.Should().Be(edit.Id);
        someoneElses.Should().BeNull();
    }

    [Fact]
    public async Task GetIncompleteDraft_WithOnlyAnEdit_ReturnsNull()
    {
        // Arrange - an edit is never the new character to resume
        await using var transaction = await BeginWithoutReferenceDataAsync();
        var userId = Guid.NewGuid();
        var character = await AddCharacterAsync(userId);
        var edit = CharacterDraft.StartEdit(await AddBuildAsync(userId, character), _timeProvider);
        await _repository.CreateAsync(edit, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetIncompleteDraft(userId, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeletingACharacter_DeletesItsBuildAndEdit()
    {
        // Arrange
        await using var transaction = await BeginWithoutReferenceDataAsync();
        var userId = Guid.NewGuid();
        var character = await AddCharacterAsync(userId);
        var edit = CharacterDraft.StartEdit(await AddBuildAsync(userId, character), _timeProvider);
        await _repository.CreateAsync(edit, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _fixture.DbContext.Database.ExecuteSqlAsync(
            $"""DELETE FROM "Characters" WHERE "Id" = {character.Id}""", TestContext.Current.CancellationToken);

        // Assert
        var left = await _fixture.DbContext.CharacterDrafts.AsNoTracking()
            .CountAsync(d => d.CreatedByUserId == userId, TestContext.Current.CancellationToken);
        left.Should().Be(0);
    }

    /// <summary>
    /// A transaction that is rolled back when disposed, whose character keys to the race, class
    /// and background wait for a commit that never comes, so a character needs no reference data.
    /// </summary>
    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginWithoutReferenceDataAsync()
    {
        var transaction = await _fixture.DbContext.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await _fixture.DbContext.Database.ExecuteSqlRawAsync("SET CONSTRAINTS ALL DEFERRED", TestContext.Current.CancellationToken);
        return transaction;
    }

    private async Task<Character> AddCharacterAsync(Guid userId)
    {
        var character = Character.Create(
            userId, "Gandalf", 10, RulesEdition.Edition2014, "elf", null, "wizard", null, "sage",
            new Dictionary<string, int> { ["STR"] = 8, ["DEX"] = 14, ["CON"] = 13, ["INT"] = 18, ["WIS"] = 15, ["CHA"] = 12 },
            _timeProvider);
        _fixture.DbContext.Characters.Add(character);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return character;
    }

    private async Task<CharacterDraft> AddBuildAsync(Guid userId, Character character)
    {
        var build = CharacterDraft.CreateEmpty(userId, _timeProvider);
        build.SetNameAndLevel(character.Name, character.Level);
        build.MarkAsCompleted(character.Id);
        await _repository.CreateAsync(build, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return build;
    }

    [Fact]
    public async Task Repository_IsolatesDataBetweenUsers()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        var draft1 = CharacterDraft.CreateEmpty(user1Id, _timeProvider);
        var draft2 = CharacterDraft.CreateEmpty(user2Id, _timeProvider);

        await _repository.CreateAsync(draft1, TestContext.Current.CancellationToken);
        await _repository.CreateAsync(draft2, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - User1 tries to access User2's draft
        var result = await _repository.GetByIdForUpdateAsync(draft2.Id, user1Id, TestContext.Current.CancellationToken);

        // Assert - Should not be able to access
        result.Should().BeNull();
    }
}