using CharacterCreatorApp.Domain.ValueObjects;
using CharacterCreatorApp.Infrastructure.Seed;
using CharacterCreatorApp.Infrastructure.Seed.Rows;

using FluentAssertions;

namespace CharacterCreatorApp.Infrastructure.IntegrationTests.Seed;

/// <summary>
/// The races the generator writes from 5etools, checked against the books, a row per race. No
/// database is involved.
/// </summary>
public class RaceSeedDataTests
{
    [Theory]
    [InlineData("dragonborn", "Medium", 30, 0, 0, new[] { "Common", "Draconic" })]
    [InlineData("dwarf", "Medium", 25, 0, 0, new[] { "Common", "Dwarvish" })]
    [InlineData("elf", "Medium", 30, 0, 0, new[] { "Common", "Elvish" })]
    [InlineData("gnome", "Small", 25, 0, 0, new[] { "Common", "Gnomish" })]
    [InlineData("half_elf", "Medium", 30, 0, 1, new[] { "Common", "Elvish" })]
    [InlineData("half_orc", "Medium", 30, 0, 0, new[] { "Common", "Orc" })]
    [InlineData("halfling", "Small", 25, 0, 0, new[] { "Common", "Halfling" })]
    [InlineData("human", "Medium", 30, 0, 1, new[] { "Common" })]
    [InlineData("tiefling", "Medium", 30, 0, 0, new[] { "Common", "Infernal" })]
    public async Task Race2014_HasTheSizeSpeedAndChoicesOfThePlayersHandbook(
        string raceId, string size, int speed, int skillChoices, int languageChoices, string[] languages)
    {
        // Act
        var race = await ReadRaceAsync(raceId);

        // Assert - the half-elf's Skill Versatility is its Standard subrace's, which SCAG's variants replace
        race.Edition.Should().Be(RulesEdition.Edition2014);
        race.Source.Should().Be("PHB");
        (race.Size, race.Speed, race.SkillChoiceCount, race.LanguageChoiceCount).Should().Be((size, speed, skillChoices, languageChoices));
        race.Languages.Should().Equal(languages);
    }

    [Theory]
    [InlineData("aasimar_2024", "Small or Medium", 30, 0)]
    [InlineData("dragonborn_2024", "Medium", 30, 10)]
    [InlineData("dwarf_2024", "Medium", 30, 0)]
    [InlineData("elf_2024", "Medium", 30, 3)]
    [InlineData("gnome_2024", "Small", 30, 2)]
    [InlineData("goliath_2024", "Medium", 35, 6)]
    [InlineData("halfling_2024", "Small", 30, 0)]
    [InlineData("human_2024", "Small or Medium", 30, 0)]
    [InlineData("orc_2024", "Medium", 30, 0)]
    [InlineData("tiefling_2024", "Small or Medium", 30, 3)]
    public async Task Species2024_HasTheSizeSpeedAndLineagesOfThePlayersHandbook(string raceId, string size, int speed, int lineages)
    {
        // Act
        var race = await ReadRaceAsync(raceId);

        // Assert - ability increases and languages come with the 2024 background instead
        race.Edition.Should().Be(RulesEdition.Edition2024);
        race.Subraces.Select(s => s.Source).Prepend(race.Source).Should().AllBe("XPHB");
        (race.Size, race.Speed, race.Subraces.Count).Should().Be((size, speed, lineages));
        race.AbilityBonuses.Should().BeEmpty();
        race.Languages.Should().BeEmpty();
    }

    [Theory]
    [InlineData("elf_2024", 1, new[] { "insight", "perception", "survival" })]
    [InlineData("human_2024", 1, new string[0])]
    public async Task Race_OffersTheSkillChoiceOfItsBook(string raceId, int count, string[] options)
    {
        // Act - Keen Senses (2024) is one of three; Skillful and Skill Versatility are any skill
        var race = await ReadRaceAsync(raceId);

        // Assert
        race.SkillChoiceCount.Should().Be(count);
        race.SkillChoiceOptions.Should().Equal(options);
    }

