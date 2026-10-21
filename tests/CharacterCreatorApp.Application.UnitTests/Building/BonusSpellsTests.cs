using CharacterCreatorApp.Application.Features.CharacterCreation.Building;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.DomainModels.Spells;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

namespace CharacterCreatorApp.Application.UnitTests.Building;

public class BonusSpellsTests
{
    private static readonly FakeTimeProvider FakeTimeProvider = new(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero));

    private static readonly BonusSpellChoice MagicInitiate = new() { ClassIds = ["cleric", "wizard"], Cantrips = 2, FirstLevelSpells = 1 };

    // The same lists, and the spellcasting ability picked with them
    private static readonly BonusSpellChoice MagicInitiate2024 = new()
    {
        ClassIds = ["cleric", "wizard"],
        Cantrips = 2,
        FirstLevelSpells = 1,
        AbilityOptions = ["INT", "WIS", "CHA"],
    };

    private static readonly Dictionary<string, List<SpellReadModel>> Lists = new Dictionary<string, List<SpellReadModel>>
    {
        ["cleric"] = [Spell("light", 0), Spell("guidance", 0), Spell("bless", 1), Spell("aid", 2)],
        ["wizard"] = [Spell("fire_bolt", 0), Spell("mage_hand", 0), Spell("shield", 1), Spell("toll_the_dead", 0, "XGE")],
    };

    [Fact]
    public void RaceSlot_ForTheHighElf_IsItsCantripUnderItsName()
    {
        // Arrange
        var cantrip = new BonusSpellChoice { ClassIds = ["wizard"], Cantrips = 1 };
        var elf = new Race { Id = "elf", Name = "Elf", Edition = RulesEdition.Edition2014, Size = "Medium" };
        var highElf = new Subrace { Id = "high_elf", Name = "High Elf", RaceId = "elf", SpellChoice = cantrip };

        // Act
        var slot = BonusSpells.RaceSlot(elf, highElf);

        // Assert
        slot.Should().Be(new BonusSpellSlot(null, "High Elf", cantrip));
    }

    [Fact]
    public void FeatSlots_ListsTheBackgroundsFeatThenTheStylesThenTheOthers_SkippingFeatsWithoutAChoice()
    {
        // Arrange
        var draft = Draft();
        draft.SetBackground("acolyte_2024", null, null);
        draft.ChooseFightingStyles(["blessed_warrior"]);
        draft.SetFeats(["alert", "magic_initiate"]);
        var background = Background(featId: "magic_initiate", fixedClassId: null);
        var feats = FeatsById(Feat("magic_initiate", MagicInitiate), Feat("blessed_warrior", new BonusSpellChoice { ClassIds = ["cleric"], Cantrips = 2 }), Feat("alert", null));

        // Act
        var slots = BonusSpells.FeatSlots(draft, background, feats);

        // Assert
        slots.Select(s => s.FeatId).Should().Equal("magic_initiate", "blessed_warrior", "magic_initiate");
    }

    [Fact]
    public void FeatSlots_ForABackgroundThatFixesTheList_OffersOnlyThatList()
    {
        // Arrange - an Acolyte's Magic Initiate is the cleric's
        var draft = Draft();
        var feats = FeatsById(Feat("magic_initiate", MagicInitiate));

        // Act
        var slot = BonusSpells.FeatSlots(draft, Background("magic_initiate", fixedClassId: "cleric"), feats).Single();

        // Assert
        slot.Choice.ClassIds.Should().Equal("cleric");
        (slot.Choice.Cantrips, slot.Choice.FirstLevelSpells).Should().Be((2, 1));
    }

    [Fact]
    public void FeatSlots_ForABackgroundWithAPickedFeat_UsesThePick()
    {
        // Arrange - a Mist Wanderer's Dark Gift
        var draft = Draft();
        draft.SetBackground("mist_wanderer_2024", null, "magic_initiate");
        var background = Background(featId: null, fixedClassId: null);
        background.FeatChoiceCategory = "Dark Gift";

        // Act
        var slots = BonusSpells.FeatSlots(draft, background, FeatsById(Feat("magic_initiate", MagicInitiate)));

        // Assert
        slots.Select(s => s.FeatId).Should().Equal("magic_initiate");
    }

    [Fact]
    public void FindInvalidPick_WithEveryPickRight_ReturnsNull()
    {
        // Act
        var error = Find(raceSpellIds: ["fire_bolt"], featSpells: [Pick("cleric", "light", "guidance", "bless")]);

        // Assert
        error.Should().BeNull();
    }

    [Fact]
    public void FindInvalidPick_WithTheRacesCantripMissing_ReturnsBonusSpellCount()
    {
        // Act
        var error = Find(raceSpellIds: [], featSpells: [Pick("cleric", "light", "guidance", "bless")]);

        // Assert
        error!.Value.Code.Should().Be("CharacterDraft.BonusSpellCount");
        error.Value.Description.Should().Contain("High Elf");
    }

    [Fact]
    public void FindInvalidPick_WithRaceSpellsAndNoRaceChoice_ReturnsBonusSpellCount()
    {
        // Act
        var error = BonusSpells.FindInvalidPick(Draft(), null, [], ["fire_bolt"], [], Lists);

        // Assert
        error!.Value.Code.Should().Be("CharacterDraft.BonusSpellCount");
    }

    [Fact]
    public void FindInvalidPick_WithAFeatsPickMissing_ReturnsBonusSpellPicksMismatch()
    {
        // Act
        var error = Find(raceSpellIds: ["fire_bolt"], featSpells: []);

        // Assert
        error!.Value.Code.Should().Be("CharacterDraft.BonusSpellPicksMismatch");
        error.Value.Description.Should().Contain("Magic Initiate");
    }

    [Fact]
    public void FindInvalidPick_WithAPickForAnotherFeat_ReturnsBonusSpellPicksMismatch()
    {
        // Arrange
        var pick = new FeatSpellPick { FeatId = "artificer_initiate", ClassId = "cleric", SpellIds = ["light", "guidance", "bless"] };

        // Act
        var error = Find(raceSpellIds: ["fire_bolt"], featSpells: [pick]);

        // Assert
        error!.Value.Code.Should().Be("CharacterDraft.BonusSpellPicksMismatch");
    }

    [Fact]
    public void FindInvalidPick_WithAListTheFeatDoesNotOffer_ReturnsBonusSpellListNotOffered()
    {
        // Act
        var error = Find(raceSpellIds: ["fire_bolt"], featSpells: [Pick("druid", "light", "guidance", "bless")]);

        // Assert
        error!.Value.Code.Should().Be("CharacterDraft.BonusSpellListNotOffered");
    }

    [Fact]
    public void FindInvalidPick_WithARepeatableFeatsListPickedTwice_ReturnsBonusSpellListRepeated()
    {
        // Arrange - the 2024 Magic Initiate takes another list each time
        var slot = new BonusSpellSlot("magic_initiate", "Magic Initiate", MagicInitiate);
        var pick = Pick("cleric", "light", "guidance", "bless");

        // Act
        var error = BonusSpells.FindInvalidPick(Draft(), null, [slot, slot], [], [pick, pick], Lists);

        // Assert
        error!.Value.Code.Should().Be("CharacterDraft.BonusSpellListRepeated");
    }

    [Fact]
    public void FindInvalidPick_WithASpellFromAnotherList_ReturnsBonusSpellNotOnList()
    {
        // Act
        var error = Find(raceSpellIds: ["fire_bolt"], featSpells: [Pick("cleric", "light", "fire_bolt", "bless")]);

        // Assert
        error!.Value.Code.Should().Be("CharacterDraft.BonusSpellNotOnList");
        error.Value.Description.Should().Contain("fire_bolt");
    }

    [Fact]
    public void FindInvalidPick_WithASpellTwice_ReturnsSpellDuplicate()
    {
        // Act
        var error = Find(raceSpellIds: ["fire_bolt"], featSpells: [Pick("cleric", "light", "light", "bless")]);

        // Assert
        error!.Value.Code.Should().Be("Spell.Duplicate");
    }

    [Fact]
    public void FindInvalidPick_WithASpellFromABookNotAllowed_ReturnsSourceNotAllowed()
    {
        // Act - Toll the Dead is Xanathar's, which the draft does not allow
        var error = Find(raceSpellIds: ["toll_the_dead"], featSpells: [Pick("cleric", "light", "guidance", "bless")]);

        // Assert
        error!.Value.Code.Should().Be("CharacterDraft.SourceNotAllowed");
    }

    [Theory]
    [InlineData("light", "bless", "aid")]
    [InlineData("light", "guidance", "guidance_extra")]
    [InlineData("light", "guidance")]
    public void FindInvalidPick_WithTheWrongCounts_ReturnsBonusSpellCount(params string[] spellIds)
    {
        // Arrange - one cantrip short and a 2nd-level spell, three cantrips, or no 1st-level spell
        var lists = new Dictionary<string, List<SpellReadModel>>(Lists) { ["cleric"] = [.. Lists["cleric"], Spell("guidance_extra", 0)] };
        var pick = Pick("cleric", spellIds);

        // Act
        var error = BonusSpells.FindInvalidPick(Draft(), null, [MagicInitiateSlot()], [], [pick], lists);

        // Assert
        error!.Value.Code.Should().Be("CharacterDraft.BonusSpellCount");
    }

    [Theory]
    [InlineData("WIS")]
    [InlineData("wisdom")]
    public void FindInvalidPick_WithASpellcastingAbilityTheFeatOffers_ReturnsNull(string abilityId)
    {
        // Arrange - the 2024 Magic Initiate's ability is Intelligence, Wisdom or Charisma, in any spelling
        var pick = Pick("cleric", "light", "guidance", "bless");
        pick.AbilityId = abilityId;

        // Act
        var error = BonusSpells.FindInvalidPick(Draft(), null, [MagicInitiate2024Slot()], [], [pick], Lists);

        // Assert
        error.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("STR")]
    [InlineData("luck")]
    public void FindInvalidPick_WithoutASpellcastingAbilityTheFeatOffers_ReturnsInvalidSpellcastingAbility(string? abilityId)
    {
        // Arrange - none picked, one it does not offer, and one that is no ability
        var pick = Pick("cleric", "light", "guidance", "bless");
        pick.AbilityId = abilityId;

        // Act
        var error = BonusSpells.FindInvalidPick(Draft(), null, [MagicInitiate2024Slot()], [], [pick], Lists);

        // Assert
        error!.Value.Code.Should().Be("CharacterDraft.InvalidSpellcastingAbility");
        error.Value.Description.Should().Be(
            "Magic Initiate's spellcasting ability is one of Intelligence, Wisdom, Charisma; pick it with the spells.");
    }

    [Fact]
    public void FindInvalidPick_WithASpellcastingAbilityForAFeatOfferingNone_ReturnsInvalidSpellcastingAbility()
    {
        // Arrange - the 2014 Magic Initiate's ability comes with the list
        var pick = Pick("cleric", "light", "guidance", "bless");
        pick.AbilityId = "WIS";

        // Act
        var error = BonusSpells.FindInvalidPick(Draft(), null, [MagicInitiateSlot()], [], [pick], Lists);

        // Assert
        error!.Value.Code.Should().Be("CharacterDraft.InvalidSpellcastingAbility");
        error.Value.Description.Should().Be("Magic Initiate has no spellcasting ability to pick.");
    }

    [Fact]
    public void FeatSlots_ForABackgroundThatFixesTheList_KeepsTheAbilityToPick()
    {
        // Arrange - a 2024 Acolyte's Magic Initiate is the cleric's, its ability still the player's
        var draft = Draft();
        var feats = FeatsById(Feat("magic_initiate", MagicInitiate2024));

        // Act
        var slot = BonusSpells.FeatSlots(draft, Background("magic_initiate", fixedClassId: "cleric"), feats).Single();

        // Assert
        slot.Choice.AbilityOptions.Should().Equal("INT", "WIS", "CHA");
    }

    private static Error? Find(List<string> raceSpellIds, List<FeatSpellPick> featSpells) =>
        BonusSpells.FindInvalidPick(
            Draft(),
            new BonusSpellSlot(null, "High Elf", new BonusSpellChoice { ClassIds = ["wizard"], Cantrips = 1 }),
            [MagicInitiateSlot()],
            raceSpellIds,
            featSpells,
            Lists);

    private static BonusSpellSlot MagicInitiateSlot() => new("magic_initiate", "Magic Initiate", MagicInitiate);

    private static BonusSpellSlot MagicInitiate2024Slot() => new("magic_initiate", "Magic Initiate", MagicInitiate2024);

    private static FeatSpellPick Pick(string classId, params string[] spellIds) =>
        new() { FeatId = "magic_initiate", ClassId = classId, SpellIds = [.. spellIds] };

    // A 2014 draft, which allows only the Player's Handbook
    private static CharacterDraft Draft()
    {
        var draft = CharacterDraft.CreateEmpty(Guid.NewGuid(), FakeTimeProvider);
        draft.ChooseEdition(RulesEdition.Edition2014);
        return draft;
    }

    private static Background Background(string? featId, string? fixedClassId) => new()
    {
        Id = "acolyte_2024",
        Name = "Acolyte",
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        FeatId = featId,
        FeatSpellListClassId = fixedClassId,
    };

    private static Dictionary<string, FeatReadModel> FeatsById(params FeatReadModel[] feats) => feats.ToDictionary(f => f.Id);

    private static FeatReadModel Feat(string id, BonusSpellChoice? spellChoice) => new()
    {
        Id = id,
        Name = id == "magic_initiate" ? "Magic Initiate" : id,
        Description = "A feat.",
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        SpellChoice = spellChoice,
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
