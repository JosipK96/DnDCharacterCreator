using CharacterCreatorApp.Application.Features.CharacterCreation.Shared.Finalizing;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels.Characters;
using CharacterCreatorApp.Application.ReadModels.Classes;
using CharacterCreatorApp.Application.ReadModels.Feats;
using CharacterCreatorApp.Application.ReadModels.Spells;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.DomainModels.Spells;
using CharacterCreatorApp.Domain.Services;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Shared;

public class BonusSpellcastingTests : IDisposable
{
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();

    public BonusSpellcastingTests()
    {
        _classRepositoryMock.Setup(r => r.GetByIdAsync("wizard", _cts.Token)).ReturnsAsync(Caster("wizard", "INT"));
        _classRepositoryMock.Setup(r => r.GetByIdAsync("cleric", _cts.Token)).ReturnsAsync(Caster("cleric", "WIS"));
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
        var sources = await BonusSpellcasting.LoadAsync(Elf(highElf), highElf, null, [], [], _classRepositoryMock.Object, _cts.Token);

        // Assert
        sources.Should().Equal(new SpellcastingSource("High Elf", "INT"));
    }

    [Fact]
    public async Task LoadAsync_ARaceCantripWhoseAbilityThePlayerPicked_CastsWithThePickAndLoadsNoClass()
    {
        // Arrange - a 2024 high elf picks Intelligence, Wisdom or Charisma
        var highElf = HighElf2024();

        // Act
        var sources = await BonusSpellcasting.LoadAsync(Elf(highElf), highElf, "WIS", [], [], _classRepositoryMock.Object, _cts.Token);

        // Assert
        sources.Should().Equal(new SpellcastingSource("High Elf", "WIS"));
        _classRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<string>(), _cts.Token), Times.Never);
    }

    [Fact]
    public async Task LoadAsync_ARaceCantripWhoseAbilityWasNeverPicked_AddsNothing()
    {
        // Arrange - a character made before the pick was kept: the wizard's Intelligence would be a guess
        var highElf = HighElf2024();

        // Act
        var sources = await BonusSpellcasting.LoadAsync(Elf(highElf), highElf, null, [], [], _classRepositoryMock.Object, _cts.Token);

        // Assert
        sources.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ATiefling_CastsItsRacesSpellsWithItsOwnAbilityUnderTheRacesName()
    {
        // Arrange - Thaumaturgy and Hellish Rebuke come with no pick, cast with Charisma
        var tiefling = new Race
        {
            Id = "tiefling", Name = "Tiefling", Edition = RulesEdition.Edition2014, Source = "PHB", Size = "Medium", SpellcastingAbility = "CHA",
        };

        // Act
        var sources = await BonusSpellcasting.LoadAsync(tiefling, null, null, [], [], _classRepositoryMock.Object, _cts.Token);

        // Assert
        sources.Should().Equal(new SpellcastingSource("Tiefling", "CHA"));
    }

    [Fact]
    public async Task LoadAsync_ADrow_CastsItsSubracesSpellsWithTheSubracesAbility()
    {
        // Arrange - Faerie Fire and Darkness, cast with Charisma
        var drow = new Subrace { Id = "drow", Name = "Drow", Source = "PHB", RaceId = "elf", SpellcastingAbility = "CHA" };

        // Act
        var sources = await BonusSpellcasting.LoadAsync(Elf(drow), drow, null, [], [], _classRepositoryMock.Object, _cts.Token);

        // Assert
        sources.Should().Equal(new SpellcastingSource("Drow", "CHA"));
    }

    [Fact]
    public async Task LoadAsync_A2014MagicInitiate_CastsWithTheAbilityOfTheListsClass()
    {
        // Arrange
        var pick = new FeatSpellPick { FeatId = "magic_initiate", ClassId = "cleric", SpellIds = ["light"] };

        // Act
        var sources = await BonusSpellcasting.LoadAsync(
            Elf(), null, null, [pick], [Feat("magic_initiate", "Magic Initiate")], _classRepositoryMock.Object, _cts.Token);

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
            Elf(), null, null, [pick], [Feat("magic_initiate_2024", "Magic Initiate")], _classRepositoryMock.Object, _cts.Token);

        // Assert
        sources.Should().Equal(new SpellcastingSource("Magic Initiate", "CHA"));
        _classRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<string>(), _cts.Token), Times.Never);
    }

    [Fact]
    public async Task LoadAsync_NoRaceOrFeatSpells_ReturnsNone()
    {
        // Act
        var sources = await BonusSpellcasting.LoadAsync(Elf(), null, null, [], [], _classRepositoryMock.Object, _cts.Token);

        // Assert
        sources.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_AListWhoseClassIsGone_AddsNothingForIt()
    {
        // Arrange - a class the seed data no longer has
        _classRepositoryMock.Setup(r => r.GetByIdAsync("bard", _cts.Token)).ReturnsAsync((ClassReadModel?)null);
        var pick = new FeatSpellPick { FeatId = "magic_initiate", ClassId = "bard", SpellIds = ["vicious_mockery"] };

        // Act
        var sources = await BonusSpellcasting.LoadAsync(
            Elf(), null, null, [pick], [Feat("magic_initiate", "Magic Initiate")], _classRepositoryMock.Object, _cts.Token);

        // Assert
        sources.Should().BeEmpty();
    }

    [Fact]
    public async Task AttackCantripsAsync_AWizardsFireBoltAndLight_ListsOnlyFireBoltCastWithIntelligence()
    {
        // Arrange - Light calls for a save but deals no damage
        var light = Cantrip("light", "Light", ["wizard"], damageType: null, dice: [], saveAbilityId: "DEX", attackType: null);

        // Act
        var cantrips = await AttackCantripsAsync([FireBolt(), light], Wizard());

        // Assert
        cantrips.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new AttackCantrip("Fire Bolt", "INT", "ranged", null, "fire", ["1d10", "2d10", "3d10", "4d10"]));
    }

    [Fact]
    public async Task AttackCantripsAsync_ALevelledSpell_IsNotListed()
    {
        // Arrange - its damage depends on the slot
        var guidingBolt = Cantrip("guiding_bolt", "Guiding Bolt", ["wizard"], dice: []);
        guidingBolt.Level = 1;

        // Act
        var cantrips = await AttackCantripsAsync([guidingBolt], Wizard());

        // Assert
        cantrips.Should().BeEmpty();
    }

    [Fact]
    public async Task AttackCantripsAsync_AFeatsPick_IsCastWithTheFeatsAbility()
    {
        // Arrange - a wizard who took the 2024 Magic Initiate's Fire Bolt with Charisma
        var pick = new FeatSpellPick { FeatId = "magic_initiate_2024", ClassId = "wizard-2024", SpellIds = ["fire_bolt"], AbilityId = "CHA" };

        // Act
        var cantrips = await AttackCantripsAsync([FireBolt()], Wizard(), featSpells: [pick]);

        // Assert
        cantrips.Should().ContainSingle().Which.AbilityId.Should().Be("CHA");
    }

    [Fact]
    public async Task AttackCantripsAsync_AHighElfFightersWizardCantrip_IsCastWithTheRacesAbility()
    {
        // Arrange - Fire Bolt is not on the fighter's list, so the race gave it
        var highElf = new Subrace
        {
            Id = "high_elf", Name = "High Elf", Source = "PHB", RaceId = "elf",
            SpellChoice = new BonusSpellChoice { ClassIds = ["wizard"], Cantrips = 1 },
        };

        // Act
        var cantrips = await AttackCantripsAsync([FireBolt()], Fighter(), race: Elf(highElf), subrace: highElf);

        // Assert
        cantrips.Should().ContainSingle().Which.AbilityId.Should().Be("INT");
    }

    [Fact]
    public async Task AttackCantripsAsync_ARacesOwnCantripOnTheClassesListToo_IsCastWithTheRacesAbility()
    {
        // Arrange - a fire genasi druid's Produce Flame comes with the race, cast with Constitution
        var genasi = new Race
        {
            Id = "genasi", Name = "Genasi", Edition = RulesEdition.Edition2014, Source = "EEPC", Size = "Medium", SpellcastingAbility = "CON",
            Spells = [new RaceSpell { RaceId = "genasi", SpellId = "produce_flame", Level = 1 }],
        };
        var druid = Wizard("druid", "WIS");

        // Act
        var cantrips = await AttackCantripsAsync([Cantrip("produce_flame", "Produce Flame", ["druid"])], druid, race: genasi);

        // Assert
        cantrips.Should().ContainSingle().Which.AbilityId.Should().Be("CON");
    }

    [Fact]
    public async Task AttackCantripsAsync_ASubclassCantripOffTheClassesList_IsCastWithTheClassesAbility()
    {
        // Arrange - a Celestial warlock's Sacred Flame, from its subclass
        var warlock = Wizard("warlock", "CHA");
        var celestial = new Subclass
        {
            Id = "celestial", Name = "The Celestial", ClassId = "warlock",
            Spells = [new SpellSubclass { SpellId = "sacred_flame", SubclassId = "celestial", Level = 1, IsAlwaysPrepared = true }],
        };
        var sacredFlame = Cantrip("sacred_flame", "Sacred Flame", ["cleric"], attackType: null, saveAbilityId: "DEX");

        // Act
        var cantrips = await AttackCantripsAsync([sacredFlame], warlock, subclass: celestial);

        // Assert
        cantrips.Should().ContainSingle().Which.AbilityId.Should().Be("CHA");
    }

    [Fact]
    public async Task AttackCantripsAsync_NoAbilityToCastWith_LeavesTheCantripOut()
    {
        // Arrange - a fighter with no race or feat spells
        // Act
        var cantrips = await AttackCantripsAsync([FireBolt()], Fighter());

        // Assert
        cantrips.Should().BeEmpty();
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
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

    private static Subrace HighElf2024() => new()
    {
        Id = "high_elf_2024", Name = "High Elf", Source = "XPHB", RaceId = "elf",
        SpellChoice = new BonusSpellChoice { ClassIds = ["wizard"], Cantrips = 1, AbilityOptions = ["INT", "WIS", "CHA"] },
    };

    private static ClassReadModel Caster(string id, string ability) => new()
    {
        Id = id,
        Name = id,
        Edition = RulesEdition.Edition2014,
        HitDie = "1d8",
        Spellcasting = new SpellcastingReadModel { SpellcastingAbility = ability },
    };

    private Task<List<AttackCantrip>> AttackCantripsAsync(
        List<SpellReadModel> spells, Class @class, Subclass? subclass = null, Race? race = null, Subrace? subrace = null, List<FeatSpellPick>? featSpells = null) =>
        BonusSpellcasting.AttackCantripsAsync(
            spells, 1, @class, subclass, race ?? Elf(subrace), subrace, null, featSpells ?? [], _classRepositoryMock.Object, _cts.Token);

    private static SpellReadModel FireBolt() => Cantrip("fire_bolt", "Fire Bolt", ["wizard", "sorcerer"], damageType: "fire", dice: ["1d10", "2d10", "3d10", "4d10"]);

    private static SpellReadModel Cantrip(
        string id, string name, List<string> classIds, string? damageType = "fire", List<string>? dice = null, string? saveAbilityId = null, string? attackType = "ranged") => new()
    {
        Id = id,
        Name = name,
        Level = 0,
        School = "Evocation",
        CastingTimeUnit = "action",
        RangeType = "point",
        DurationType = "instant",
        Description = string.Empty,
        ClassIds = classIds,
        AttackType = attackType,
        SaveAbilityId = saveAbilityId,
        DamageType = damageType,
        CantripDamageDice = dice ?? ["1d8", "2d8", "3d8", "4d8"],
    };

    // A full caster of this class with three cantrips from level 1
    private static Class Wizard(string id = "wizard", string ability = "INT") => new()
    {
        Id = id,
        Edition = RulesEdition.Edition2014,
        Name = id,
        Source = "PHB",
        HitDie = "1d6",
        Spellcasting = new Spellcasting { ClassId = id, SpellcastingAbility = ability },
        SpellProgression = [new ClassSpellProgression { ClassId = id, CharacterLevel = 1, CantripsKnown = 3, MaxSpellLevel = 1 }],
    };

    private static Class Fighter() => new()
    {
        Id = "fighter",
        Edition = RulesEdition.Edition2014,
        Name = "Fighter",
        Source = "PHB",
        HitDie = "1d10",
    };

    private static CharacterFeatReadModel Feat(string id, string name) => new()
    {
        Feat = new FeatReadModel { Id = id, Name = name, Description = string.Empty, Edition = RulesEdition.Edition2014 },
    };
}