    [Theory]
    [InlineData("human", 0, new string[0])]
    [InlineData("dwarf", 0, new string[0])]
    public async Task Race2014_OffersTheAbilityIncreasesOfItsBook(string raceId, int count, string[] options)
    {
        // Act
        var race = await ReadRaceAsync(raceId);

        // Assert
        race.AbilityChoiceCount.Should().Be(count);
        race.AbilityChoiceOptions.Should().Equal(options);
    }

    [Fact]
    public async Task HalfElf_IsTheBooksAsItsStandardSubraceAndADragonmarkReplacesItsIncreases()
    {
        // Act - ERLW's Mark of Detection raises Wisdom by 2 in place of the half-elf's Charisma
        var halfElf = await ReadRaceAsync("half_elf");

        // Assert
        halfElf.AbilityBonuses.Should().BeEmpty();
        var standard = halfElf.Subraces.Single(s => s.Id == "standard_half_elf");
        standard.Source.Should().Be("PHB");
        standard.AbilityBonuses.Select(b => (b.AbilityScoreId, b.Bonus)).Should().Equal(("CHA", 2));
        (standard.AbilityChoiceCount, standard.AbilityChoiceMaxPerAbility, standard.SkillChoiceCount).Should().Be((2, 1, 2));
        standard.AbilityChoiceOptions.Should().Equal("STR", "DEX", "CON", "INT", "WIS");
        standard.Traits.Select(t => t.Name).Should().Equal("Skill Versatility");

        var detection = halfElf.Subraces.Single(s => s.Id == "mark_of_detection_half_elf");
        detection.Source.Should().Be("ERLW");
        detection.AbilityBonuses.Select(b => (b.AbilityScoreId, b.Bonus)).Should().Equal(("WIS", 2));
        detection.AbilityChoiceOptions.Should().Equal("STR", "DEX", "CON", "INT", "CHA");
    }

    [Fact]
    public async Task Human2014_IsAStandardOrAVariantHuman()
    {
        // Act - the variant's picks replace the standard human's +1 to everything
        var human = await ReadRaceAsync("human");

        // Assert
        human.AbilityBonuses.Should().BeEmpty();
        human.Subraces.Where(s => s.Source == "PHB").Select(s => s.Id).Should().Equal("standard_human", "variant_human");

        var standard = human.Subraces[0];
        standard.AbilityBonuses.Select(b => (b.AbilityScoreId, b.Bonus)).Should().Equal(
            ("STR", 1), ("DEX", 1), ("CON", 1), ("INT", 1), ("WIS", 1), ("CHA", 1));
        (standard.AbilityChoiceCount, standard.SkillChoiceCount, standard.FeatChoiceCount).Should().Be((0, 0, 0));

        var variant = human.Subraces.Single(s => s.Id == "variant_human");
        variant.AbilityBonuses.Should().BeEmpty();
        variant.AbilityChoiceOptions.Should().Equal("STR", "DEX", "CON", "INT", "WIS", "CHA");
        (variant.AbilityChoiceCount, variant.SkillChoiceCount, variant.FeatChoiceCount, variant.FeatChoiceCategory).Should().Be((2, 1, 1, (string?)null));
        variant.SkillChoiceOptions.Should().BeEmpty();
    }

    [Fact]
    public async Task Races_GiveAFeatOnlyAsTheBooksSay()
    {
        // Act
        var races = await SeedFile.ReadListAsync<RaceSeedRow>(Path.Combine(SeedService.DataPath, "Races.json"), TestContext.Current.CancellationToken);

        // Assert - Tasha's Custom Lineage, the 2024 human's Versatile, and the variant human's feat,
        // which Plane Shift: Amonkhet's human copies
        races.Where(r => r.FeatChoiceCount > 0).Select(r => (r.Id, r.FeatChoiceCount, r.FeatChoiceCategory)).Should().Equal(
            ("custom_lineage_tce", 1, (string?)null), ("human_2024", 1, "Origin"));
        races.SelectMany(r => r.Subraces).Where(s => s.FeatChoiceCount > 0).Select(s => s.Id).Should().Equal("amonkhet_human", "variant_human");
    }

