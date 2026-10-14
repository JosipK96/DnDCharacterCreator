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
    [InlineData("half_elf", "Medium", 30, 2, 1, new[] { "Common", "Elvish" })]
    [InlineData("half_orc", "Medium", 30, 0, 0, new[] { "Common", "Orc" })]
    [InlineData("halfling", "Small", 25, 0, 0, new[] { "Common", "Halfling" })]
    [InlineData("human", "Medium", 30, 0, 1, new[] { "Common" })]
    [InlineData("tiefling", "Medium", 30, 0, 0, new[] { "Common", "Infernal" })]
    public async Task Race2014_HasTheSizeSpeedAndChoicesOfThePlayersHandbook(
        string raceId, string size, int speed, int skillChoices, int languageChoices, string[] languages)
    {
        // Act
        var race = await ReadRaceAsync(raceId);

        // Assert
        race.Edition.Should().Be(RulesEdition.Edition2014);
        race.Subraces.Select(s => s.Source).Prepend(race.Source).Should().AllBe("PHB");
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
    [InlineData("half_elf", 2, new string[0])]
    public async Task Race_OffersTheSkillChoiceOfItsBook(string raceId, int count, string[] options)
    {
        // Act - Keen Senses (2024) is one of three; Skillful and Skill Versatility are any skill
        var race = await ReadRaceAsync(raceId);

        // Assert
        race.SkillChoiceCount.Should().Be(count);
        race.SkillChoiceOptions.Should().Equal(options);
    }

    [Theory]
    [InlineData("half_elf", 2, new[] { "STR", "DEX", "CON", "INT", "WIS" })]
    [InlineData("human", 0, new string[0])]
    [InlineData("dwarf", 0, new string[0])]
    public async Task Race2014_OffersTheAbilityIncreasesOfItsBook(string raceId, int count, string[] options)
    {
        // Act - a half-elf raises two abilities other than its Charisma by 1
        var race = await ReadRaceAsync(raceId);

        // Assert
        race.AbilityChoiceCount.Should().Be(count);
        race.AbilityChoiceOptions.Should().Equal(options);
    }

    [Fact]
    public async Task HalfElf_KeepsItsCharismaBesideTheChoice()
    {
        // Act
        var halfElf = await ReadRaceAsync("half_elf");

        // Assert
        halfElf.AbilityBonuses.Select(b => (b.AbilityScoreId, b.Bonus)).Should().Equal(("CHA", 2));
    }

    [Fact]
    public async Task Human2014_IsAStandardOrAVariantHuman()
    {
        // Act - the variant's picks replace the standard human's +1 to everything
        var human = await ReadRaceAsync("human");

        // Assert
        human.AbilityBonuses.Should().BeEmpty();
        human.Subraces.Select(s => s.Id).Should().Equal("standard_human", "variant_human");

        var standard = human.Subraces[0];
        standard.AbilityBonuses.Select(b => (b.AbilityScoreId, b.Bonus)).Should().Equal(
            ("STR", 1), ("DEX", 1), ("CON", 1), ("INT", 1), ("WIS", 1), ("CHA", 1));
        (standard.AbilityChoiceCount, standard.SkillChoiceCount, standard.FeatChoiceCount).Should().Be((0, 0, 0));

        var variant = human.Subraces[1];
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

        // Assert - the 2024 human's Versatile, and the variant human's feat
        races.Where(r => r.FeatChoiceCount > 0).Select(r => (r.Id, r.FeatChoiceCount, r.FeatChoiceCategory)).Should().Equal(("human_2024", 1, "Origin"));
        races.SelectMany(r => r.Subraces).Where(s => s.FeatChoiceCount > 0).Select(s => s.Id).Should().Equal("variant_human");
    }

    [Fact]
    public async Task Elf2014_KnowsPerceptionAndItsSubracesDifferAsTheBookSays()
    {
        // Act - the hand-made data had no Perception, no drow and a 30-foot wood elf
        var elf = await ReadRaceAsync("elf");

        // Assert
        elf.ProficiencyIds.Should().Equal("perception");
        elf.Subraces.Select(s => s.Id).Should().Equal("drow_elf", "high_elf", "wood_elf");

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
    public async Task Races_OtherThanTheHighElves_PickNoSpells()
    {
        // Act
        var races = await SeedFile.ReadListAsync<RaceSeedRow>(Path.Combine(SeedService.DataPath, "Races.json"), TestContext.Current.CancellationToken);

        // Assert
        races.Where(r => r.SpellChoice != null).Should().BeEmpty();
        races.SelectMany(r => r.Subraces).Where(s => s.SpellChoice != null).Select(s => s.Id)
            .Should().BeEquivalentTo(["high_elf", "elf_high_elf_lineage_2024"]);
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

    private static async Task<RaceSeedRow> ReadRaceAsync(string raceId)
    {
        var races = await SeedFile.ReadListAsync<RaceSeedRow>(Path.Combine(SeedService.DataPath, "Races.json"), TestContext.Current.CancellationToken);
        return races.Single(r => r.Id == raceId);
    }
}
