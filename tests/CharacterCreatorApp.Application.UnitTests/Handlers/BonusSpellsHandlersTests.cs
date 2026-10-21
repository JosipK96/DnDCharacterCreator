using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.ChooseBonusSpells;
using CharacterCreatorApp.Application.Features.CharacterCreation.GetBonusSpells;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.DomainModels.Spells;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

/// <summary>
/// The spells step's bonus section: a 2014 high elf with the Acolyte background, whose feat is
/// Magic Initiate with any of the cleric or wizard lists (the background fixes none here).
/// </summary>
public class BonusSpellsHandlersTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IRaceRepository> _raceRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IBackgroundRepository> _backgroundRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFeatRepository> _featRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ISpellRepository> _spellRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _draftId = Guid.NewGuid();
    private readonly CharacterDraft _draft;

    public BonusSpellsHandlersTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        _draft = CharacterDraft.CreateEmpty(_userId, new FakeTimeProvider());
        _draft.ChooseEdition(RulesEdition.Edition2014);
        _draft.SetRace("elf", "high_elf");
        _draft.SetBackground("acolyte");

        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(_draft);
        _draftRepositoryMock.Setup(r => r.GetByIdAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(_draft);
        _raceRepositoryMock.Setup(r => r.GetByIdWithSubracesAsync("elf", _cts.Token)).ReturnsAsync(HighElf());
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("acolyte", _cts.Token)).ReturnsAsync(Acolyte());
        _featRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "magic_initiate" })), _cts.Token))
            .ReturnsAsync([MagicInitiate()]);
        _spellRepositoryMock.Setup(r => r.GetByClassIdAsync("wizard", _cts.Token)).ReturnsAsync(
            [Spell("fire_bolt", 0), Spell("mage_hand", 0), Spell("shield", 1), Spell("toll_the_dead", 0, "XGE"), Spell("misty_step", 2)]);
        _spellRepositoryMock.Setup(r => r.GetByClassIdAsync("cleric", _cts.Token)).ReturnsAsync(
            [Spell("light", 0), Spell("guidance", 0), Spell("bless", 1)]);
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ChooseBonusSpells_WithEveryPickRight_SavesThem()
    {
        // Arrange
        var pick = new FeatSpellPick { FeatId = "magic_initiate", ClassId = "cleric", SpellIds = ["light", "guidance", "bless"] };

        // Act
        var result = await ChooseHandler().Handle(new ChooseBonusSpellsCommand(_draftId, ["fire_bolt"], [pick]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _draft.RaceSpellIds.Should().Equal("fire_bolt");
        _draft.FeatSpells.Should().BeEquivalentTo([pick]);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task ChooseBonusSpells_WithAWrongPick_RefusesItAndSavesNothing()
    {
        // Arrange - a wizard spell for the cleric list
        var pick = new FeatSpellPick { FeatId = "magic_initiate", ClassId = "cleric", SpellIds = ["light", "guidance", "shield"] };

        // Act
        var result = await ChooseHandler().Handle(new ChooseBonusSpellsCommand(_draftId, ["fire_bolt"], [pick]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.BonusSpellNotOnList");
        _draft.FeatSpells.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChooseBonusSpells_ForADraftNotFound_ReturnsNotFound()
    {
        // Arrange
        var otherId = Guid.NewGuid();
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(otherId, _userId, _cts.Token)).ReturnsAsync((CharacterDraft?)null);

        // Act
        var result = await ChooseHandler().Handle(new ChooseBonusSpellsCommand(otherId, [], []), _cts.Token);

        // Assert
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.NotFound");
    }

    [Fact]
    public async Task GetBonusSpells_ListsTheRacesPickFirstThenTheFeats_WithTheirListsNamed()
    {
        // Arrange
        ArrangeClassNames();

        // Act
        var result = await QueryHandler().Handle(new GetBonusSpellsQuery(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Select(c => (c.FeatId, c.Name, c.Cantrips, c.FirstLevelSpells)).Should().Equal(
            (null, "High Elf", 1, 0),
            ("magic_initiate", "Magic Initiate", 2, 1));
        result.Value[1].Lists.Select(l => (l.ClassId, l.ClassName)).Should().Equal(("cleric", "Cleric"), ("wizard", "Wizard"));
        result.Value[1].AbilityOptions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBonusSpells_ForAFeatWithASpellcastingAbilityToPick_ListsTheAbilities()
    {
        // Arrange - the 2024 Magic Initiate's Intelligence, Wisdom or Charisma
        ArrangeClassNames();
        var magicInitiate = MagicInitiate();
        magicInitiate.SpellChoice!.AbilityOptions = ["INT", "WIS", "CHA"];
        _featRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "magic_initiate" })), _cts.Token))
            .ReturnsAsync([magicInitiate]);

        // Act
        var result = await QueryHandler().Handle(new GetBonusSpellsQuery(_draftId), _cts.Token);

        // Assert
        result.Value[1].AbilityOptions.Should().Equal("INT", "WIS", "CHA");
    }

    [Fact]
    public async Task GetBonusSpells_OffersOnlyTheLevelsCountedFromTheAllowedBooks()
    {
        // Arrange - the high elf picks a cantrip only; Toll the Dead is Xanathar's
        ArrangeClassNames();

        // Act
        var result = await QueryHandler().Handle(new GetBonusSpellsQuery(_draftId), _cts.Token);

        // Assert
        result.Value[0].Lists.Single().Spells.Select(s => s.Id).Should().Equal("fire_bolt", "mage_hand");
        result.Value[1].Lists[1].Spells.Select(s => s.Id).Should().Equal("fire_bolt", "mage_hand", "shield");
    }

    [Fact]
    public async Task GetBonusSpells_WithNothingToPick_ReturnsAnEmptyList()
    {
        // Arrange - a dwarf with a background without a feat
        _draft.SetRace("dwarf");
        _draft.SetBackground("soldier");
        _raceRepositoryMock.Setup(r => r.GetByIdWithSubracesAsync("dwarf", _cts.Token)).ReturnsAsync(
            new Race { Id = "dwarf", Name = "Dwarf", Edition = RulesEdition.Edition2014, Source = "PHB", Size = "Medium" });
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("soldier", _cts.Token)).ReturnsAsync(
            new Background { Id = "soldier", Name = "Soldier", Edition = RulesEdition.Edition2014, Source = "PHB" });
        _featRepositoryMock.Setup(r => r.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => !ids.Any()), _cts.Token)).ReturnsAsync([]);

        // Act
        var result = await QueryHandler().Handle(new GetBonusSpellsQuery(_draftId), _cts.Token);

        // Assert
        result.Value.Should().BeEmpty();
    }

    private ChooseBonusSpellsCommandHandler ChooseHandler() => new(
        _draftRepositoryMock.Object,
        _raceRepositoryMock.Object,
        _backgroundRepositoryMock.Object,
        _featRepositoryMock.Object,
        _spellRepositoryMock.Object,
        _userContextMock.Object,
        _unitOfWorkMock.Object);

    private GetBonusSpellsQueryHandler QueryHandler() => new(
        _draftRepositoryMock.Object,
        _raceRepositoryMock.Object,
        _backgroundRepositoryMock.Object,
        _featRepositoryMock.Object,
        _spellRepositoryMock.Object,
        _classRepositoryMock.Object,
        _userContextMock.Object);

    private void ArrangeClassNames() =>
        _classRepositoryMock.Setup(r => r.GetClassesAsync(RulesEdition.Edition2014, _cts.Token)).ReturnsAsync(
        [
            new ClassReadModel { Id = "cleric", Name = "Cleric", Edition = RulesEdition.Edition2014, HitDie = "1d8" },
            new ClassReadModel { Id = "wizard", Name = "Wizard", Edition = RulesEdition.Edition2014, HitDie = "1d6" },
        ]);

    private static Race HighElf() => new()
    {
        Id = "elf",
        Name = "Elf",
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        Size = "Medium",
        Subraces =
        [
            new Subrace
            {
                Id = "high_elf",
                Name = "High Elf",
                RaceId = "elf",
                Source = "PHB",
                SpellChoice = new BonusSpellChoice { ClassIds = ["wizard"], Cantrips = 1 },
            },
        ],
    };

    private static Background Acolyte() => new()
    {
        Id = "acolyte",
        Name = "Acolyte",
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        FeatId = "magic_initiate",
    };

    private static FeatReadModel MagicInitiate() => new()
    {
        Id = "magic_initiate",
        Name = "Magic Initiate",
        Description = "A feat.",
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        SpellChoice = new BonusSpellChoice { ClassIds = ["cleric", "wizard"], Cantrips = 2, FirstLevelSpells = 1 },
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
}