    [Fact]
    public async Task Elf2014_KnowsPerceptionAndItsSubracesDifferAsTheBookSays()
    {
        // Act - the hand-made data had no Perception, no drow and a 30-foot wood elf
        var elf = await ReadRaceAsync("elf");

        // Assert
        elf.ProficiencyIds.Should().Equal("perception");
        elf.Subraces.Where(s => s.Source == "PHB").Select(s => s.Id).Should().Equal("drow_elf", "high_elf", "wood_elf");

        var subraces = elf.Subraces.ToDictionary(s => s.Id);
        subraces["high_elf"].LanguageChoiceCount.Should().Be(1);
        subraces["wood_elf"].Speed.Should().Be(35);
        subraces["drow_elf"].Spells.Select(s => (s.SpellId, s.Level)).Should().Equal(("dancing_lights", 1), ("faerie_fire", 3), ("darkness", 5));
    }

    [Fact]
    public async Task Dragonborn2024_HasAnAncestryForEachDragonWithItsOwnDamageType()
    {
        // Act
        var dragonborn = await ReadRaceAsync("dragonborn_2024");

        // Assert - the species keeps the traits every ancestry shares
        dragonborn.Traits.Select(t => t.Name).Should().Equal("Darkvision", "Draconic Flight");

        var black = dragonborn.Subraces.Single(s => s.Id == "dragonborn_black_2024");
        black.Name.Should().Be("Black");
        black.Traits.Single(t => t.Name == "Damage Resistance").Description.Should().Be("You have Resistance to Acid damage.");
    }

    [Fact]
    public async Task Tiefling2024_LegaciesGiveThaumaturgyAndTheirOwnSpells()
    {
        // Act
        var tiefling = await ReadRaceAsync("tiefling_2024");

        // Assert
        var infernal = tiefling.Subraces.Single(s => s.Id == "tiefling_infernal_legacy_2024");
        infernal.Spells.Select(s => (s.SpellId, s.Level)).Should().Equal(
            ("thaumaturgy_2024", 1), ("fire_bolt_2024", 1), ("hellish_rebuke_2024", 3), ("darkness_2024", 5));
    }

    [Fact]
    public async Task Elf2024_KeepsDarkvisionForEveryLineage()
    {
        // Act - the drow lineage has a longer one of its own
        var elf = await ReadRaceAsync("elf_2024");

        // Assert
        elf.Traits.Select(t => t.Name).Should().Contain("Darkvision").And.NotContain("Elven Lineage");
        elf.Subraces.Single(s => s.Id == "elf_drow_lineage_2024").Traits.Select(t => t.Name).Should().Equal("Elven Lineage (Drow)", "Darkvision");
    }

    [Theory]
    // The 2014 high elf's cantrip, and the 2024 High Elf Lineage's, which 5etools writes as a
    // wizard cantrip of the player's choice in place of Prestidigitation
    [InlineData("elf", "high_elf", "wizard")]
    [InlineData("elf_2024", "elf_high_elf_lineage_2024", "wizard-2024")]
    public async Task HighElf_PicksOneCantripFromTheWizardsList(string raceId, string subraceId, string classId)
    {
        // Act
        var highElf = (await ReadRaceAsync(raceId)).Subraces.Single(s => s.Id == subraceId);

        // Assert
        highElf.SpellChoice!.ClassIds.Should().Equal(classId);
        (highElf.SpellChoice.Cantrips, highElf.SpellChoice.FirstLevelSpells).Should().Be((1, 0));
    }

    [Fact]
    public async Task Races_PickSpellsOnlyAsTheBooksSay()
    {
        // Act
        var races = await SeedFile.ReadListAsync<RaceSeedRow>(Path.Combine(SeedService.DataPath, "Races.json"), TestContext.Current.CancellationToken);

        // Assert - the high elves' wizard cantrip, a kobold's Draconic Sorcery, a Kaladesh vahadar's
        // and a half-elf of moon or sun elf descent's cantrip, the merfolk's, and a Lorwyn elf's
        races.Where(r => r.SpellChoice != null).Should().BeEmpty();
        races.SelectMany(r => r.Subraces).Where(s => s.SpellChoice != null).Select(s => s.Id).Should().BeEquivalentTo(
        [
            "high_elf", "elf_high_elf_lineage_2024", "kobold_mpmm_draconic_sorcery", "vahadar_elf_kaladesh_psk",
            "moon_elf_or_sun_elf_descent_half_elf", "blue_merfolk_psz", "green_merfolk_psz", "cosi_creed_merfolk_psz",
            "emeria_creed_merfolk_psz", "ula_creed_merfolk_psz", "elf_lfl_lorwyn_lineage_2024",
        ]);
    }

