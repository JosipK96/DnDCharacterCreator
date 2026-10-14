using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.FinalizeCharacter;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Feats;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.DomainModels.Spells;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

/// <summary>
/// Finalize with a choice from issue #7 left open, and what each one gives the character: the
/// size, a background's picked feat, the spells a race or feat lets the player pick, and
/// Resilient's saving throw. A 2014 dwarf fighter soldier, changed one choice at a time.
/// </summary>
public class FinalizeFeatAndRaceChoicesTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ICharacterRepository> _characterRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IRaceRepository> _raceRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IBackgroundRepository> _backgroundRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ILanguageRepository> _languageRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFeatRepository> _featRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ISpellRepository> _spellRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero));
    private readonly CancellationTokenSource _cts = new();
    private readonly FinalizeCharacterCommandHandler _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _draftId = Guid.NewGuid();

    private Character? _createdCharacter;

    public FinalizeFeatAndRaceChoicesTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);
        _languageRepositoryMock.Setup(r => r.GetAllForEditionAsync(RulesEdition.Edition2014, _cts.Token)).ReturnsAsync([]);
        _featRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token)).ReturnsAsync([]);
        _characterRepositoryMock
            .Setup(r => r.Create(It.IsAny<Character>(), _cts.Token))
            .Callback<Character, CancellationToken>((character, _) => _createdCharacter = character)
            .Returns(Task.CompletedTask);

        _sut = new FinalizeCharacterCommandHandler(
            _draftRepositoryMock.Object,
            _characterRepositoryMock.Object,
            _raceRepositoryMock.Object,
            _classRepositoryMock.Object,
            _backgroundRepositoryMock.Object,
            _languageRepositoryMock.Object,
            _featRepositoryMock.Object,
            _spellRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _userContextMock.Object,
            _timeProvider);
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    // --- Size ---

    [Fact]
    public async Task Handle_WithASmallOrMediumRaceAndNoSize_ReturnsSizeRequired()
    {
        // Arrange
        var draft = Draft();

        // Act
        var result = await Finalize(draft, race: Dwarf(size: "Small or Medium"));

        // Assert
        AssertRefused(result, "Character.SizeRequired");
    }

    [Fact]
    public async Task Handle_WithASmallOrMediumRaceAndASize_GivesTheCharacterThatSize()
    {
        // Arrange
        var draft = Draft();
        draft.SetRace("dwarf", null, null, "Small");

        // Act
        var created = await FinalizeSuccessfully(draft, race: Dwarf(size: "Small or Medium"));

        // Assert
        created.Size.Should().Be("Small");
    }

    [Fact]
    public async Task Handle_WithARaceOfOneSize_GivesTheCharacterTheRacesSize()
    {
        // Act
        var created = await FinalizeSuccessfully(Draft());

        // Assert
        created.Size.Should().Be("Medium");
    }

    // --- A background's picked feat ---

    [Fact]
    public async Task Handle_WithABackgroundWhoseFeatIsPickedAndNoneIs_ReturnsBackgroundFeatRequired()
    {
        // Act
        var result = await Finalize(Draft(), background: MistWanderer());

        // Assert
        AssertRefused(result, "Character.BackgroundFeatRequired");
    }

    [Fact]
    public async Task Handle_WithABackgroundFeatOfAnotherCategory_ReturnsBackgroundFeatNotOfCategory()
    {
        // Arrange
        var draft = Draft();
        draft.SetBackground("mist_wanderer", null, "alert");
        ArrangeFeats(Feat("alert", "Alert", category: "General"));

        // Act
        var result = await Finalize(draft, background: MistWanderer());

        // Assert
        AssertRefused(result, "CharacterDraft.BackgroundFeatNotOfCategory");
    }

    [Fact]
    public async Task Handle_WithABackgroundFeatFromABookNotAllowed_ReturnsSourceNotAllowed()
    {
        // Arrange
        var draft = Draft();
        draft.SetBackground("mist_wanderer", null, "mist_walker");
        ArrangeFeats(Feat("mist_walker", "Mist Walker", category: "Dark Gift", source: "RHW"));

        // Act
        var result = await Finalize(draft, background: MistWanderer());

        // Assert
        AssertRefused(result, "CharacterDraft.SourceNotAllowed");
    }

    [Fact]
    public async Task Handle_WithABackgroundFeatPicked_GivesTheCharacterTheFeat()
    {
        // Arrange
        var draft = Draft();
        draft.SetBackground("mist_wanderer", null, "mist_walker");
        ArrangeFeats(Feat("mist_walker", "Mist Walker", category: "Dark Gift"));

        // Act
        var created = await FinalizeSuccessfully(draft, background: MistWanderer());

        // Assert
        created.FeatIds.Should().Equal("mist_walker");
    }

    // --- The race's spells ---

    [Fact]
    public async Task Handle_WithTheHighElfsCantripNotPicked_ReturnsBonusSpellCount()
    {
        // Arrange
        var draft = Draft();
        draft.SetRace("elf", "high_elf");
        ArrangeSpellList("wizard", Spell("fire_bolt", 0));

        // Act
        var result = await Finalize(draft, race: HighElf());

        // Assert
        AssertRefused(result, "CharacterDraft.BonusSpellCount");
    }

    [Fact]
    public async Task Handle_WithTheHighElfsCantripPicked_GivesTheCharacterTheCantrip()
    {
        // Arrange
        var draft = Draft();
        draft.SetRace("elf", "high_elf");
        draft.ChooseBonusSpells(["fire_bolt"], []);
        ArrangeSpellList("wizard", Spell("fire_bolt", 0));

        // Act
        var created = await FinalizeSuccessfully(draft, race: HighElf());

        // Assert
        created.SpellIds.Should().Equal("fire_bolt");
    }

    // --- A feat's spells ---

    [Fact]
    public async Task Handle_WithMagicInitiatesSpellsNotPicked_ReturnsBonusSpellPicksMismatch()
    {
        // Arrange
        var draft = DraftWithFeat("magic_initiate");
        ArrangeFeats(MagicInitiate());
        ArrangeMagicInitiateLists();

        // Act
        var result = await Finalize(draft);

        // Assert
        AssertRefused(result, "CharacterDraft.BonusSpellPicksMismatch");
    }

    [Fact]
    public async Task Handle_WithMagicInitiatesSpellsPicked_GivesTheCharacterTheSpells()
    {
        // Arrange
        var draft = DraftWithFeat("magic_initiate");
        draft.ChooseBonusSpells([], [new FeatSpellPick { FeatId = "magic_initiate", ClassId = "wizard", SpellIds = ["fire_bolt", "mage_hand", "shield"] }]);
        ArrangeFeats(MagicInitiate());
        ArrangeMagicInitiateLists();

        // Act
        var created = await FinalizeSuccessfully(draft);

        // Assert
        created.SpellIds.Should().BeEquivalentTo(["fire_bolt", "mage_hand", "shield"]);
    }

    [Fact]
    public async Task Handle_WithABackgroundsMagicInitiateFromAnotherList_ReturnsBonusSpellListNotOffered()
    {
        // Arrange - an Acolyte's Magic Initiate is the cleric's
        var draft = Draft();
        draft.SetBackground("acolyte");
        draft.ChooseBonusSpells([], [new FeatSpellPick { FeatId = "magic_initiate", ClassId = "wizard", SpellIds = ["fire_bolt", "mage_hand", "shield"] }]);
        ArrangeFeats(MagicInitiate());
        ArrangeSpellList("cleric", Spell("light", 0), Spell("guidance", 0), Spell("bless", 1));

        var acolyte = Soldier();
        acolyte.Id = "acolyte";
        acolyte.Name = "Acolyte";
        acolyte.FeatId = "magic_initiate";
        acolyte.FeatSpellListClassId = "cleric";

        // Act
        var result = await Finalize(draft, background: acolyte);

        // Assert
        AssertRefused(result, "CharacterDraft.BonusSpellListNotOffered");
    }

    // --- Resilient ---

    [Fact]
    public async Task Handle_WithResilient_GivesTheSavingThrowOfTheAbilityItRaised()
    {
        // Arrange
        var draft = DraftWithFeat("resilient", new Dictionary<string, int> { ["WIS"] = 1 });
        ArrangeFeats(Resilient());

        // Act
        var created = await FinalizeSuccessfully(draft);

        // Assert
        created.FeatSavingThrowAbilityIds.Should().Equal("WIS");
        created.AbilityScores["WIS"].Should().Be(11);
    }

    [Fact]
    public async Task Handle_WithAFeatThatAddsNoSave_GivesNoFeatSavingThrow()
    {
        // Arrange
        var draft = DraftWithFeat("alert");
        ArrangeFeats(Feat("alert", "Alert"));

        // Act
        var created = await FinalizeSuccessfully(draft);

        // Assert
        created.FeatSavingThrowAbilityIds.Should().BeEmpty();
    }

    private void AssertRefused(Response<Guid> result, string code)
    {
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be(code);
        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private Task<Response<Guid>> Finalize(CharacterDraft draft, Race? race = null, Background? background = null)
    {
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.RaceId!, _cts.Token)).ReturnsAsync(race ?? Dwarf());
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.ClassId!, _cts.Token)).ReturnsAsync(Fighter());
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.BackgroundId!, _cts.Token)).ReturnsAsync(background ?? Soldier());

        return _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);
    }

    private async Task<Character> FinalizeSuccessfully(CharacterDraft draft, Race? race = null, Background? background = null)
    {
        var result = await Finalize(draft, race, background);

        result.IsSuccess.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.Description)));
        return _createdCharacter!;
    }

    private void ArrangeFeats(params FeatReadModel[] feats) =>
        _featRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token)).ReturnsAsync([.. feats]);

    private void ArrangeSpellList(string classId, params SpellReadModel[] spells) =>
        _spellRepositoryMock.Setup(r => r.GetByClassIdAsync(classId, _cts.Token)).ReturnsAsync([.. spells]);

    private void ArrangeMagicInitiateLists()
    {
        ArrangeSpellList("cleric", Spell("light", 0), Spell("guidance", 0), Spell("bless", 1));
        ArrangeSpellList("wizard", Spell("fire_bolt", 0), Spell("mage_hand", 0), Spell("shield", 1));
    }

    // A level 1 dwarf fighter soldier with every other choice made
    private CharacterDraft Draft(int level = 1)
    {
        var draft = CharacterDraft.CreateEmpty(_userId, _timeProvider);
        draft.ChooseEdition(RulesEdition.Edition2014);
        draft.SetNameAndLevel("Thorin", level);
        draft.SetRace("dwarf");
        draft.SetClass("fighter");
        draft.SetBackground("soldier");
        draft.SetBaseAbilityScores(new Dictionary<string, int>
        {
            ["STR"] = 15,
            ["DEX"] = 14,
            ["CON"] = 13,
            ["INT"] = 12,
            ["WIS"] = 10,
            ["CHA"] = 8,
        }, AbilityScoreMethod.Rolled);
        return draft;
    }

    // At level 4 the fighter's one improvement, spent on this feat
    private CharacterDraft DraftWithFeat(string featId, Dictionary<string, int>? increase = null)
    {
        var draft = Draft(level: 4);
        draft.SetClass("fighter", "champion");
        draft.SetFeats([featId], [increase ?? []]);
        return draft;
    }

    private static Race Dwarf(string size = "Medium") => new()
    {
        Id = "dwarf",
        Name = "Dwarf",
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        Size = size,
        Speed = 25,
    };

    private static Race HighElf()
    {
        var elf = Dwarf();
        elf.Id = "elf";
        elf.Name = "Elf";
        elf.Speed = 30;
        elf.Subraces =
        [
            new Subrace
            {
                Id = "high_elf",
                Name = "High Elf",
                RaceId = "elf",
                Source = "PHB",
                SpellChoice = new BonusSpellChoice { ClassIds = ["wizard"], Cantrips = 1 },
            },
        ];
        return elf;
    }

    private static Class Fighter() => new()
    {
        Id = "fighter",
        Name = "Fighter",
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        HitDie = "1d10",
        SubclassLevel = 3,
        AsiLevels = [4, 6, 8, 12, 14, 16, 19],
        SavingThrowAbilityIds = ["STR", "CON"],
        Subclasses = [new() { Id = "champion", ClassId = "fighter", Name = "Champion", Source = "PHB", Features = [] }],
    };

    private static Background Soldier() => new()
    {
        Id = "soldier",
        Name = "Soldier",
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
    };

    private static Background MistWanderer()
    {
        var background = Soldier();
        background.Id = "mist_wanderer";
        background.Name = "Mist Wanderer";
        background.FeatChoiceCategory = "Dark Gift";
        return background;
    }

    private static FeatReadModel MagicInitiate()
    {
        var feat = Feat("magic_initiate", "Magic Initiate");
        feat.SpellChoice = new BonusSpellChoice { ClassIds = ["cleric", "wizard"], Cantrips = 2, FirstLevelSpells = 1 };
        return feat;
    }

    private static FeatReadModel Resilient()
    {
        var feat = Feat("resilient", "Resilient");
        feat.AbilityIncrease = new FeatAbilityIncrease { Options = [.. AbilityScore.All.Select(a => a.Id)] };
        feat.Choice = new FeatChoiceReadModel { Type = FeatChoice.SavingThrow, Count = 1, Options = [.. AbilityScore.All.Select(a => a.Id)] };
        return feat;
    }

    private static FeatReadModel Feat(string id, string name, string? category = null, string source = "PHB") => new()
    {
        Id = id,
        Name = name,
        Description = "A feat.",
        Edition = RulesEdition.Edition2014,
        Source = source,
        Category = category,
    };

    private static SpellReadModel Spell(string id, int level) => new()
    {
        Id = id,
        Name = id,
        Source = "PHB",
        Level = level,
        School = "Evocation",
        CastingTimeUnit = "action",
        RangeType = "point",
        DurationType = "instant",
        Description = "A spell.",
    };
}
