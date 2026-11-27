using CharacterCreatorApp.Application.Features.CharacterCreation.Building;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.DomainModels.Spells;
using CharacterCreatorApp.Domain.Services;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Building;

public class BonusSpellcastingTests
{
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);

    public BonusSpellcastingTests()
    {
        _classRepositoryMock.Setup(r => r.GetByIdAsync("wizard", It.IsAny<CancellationToken>())).ReturnsAsync(Caster("wizard", "INT"));
        _classRepositoryMock.Setup(r => r.GetByIdAsync("cleric", It.IsAny<CancellationToken>())).ReturnsAsync(Caster("cleric", "WIS"));
    }

    [Fact]
    public async Task LoadAsync_AHighElf_CastsItsWizardCantripWithTheWizardsAbilityUnderTheSubracesName()
    {
        // Arrange
        var highElf = new Subrace
        {
            Id = "high_elf", Name = "High Elf", Source = "PHB", RaceId = "elf",
            SpellChoice = new BonusSpellChoice { ClassIds = ["wizard"], Cantrips = 1 },
        };

        // Act
        var sources = await BonusSpellcasting.LoadAsync(Elf(highElf), highElf, [], [], _classRepositoryMock.Object, CancellationToken.None);

        // Assert
        sources.Should().Equal(new SpellcastingSource("High Elf", "INT"));
    }

    [Fact]
    public async Task LoadAsync_A2014MagicInitiate_CastsWithTheAbilityOfTheListsClass()
    {
        // Arrange
        var pick = new FeatSpellPick { FeatId = "magic_initiate", ClassId = "cleric", SpellIds = ["light"] };

        // Act
        var sources = await BonusSpellcasting.LoadAsync(
            Elf(), null, [pick], [Feat("magic_initiate", "Magic Initiate")], _classRepositoryMock.Object, CancellationToken.None);

        // Assert
        sources.Should().Equal(new SpellcastingSource("Magic Initiate", "WIS"));
    }

    [Fact]
    public async Task LoadAsync_A2024MagicInitiate_CastsWithTheAbilityPickedAndLoadsNoClass()
    {
        // Arrange - a wizard list cast with Charisma
        var pick = new FeatSpellPick { FeatId = "magic_initiate_2024", ClassId = "wizard-2024", SpellIds = ["fire_bolt"], AbilityId = "CHA" };

        // Act
        var sources = await BonusSpellcasting.LoadAsync(
            Elf(), null, [pick], [Feat("magic_initiate_2024", "Magic Initiate")], _classRepositoryMock.Object, CancellationToken.None);

        // Assert
        sources.Should().Equal(new SpellcastingSource("Magic Initiate", "CHA"));
        _classRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadAsync_NoRaceOrFeatSpells_ReturnsNone()
    {
        // Act
        var sources = await BonusSpellcasting.LoadAsync(Elf(), null, [], [], _classRepositoryMock.Object, CancellationToken.None);

        // Assert
        sources.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_AListWhoseClassIsGone_AddsNothingForIt()
    {
        // Arrange - a class the seed data no longer has
        _classRepositoryMock.Setup(r => r.GetByIdAsync("bard", It.IsAny<CancellationToken>())).ReturnsAsync((ClassReadModel?)null);
        var pick = new FeatSpellPick { FeatId = "magic_initiate", ClassId = "bard", SpellIds = ["vicious_mockery"] };

        // Act
        var sources = await BonusSpellcasting.LoadAsync(
            Elf(), null, [pick], [Feat("magic_initiate", "Magic Initiate")], _classRepositoryMock.Object, CancellationToken.None);

        // Assert
        sources.Should().BeEmpty();
    }

    private static Race Elf(Subrace? subrace = null) => new()
    {
        Id = "elf",
        Edition = RulesEdition.Edition2014,
        Name = "Elf",
        Source = "PHB",
        Size = "Medium",
        Speed = 30,
        Subraces = subrace == null ? [] : [subrace],
    };

    private static ClassReadModel Caster(string id, string ability) => new()
    {
        Id = id,
        Name = id,
        Edition = RulesEdition.Edition2014,
        HitDie = "1d8",
        Spellcasting = new SpellcastingReadModel { SpellcastingAbility = ability },
    };

    private static CharacterFeatReadModel Feat(string id, string name) => new()
    {
        Feat = new FeatReadModel { Id = id, Name = name, Description = string.Empty, Edition = RulesEdition.Edition2014 },
    };
}