    [Fact]
    public async Task Dwarf2014_PicksSmithsToolsBrewersSuppliesOrMasonsTools()
    {
        // Act
        var dwarf = await ReadRaceAsync("dwarf");

        // Assert
        dwarf.ToolChoices.Should().ContainSingle();
        dwarf.ToolChoices[0].Count.Should().Be(1);
        dwarf.ToolChoices[0].Options.Should().Equal("brewer_supplies", "mason_tools", "smith_tools");
    }

    [Fact]
    public async Task Dwarf2024_PicksNoTools()
    {
        // Act
        var dwarf = await ReadRaceAsync("dwarf_2024");

        // Assert
        dwarf.ToolChoices.Should().BeEmpty();
    }

    [Theory]
    [InlineData("aasimar_vgm", "VGM", "2014")]
    [InlineData("aasimar_mpmm", "MPMM", "2014")]
    [InlineData("warforged_erlw", "ERLW", "2014")]
    [InlineData("warforged_efa_2024", "EFA", "2024")]
    [InlineData("dhampir_vrgr", "VRGR", "2014")]
    [InlineData("dhampir_rhw_2024", "RHW", "2024")]
    [InlineData("elf_lfl_2024", "LFL", "2024")]
    public async Task Race_FromAnotherBook_HasItsBookAndEditionInItsId(string raceId, string source, string edition)
    {
        // Act - a reprint is a race of its own, so a draft may tick one book and not the other
        var race = await ReadRaceAsync(raceId);

        // Assert
        (race.Source, race.Edition).Should().Be((source, edition));
    }

    [Theory]
    [InlineData("aasimar_mpmm")]
    [InlineData("autognome_aag")]
    [InlineData("hexblood_vrgr")]
    [InlineData("fairy_wbtw")]
    [InlineData("owlin_scc")]
    [InlineData("kender_dsotdq")]
    public async Task Race2014_OfTashasLineageRules_RaisesPlusTwoAndPlusOneOrPlusOneToThree(string raceId)
    {
        // Act
        var race = await ReadRaceAsync(raceId);

        // Assert
        race.AbilityBonuses.Should().BeEmpty();
        (race.AbilityChoiceCount, race.AbilityChoiceMaxPerAbility, race.AbilityChoiceMinPerAbility).Should().Be((3, 2, 1));
        race.AbilityChoiceOptions.Should().Equal("STR", "DEX", "CON", "INT", "WIS", "CHA");
    }

    [Fact]
    public async Task CustomLineage_RaisesOneAbilityByTwoAndIsDarkvisionOrASkill()
    {
        // Act
        var lineage = await ReadRaceAsync("custom_lineage_tce");

        // Assert
        (lineage.AbilityChoiceCount, lineage.AbilityChoiceMaxPerAbility, lineage.AbilityChoiceMinPerAbility).Should().Be((2, 2, 2));
        lineage.SkillChoiceCount.Should().Be(0);
        lineage.Subraces.Select(s => (s.Id, s.SkillChoiceCount)).Should().Equal(
            ("custom_lineage_tce_darkvision", 0), ("custom_lineage_tce_skill_proficiency", 1));
    }

    [Fact]
    public async Task WingedTiefling_RaisesDexterityOrCharismaByTwoFliesAndHasNoInfernalLegacy()
    {
        // Act - SCAG's variant tiefling: Intelligence +1 and +2 to Dexterity or Charisma
        var winged = (await ReadRaceAsync("tiefling")).Subraces.Single(s => s.Id == "winged_tiefling");

        // Assert
        winged.AbilityBonuses.Select(b => (b.AbilityScoreId, b.Bonus)).Should().Equal(("INT", 1));
        (winged.AbilityChoiceCount, winged.AbilityChoiceMaxPerAbility, winged.AbilityChoiceMinPerAbility).Should().Be((2, 2, 2));
        winged.AbilityChoiceOptions.Should().Equal("DEX", "CHA");
        winged.Spells.Should().BeEmpty("its wings replace the Infernal Legacy");
        winged.Traits.Single(t => t.Name == "Speeds").Description.Should().StartWith("Flying speed 30 feet.");
    }

