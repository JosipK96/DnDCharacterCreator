using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.GetSpellAllowance;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels.Books;
using CharacterCreatorApp.Application.ReadModels.Spells;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.DomainModels.Spells;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class GetSpellAllowanceQueryHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IRaceRepository> _raceRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IBackgroundRepository> _backgroundRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ILanguageRepository> _languageRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFeatRepository> _featRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ISpellRepository> _spellRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IBookRepository> _bookRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly GetSpellAllowanceQueryHandler _sut;

    private readonly Guid _userId = new("00000000-0000-0000-0000-000000000001");
    private readonly Guid _draftId = new("00000000-0000-0000-0000-000000000002");

    public GetSpellAllowanceQueryHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);
        _featRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token)).ReturnsAsync([]);
        _languageRepositoryMock.Setup(r => r.GetAllForEditionAsync(RulesEdition.Edition2014, _cts.Token)).ReturnsAsync([]);

        // The 2014 books, the Player's Handbook first, as the repository orders them
        _bookRepositoryMock.Setup(r => r.GetAllForEditionAsync(RulesEdition.Edition2014, _cts.Token)).ReturnsAsync(() =>
        [
            Book("PHB", "Player's Handbook (2014)", isPlayersHandbook: true),
            Book("TCE", "Tasha's Cauldron of Everything"),
            Book("XGE", "Xanathar's Guide to Everything"),
        ]);

        _sut = new GetSpellAllowanceQueryHandler(
            _draftRepositoryMock.Object,
            _classRepositoryMock.Object,
            _raceRepositoryMock.Object,
            _backgroundRepositoryMock.Object,
            _languageRepositoryMock.Object,
            _featRepositoryMock.Object,
            _spellRepositoryMock.Object,
            _bookRepositoryMock.Object,
            _userContextMock.Object,
            new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
    }

    [Fact]
    public async Task Handle_ForAClassWhoseTableGivesTheCount_ReturnsTheTableRow()
    {
        // Arrange - a level 1 sorcerer knows four cantrips and two spells
        ArrangeDraft("sorcerer", level: 1, withScores: false);
        ArrangeClass("sorcerer", Row("sorcerer", 1, cantrips: 4, spells: 2), spellcasting: null);

        // Act
        var result = await _sut.Handle(new GetSpellAllowanceQuery(_draftId), _cts.Token);

        // Assert
        result.Value.Should().BeEquivalentTo(new
        {
            CastsSpells = true,
            Cantrips = 4,
            Spells = 2,
            MaxSpellLevel = 1,
            SpellsFollowScores = false,
            AlwaysPreparedSpellIds = new List<string>(),
        });
    }

    [Theory]
    [InlineData("cleric", "Wisdom", 1, 1, 15, 3)]
    [InlineData("druid", "Wisdom", 1, 2, 16, 5)]
    [InlineData("paladin", "Charisma", 2, 2, 14, 3)]
    [InlineData("artificer", "Intelligence", 2, 1, 17, 3)]
    public async Task Handle_ForA2014PreparedCaster_ReturnsItsModifierPlusItsLevelOrHalfOfIt(
        string classId, string ability, int divisor, int level, int score, int prepared)
    {
        // Arrange - the dwarf adds 1 to the ability, which the count follows
        ArrangeDraft(classId, level, withScores: true, ability, score - 1);
        ArrangeRaceAndBackground(ability, raceBonus: 1);
        ArrangeClass(classId, Row(classId, level, cantrips: 2, spells: 0), Rule(classId, ability, divisor));

        // Act
        var result = await _sut.Handle(new GetSpellAllowanceQuery(_draftId), _cts.Token);

        // Assert
        result.Value.Spells.Should().Be(prepared);
        result.Value.SpellsFollowScores.Should().BeTrue();
        result.Value.Cantrips.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ForALifeCleric_ListsItsDomainSpellsAsAlreadyPrepared()
    {
        // Arrange - Bless and Cure Wounds at level 1, Lesser Restoration only from level 3
        ArrangeDraft("cleric", level: 1, withScores: true, "Wisdom", 14, subclassId: "life");
        ArrangeRaceAndBackground("Wisdom", raceBonus: 0);
        ArrangeClass("cleric", Row("cleric", 1, cantrips: 3, spells: 0), Rule("cleric", "Wisdom", 1), new Subclass
        {
            Id = "life",
            Name = "Life Domain",
            Source = "PHB",
            ClassId = "cleric",
            Spells =
            [
                new SpellSubclass { SpellId = "bless", SubclassId = "life", Level = 1, IsAlwaysPrepared = true },
                new SpellSubclass { SpellId = "cure_wounds", SubclassId = "life", Level = 1, IsAlwaysPrepared = true },
                new SpellSubclass { SpellId = "lesser_restoration", SubclassId = "life", Level = 3, IsAlwaysPrepared = true },
            ]
        });
        _spellRepositoryMock
            .Setup(r => r.GetByIdsAsync(new List<string> { "bless", "cure_wounds" }, _cts.Token))
            .ReturnsAsync([Spell("cure_wounds", 1), Spell("bless", 1)]);

        // Act
        var result = await _sut.Handle(new GetSpellAllowanceQuery(_draftId), _cts.Token);

        // Assert - the class list here holds neither, so they are loaded to be named in the list
        result.Value.AlwaysPreparedSpellIds.Should().Equal("bless", "cure_wounds");
        result.Value.Spells.Should().Be(3);
        result.Value.SpellList.Select(s => s.Id).Should().Equal("bless", "cure_wounds");
    }

    [Fact]
    public async Task Handle_ForACaster_ListsItsClassSpellsFromItsBooksUpToItsHighestLevelByLevelThenName()
    {
        // Arrange - a level 1 sorcerer of the PHB alone, picking up to level 1
        ArrangeDraft("sorcerer", level: 1, withScores: false);
        ArrangeClass("sorcerer", Row("sorcerer", 1, cantrips: 4, spells: 2), spellcasting: null);
        _spellRepositoryMock.Setup(r => r.GetByClassIdAsync("sorcerer", _cts.Token)).ReturnsAsync(
        [
            Spell("shield", 1), Spell("web", 2), Spell("ice_knife", 1, "XGE"), Spell("fire_bolt", 0), Spell("burning_hands", 1),
        ]);

        // Act
        var result = await _sut.Handle(new GetSpellAllowanceQuery(_draftId), _cts.Token);

        // Assert
        result.Value.SpellList.Select(s => s.Id).Should().Equal("fire_bolt", "burning_hands", "shield");
    }

    [Fact]
    public async Task Handle_ForACaster_CountsWhatItsUntickedBooksHoldUpToItsHighestLevel()
    {
        // Arrange - a level 1 sorcerer of the PHB alone: Ice Knife (XGE) would be offered with
        // Xanathar's ticked, its level 2 spell would not be at all
        ArrangeDraft("sorcerer", level: 1, withScores: false);
        ArrangeClass("sorcerer", Row("sorcerer", 1, cantrips: 4, spells: 2), spellcasting: null);
        _spellRepositoryMock.Setup(r => r.GetByClassIdAsync("sorcerer", _cts.Token)).ReturnsAsync(
        [
            Spell("shield", 1), Spell("ice_knife", 1, "XGE"), Spell("xge_cantrip", 0, "XGE"), Spell("xge_high", 2, "XGE"), Spell("tce_spell", 1, "TCE"),
        ]);

        // Act
        var result = await _sut.Handle(new GetSpellAllowanceQuery(_draftId), _cts.Token);

        // Assert
        result.Value.HiddenSpells.Count.Should().Be(3);
        Books(result.Value).Should().Equal(("TCE", "Tasha's Cauldron of Everything", 1), ("XGE", "Xanathar's Guide to Everything", 2));
    }

    [Fact]
    public async Task Handle_ForASubclassThatOpensAnotherList_ListsAndCountsEverySpellOnce()
    {
        // Arrange - an Eldritch Knight-like subclass opens the wizard's list and adds spells of its
        // own; Shield is on both and counts once, and Absorb Elements once, though two lists hold it
        ArrangeDraft("fighter", level: 1, withScores: false, subclassId: "knight");
        ArrangeClass("fighter", Row("fighter", 1, cantrips: 2, spells: 3), spellcasting: null, new Subclass
        {
            Id = "knight",
            Name = "Knight",
            Source = "PHB",
            ClassId = "fighter",
            SpellListClassId = "wizard",
            Spells = [new SpellSubclass { SpellId = "absorb_elements", SubclassId = "knight", Level = 1 }],
        });
        _spellRepositoryMock.Setup(r => r.GetByClassIdAsync("wizard", _cts.Token)).ReturnsAsync(
            [Spell("shield", 1), Spell("magic_missile", 1), Spell("absorb_elements", 1, "XGE")]);
        _spellRepositoryMock.Setup(r => r.GetByIdsAsync(new List<string> { "absorb_elements" }, _cts.Token)).ReturnsAsync(
            [Spell("absorb_elements", 1, "XGE")]);

        // Act
        var result = await _sut.Handle(new GetSpellAllowanceQuery(_draftId), _cts.Token);

        // Assert
        result.Value.SpellList.Select(s => s.Id).Should().Equal("magic_missile", "shield");
        Books(result.Value).Should().Equal(("XGE", "Xanathar's Guide to Everything", 1));
    }

    [Fact]
    public async Task Handle_ForASubclassWithAnAlwaysPreparedSpellOfAnUntickedBook_ListsItAndDoesNotCountIt()
    {
        // Arrange - an oath's spell from Xanathar's is prepared whatever the books, so ticking
        // Xanathar's would not add it to the picks; the class's own Xanathar's spell would be
        ArrangeDraft("paladin", level: 3, withScores: true, "Charisma", 14, subclassId: "oath");
        ArrangeRaceAndBackground("Charisma", raceBonus: 0);
        ArrangeClass("paladin", Row("paladin", 3, cantrips: 0, spells: 0), Rule("paladin", "Charisma", 2), new Subclass
        {
            Id = "oath",
            Name = "Oath",
            Source = "PHB",
            ClassId = "paladin",
            Spells = [new SpellSubclass { SpellId = "ceremony", SubclassId = "oath", Level = 3, IsAlwaysPrepared = true }],
        });
        _spellRepositoryMock.Setup(r => r.GetByClassIdAsync("paladin", _cts.Token)).ReturnsAsync(
            [Spell("bless", 1), Spell("ceremony", 1, "XGE"), Spell("searing_smite", 1, "XGE")]);

        // Act
        var result = await _sut.Handle(new GetSpellAllowanceQuery(_draftId), _cts.Token);

        // Assert
        result.Value.AlwaysPreparedSpellIds.Should().Equal("ceremony");
        result.Value.SpellList.Select(s => s.Id).Should().Equal("bless", "ceremony");
        Books(result.Value).Should().Equal(("XGE", "Xanathar's Guide to Everything", 1));
    }

    [Fact]
    public async Task Handle_ForACharacterThatCastsNothingYet_CountsNothingHidden()
    {
        // Arrange - the 2014 paladin's table starts at level 2
        ArrangeDraft("paladin", level: 1, withScores: false);
        ArrangeClass("paladin", Row("paladin", 2, cantrips: 0, spells: 0), Rule("paladin", "Charisma", 2));

        // Act
        var result = await _sut.Handle(new GetSpellAllowanceQuery(_draftId), _cts.Token);

        // Assert
        result.Value.HiddenSpells.Count.Should().Be(0);
        result.Value.HiddenSpells.Books.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ForACharacterThatCastsNothingYet_SaysSo()
    {
        // Arrange - the 2014 paladin's table starts at level 2
        ArrangeDraft("paladin", level: 1, withScores: false);
        ArrangeClass("paladin", Row("paladin", 2, cantrips: 0, spells: 0), Rule("paladin", "Charisma", 2));

        // Act
        var result = await _sut.Handle(new GetSpellAllowanceQuery(_draftId), _cts.Token);

        // Assert
        result.Value.CastsSpells.Should().BeFalse();
        result.Value.Spells.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ForA2014PreparedCasterBeforeItsScoresAreChosen_ReturnsScoresRequired()
    {
        // Arrange
        ArrangeDraft("cleric", level: 1, withScores: false);
        ArrangeClass("cleric", Row("cleric", 1, cantrips: 3, spells: 0), Rule("cleric", "Wisdom", 1));

        // Act
        var result = await _sut.Handle(new GetSpellAllowanceQuery(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.ScoresRequired");
    }

    [Fact]
    public async Task Handle_WithAMissingDraft_ReturnsNotFound()
    {
        // Arrange
        _draftRepositoryMock
            .Setup(r => r.GetByIdAsync(_draftId, _userId, _cts.Token))
            .ReturnsAsync((CharacterDraft?)null);

        // Act
        var result = await _sut.Handle(new GetSpellAllowanceQuery(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.NotFound");
    }

    // A 2014 dwarf soldier of this class and level; with its scores chosen, everything but
    // the spellcasting ability is 10
    private void ArrangeDraft(string classId, int level, bool withScores, string ability = "Wisdom", int score = 10, string? subclassId = null)
    {
        var draft = CharacterDraft.CreateEmpty(_userId, new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
        draft.ChooseEdition(RulesEdition.Edition2014);
        draft.SetNameAndLevel("Elminster", level);
        draft.SetClass(classId, subclassId);

        if (withScores)
        {
            var scores = new Dictionary<string, int> { ["STR"] = 10, ["DEX"] = 10, ["CON"] = 10, ["INT"] = 10, ["WIS"] = 10, ["CHA"] = 10 };
            scores[AbilityScore.Normalize(ability)] = score;
            draft.SetRace("dwarf");
            draft.SetBackground("soldier");
            draft.SetBaseAbilityScores(scores, AbilityScoreMethod.Rolled);
        }

        _draftRepositoryMock.Setup(r => r.GetByIdAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
    }

    private void ArrangeRaceAndBackground(string ability, int raceBonus)
    {
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("dwarf", _cts.Token)).ReturnsAsync(new Race
        {
            Id = "dwarf",
            Name = "Dwarf",
            Edition = RulesEdition.Edition2014,
            Source = "PHB",
            Speed = 25,
            Size = "Medium",
            AbilityBonuses = [new RaceAbilityBonus { RaceId = "dwarf", AbilityScoreId = AbilityScore.Normalize(ability), Bonus = raceBonus }],
        });
        _backgroundRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync("soldier", _cts.Token))
            .ReturnsAsync(new Background { Id = "soldier", Name = "Soldier", Edition = RulesEdition.Edition2014, Source = "PHB", FeatureId = "military-rank" });
    }

    private void ArrangeClass(string classId, ClassSpellProgression row, Spellcasting? spellcasting, params Subclass[] subclasses)
    {
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(classId, _cts.Token)).ReturnsAsync(new Class
        {
            Id = classId,
            Name = classId,
            Edition = RulesEdition.Edition2014,
            Source = "PHB",
            HitDie = "1d8",
            SubclassLevel = 1,
            Spellcasting = spellcasting,
            SpellProgression = [row],
            Subclasses = [.. subclasses],
        });

        // DraftSourceLoader loads the class's list with the rest of a prepared caster's sources
        _spellRepositoryMock.Setup(r => r.GetByClassIdAsync(classId, _cts.Token)).ReturnsAsync([]);
    }

    private static Spellcasting Rule(string classId, string ability, int divisor) => new()
    {
        ClassId = classId, SpellcastingAbility = ability, IsPreparedCaster = true, PreparedSpellsLevelDivisor = divisor
    };

    private static SpellReadModel Spell(string id, int level, string source = "PHB") => new()
    {
        Id = id,
        Name = id,
        Source = source,
        Level = level,
        School = "Evocation",
        CastingTimeUnit = "action",
        RangeType = "point",
        DurationType = "instant",
        Description = "A spell.",
    };

    private static List<(string Code, string Name, int Count)> Books(SpellAllowanceReadModel allowance) =>
        allowance.HiddenSpells.Books.ConvertAll(b => (b.Code, b.Name, b.Count));

    private static BookReadModel Book(string code, string name, bool isPlayersHandbook = false) =>
        new() { Code = code, Name = name, Edition = RulesEdition.Edition2014, IsPlayersHandbook = isPlayersHandbook };

    private static ClassSpellProgression Row(string classId, int level, int cantrips, int spells) => new()
    {
        ClassId = classId, CharacterLevel = level, CantripsKnown = cantrips, SpellsKnown = spells, MaxSpellLevel = 1
    };

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