    [Fact]
    public async Task Dragonborn2014_DraconbloodReplacesTheIncreasesTheStandardDragonbornKeeps()
    {
        // Act
        var dragonborn = await ReadRaceAsync("dragonborn");

        // Assert
        dragonborn.AbilityBonuses.Should().BeEmpty();
        var subraces = dragonborn.Subraces.ToDictionary(s => s.Id);
        subraces["standard_dragonborn"].AbilityBonuses.Select(b => (b.AbilityScoreId, b.Bonus)).Should().Equal(("STR", 2), ("CHA", 1));
        subraces["draconblood_dragonborn"].AbilityBonuses.Select(b => (b.AbilityScoreId, b.Bonus)).Should().Equal(("INT", 2), ("CHA", 1));
    }

    [Fact]
    public async Task Dwarf2014_DuergarReplacesTheDarkvisionTheOtherDwarvesKeep()
    {
        // Act
        var dwarf = await ReadRaceAsync("dwarf");

        // Assert
        dwarf.Traits.Select(t => t.Name).Should().NotContain("Darkvision");
        var subraces = dwarf.Subraces.ToDictionary(s => s.Id);
        subraces["hill_dwarf"].Traits.Select(t => t.Name).Should().Contain(["Darkvision", "Dwarven Toughness"]);
        subraces["duergar_dwarf"].Traits.Select(t => t.Name).Should().Contain("Superior Darkvision").And.NotContain("Darkvision");
    }

    [Fact]
    public async Task GoblinOfEberron_IsTheCopyOfVolosGoblin()
    {
        // Act - 5etools writes it as a copy of VGM's
        var goblin = await ReadRaceAsync("goblin_erlw");

        // Assert
        (goblin.Size, goblin.Speed).Should().Be(("Small", 30));
        goblin.AbilityBonuses.Select(b => (b.AbilityScoreId, b.Bonus)).Should().Equal(("DEX", 2), ("CON", 1));
        goblin.Traits.Select(t => t.Name).Should().Equal("Darkvision", "Fury of the Small", "Nimble Escape");
    }

    [Theory]
    [InlineData("autognome_aag", "Your creature type is Construct.")]
    [InlineData("kalashtar_efa_2024", "Your creature type is Aberration.")]
    public async Task Race_NotHumanoid_SaysItsCreatureType(string raceId, string mechanics)
    {
        // Act
        var race = await ReadRaceAsync(raceId);

        // Assert
        race.Traits.Single(t => t.Name == "Creature Type").Description.Should().StartWith(mechanics);
    }

    [Fact]
    public async Task Species2024_FromAnotherBook_RaisesNoAbilities()
    {
        // Act - the 2024 background raises them, as for every 2024 species
        var races = await SeedFile.ReadListAsync<RaceSeedRow>(Path.Combine(SeedService.DataPath, "Races.json"), TestContext.Current.CancellationToken);

        // Assert
        races.Where(r => r.Edition == RulesEdition.Edition2024)
            .Where(r => r.AbilityBonuses.Count != 0 || r.AbilityChoiceCount != 0)
            .Select(r => r.Id)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task EveryRaceWithSubraces_HasOneFromItsOwnBook()
    {
        // Act - a race needs a subrace at finalize, and a draft always allows the race's own book
        var races = await SeedFile.ReadListAsync<RaceSeedRow>(Path.Combine(SeedService.DataPath, "Races.json"), TestContext.Current.CancellationToken);

        // Assert
        races.Where(r => r.Subraces.Count != 0 && !r.Subraces.Exists(s => s.Source == r.Source)).Select(r => r.Id).Should().BeEmpty();
    }

    private static async Task<RaceSeedRow> ReadRaceAsync(string raceId)
    {
        var races = await SeedFile.ReadListAsync<RaceSeedRow>(Path.Combine(SeedService.DataPath, "Races.json"), TestContext.Current.CancellationToken);
        return races.Single(r => r.Id == raceId);
    }
}
