using CharacterCreatorApp.Infrastructure.Seed;
using CharacterCreatorApp.Infrastructure.Seed.Rows;

using FluentAssertions;

namespace CharacterCreatorApp.Infrastructure.IntegrationTests.Seed;

/// <summary>
/// The per-class rules the generator reads from 5etools, checked against the Player's Handbook.
/// They used to be switch statements on Class, and two bugs came from assuming every class picks
/// its subclass at level 3, so every class has its own row. No database is involved.
/// </summary>
public class ClassSeedDataTests
{
    [Theory]
    [InlineData("artificer", 3)]
    [InlineData("barbarian", 3)]
    [InlineData("bard", 3)]
    [InlineData("cleric", 1)]
    [InlineData("druid", 2)]
    [InlineData("fighter", 3)]
    [InlineData("monk", 3)]
    [InlineData("paladin", 3)]
    [InlineData("ranger", 3)]
    [InlineData("rogue", 3)]
    [InlineData("sorcerer", 1)]
    [InlineData("warlock", 1)]
    [InlineData("wizard", 2)]
    public async Task Class_PicksItsSubclassAtTheBooksLevel(string classId, int level)
    {
        // Act
        var @class = await ClassAsync(classId);

        // Assert
        @class.SubclassLevel.Should().Be(level);
    }

    [Theory]
    [InlineData("artificer", new[] { 4, 8, 12, 16, 19 })]
    [InlineData("barbarian", new[] { 4, 8, 12, 16, 19 })]
    [InlineData("bard", new[] { 4, 8, 12, 16, 19 })]
    [InlineData("cleric", new[] { 4, 8, 12, 16, 19 })]
    [InlineData("druid", new[] { 4, 8, 12, 16, 19 })]
    [InlineData("fighter", new[] { 4, 6, 8, 12, 14, 16, 19 })]
    [InlineData("monk", new[] { 4, 8, 12, 16, 19 })]
    [InlineData("paladin", new[] { 4, 8, 12, 16, 19 })]
    [InlineData("ranger", new[] { 4, 8, 12, 16, 19 })]
    [InlineData("rogue", new[] { 4, 8, 10, 12, 16, 19 })]
    [InlineData("sorcerer", new[] { 4, 8, 12, 16, 19 })]
    [InlineData("warlock", new[] { 4, 8, 12, 16, 19 })]
    [InlineData("wizard", new[] { 4, 8, 12, 16, 19 })]
    // In 2024 the level 19 slot is the Epic Boon, a feat in the same place
    [InlineData("fighter-2024", new[] { 4, 6, 8, 12, 14, 16, 19 })]
    [InlineData("rogue-2024", new[] { 4, 8, 10, 12, 16, 19 })]
    [InlineData("wizard-2024", new[] { 4, 8, 12, 16, 19 })]
    public async Task Class_ImprovesAbilityScoresAtTheBooksLevels(string classId, int[] levels)
    {
        // Act
        var @class = await ClassAsync(classId);

        // Assert
        @class.AsiLevels.Should().Equal(levels);
    }

    [Theory]
    [InlineData("artificer", "CON", "INT")]
    [InlineData("barbarian", "STR", "CON")]
    [InlineData("bard", "DEX", "CHA")]
    [InlineData("cleric", "WIS", "CHA")]
    [InlineData("druid", "INT", "WIS")]
    [InlineData("fighter", "STR", "CON")]
    [InlineData("monk", "STR", "DEX")]
    [InlineData("paladin", "WIS", "CHA")]
    [InlineData("ranger", "STR", "DEX")]
    [InlineData("rogue", "DEX", "INT")]
    [InlineData("sorcerer", "CON", "CHA")]
    [InlineData("warlock", "WIS", "CHA")]
    [InlineData("wizard", "INT", "WIS")]
    public async Task Class_IsProficientInItsTwoSavingThrows(string classId, string first, string second)
    {
        // Act
        var @class = await ClassAsync(classId);

        // Assert
        @class.SavingThrowAbilityIds.Should().Equal(first, second);
    }

    [Theory]
    // Spells known, from the class tables
    [InlineData("bard", 10, 4, 14, 5)]
    [InlineData("ranger", 2, 0, 2, 1)]
    [InlineData("sorcerer", 20, 6, 15, 9)]
    [InlineData("warlock", 11, 4, 11, 5)]
    // A wizard picks the six spells of a first spellbook, and two more each level
    [InlineData("wizard", 1, 3, 6, 1)]
    [InlineData("wizard", 5, 4, 14, 3)]
    // Prepared casters prepare from the whole list, so they pick no levelled spells
    [InlineData("cleric", 17, 5, 0, 9)]
    [InlineData("druid", 4, 3, 0, 2)]
    [InlineData("paladin", 5, 0, 0, 2)]
    // The hardcoded table gave the Artificer three cantrips from level 14; it has four
    [InlineData("artificer", 14, 4, 0, 4)]
    // Every 2024 caster prepares a set number of spells, the Paladin from level 1
    [InlineData("cleric-2024", 1, 3, 4, 1)]
    [InlineData("paladin-2024", 1, 0, 2, 1)]
    [InlineData("warlock-2024", 11, 4, 11, 5)]
    // The 2024 wizard picks a spellbook too
    [InlineData("wizard-2024", 1, 3, 6, 1)]
    public async Task Class_HasTheBooksSpellTable(string classId, int level, int cantrips, int spells, int maxSpellLevel)
    {
        // Act
        var row = (await ClassAsync(classId)).SpellProgression.Single(p => p.Level == level);

        // Assert
        (row.CantripsKnown, row.SpellsKnown, row.MaxSpellLevel).Should().Be((cantrips, spells, maxSpellLevel));
    }

    [Fact]
    public async Task Classes2024_AllPickTheirSubclassAtLevelThree()
    {
        // Act
        var classes = (await ReadAsync<ClassSeedRow>("Classes.json")).Where(c => c.Edition == "2024").ToList();

        // Assert
        classes.Should().HaveCount(13);
        classes.Should().OnlyContain(c => c.SubclassLevel == 3);
    }

    [Fact]
    public async Task EveryClass_HasItsTwinInTheOtherEdition()
    {
        // Act - "wizard" and "wizard-2024" and so on
        var classes = await ReadAsync<ClassSeedRow>("Classes.json");

        // Assert
        classes.Where(c => c.Edition == "2014").Select(c => c.Id + "-2024")
            .Should().BeEquivalentTo(classes.Where(c => c.Edition == "2024").Select(c => c.Id));
    }

    [Theory]
    [InlineData("paladin")]
    [InlineData("ranger")]
    public async Task HalfCaster_CastsNothingAtLevelOne(string classId)
    {
        // Act
        var @class = await ClassAsync(classId);

        // Assert - a level with no row is a level without spells
        @class.SpellProgression.Should().NotContain(p => p.Level == 1);
        @class.SpellProgression.Min(p => p.Level).Should().Be(2);
    }

    [Theory]
    [InlineData("artificer", "TCE")]
    [InlineData("barbarian", "PHB")]
    [InlineData("bard", "PHB")]
    [InlineData("cleric", "PHB")]
    [InlineData("druid", "PHB")]
    [InlineData("fighter", "PHB")]
    [InlineData("monk", "PHB")]
    [InlineData("paladin", "PHB")]
    [InlineData("ranger", "PHB")]
    [InlineData("rogue", "PHB")]
    [InlineData("sorcerer", "PHB")]
    [InlineData("warlock", "PHB")]
    [InlineData("wizard", "PHB")]
    [InlineData("artificer-2024", "EFA")]
    [InlineData("barbarian-2024", "XPHB")]
    [InlineData("bard-2024", "XPHB")]
    [InlineData("cleric-2024", "XPHB")]
    [InlineData("druid-2024", "XPHB")]
    [InlineData("fighter-2024", "XPHB")]
    [InlineData("monk-2024", "XPHB")]
    [InlineData("paladin-2024", "XPHB")]
    [InlineData("ranger-2024", "XPHB")]
    [InlineData("rogue-2024", "XPHB")]
    [InlineData("sorcerer-2024", "XPHB")]
    [InlineData("warlock-2024", "XPHB")]
    [InlineData("wizard-2024", "XPHB")]
    public async Task Class_ComesFromItsBook(string classId, string source)
    {
        // Act
        var @class = await ClassAsync(classId);

        // Assert - the artificer is in neither Player's Handbook
        @class.Source.Should().Be(source);
    }

    [Theory]
    [InlineData("champion", "PHB")]
    [InlineData("champion-2024", "XPHB")]
    [InlineData("swashbuckler", "XGE")]
    [InlineData("alchemist", "TCE")]
    [InlineData("alchemist-2024", "EFA")]
    public async Task Subclass_ComesFromItsOwnBook(string subclassId, string source)
    {
        // Act
        var subclass = (await ReadAsync<SubclassSeedRow>("Subclasses.json")).Single(s => s.Id == subclassId);

        // Assert
        subclass.Source.Should().Be(source);
    }

    [Theory]
    [InlineData("eldritch-knight")]
    [InlineData("arcane-trickster")]
    public async Task ThirdCaster_KnowsThreeSpellsAtLevelThree(string subclassId)
    {
        // Arrange - the hardcoded table said two, one short of the book at every level to 16
        var subclasses = await ReadAsync<SubclassSeedRow>("Subclasses.json");

        // Act
        var row = subclasses.Single(s => s.Id == subclassId).SpellProgression.Single(p => p.Level == 3);

        // Assert
        (row.CantripsKnown, row.SpellsKnown, row.MaxSpellLevel).Should().Be((2, 3, 1));
    }

    [Theory]
    [InlineData("eldritch-knight", new[] { "Abjuration", "Evocation" }, new[] { 3, 8, 14, 20 })]
    [InlineData("arcane-trickster", new[] { "Enchantment", "Illusion" }, new[] { 3, 8, 14, 20 })]
    [InlineData("eldritch-knight-2024", new string[0], new int[0])]
    [InlineData("arcane-trickster-2024", new string[0], new int[0])]
    public async Task ThirdCaster_LimitsSpellSchoolsAsTheBookDoes(string subclassId, string[] schools, int[] anySchoolLevels)
    {
        // Arrange - 5etools keeps the 2014 limit only in the feature text; the 2024 rules have none
        var subclasses = await ReadAsync<SubclassSeedRow>("Subclasses.json");

        // Act
        var subclass = subclasses.Single(s => s.Id == subclassId);

        // Assert
        subclass.SpellSchools.Should().Equal(schools);
        subclass.AnySchoolSpellLevels.Should().Equal(anySchoolLevels);
    }

    [Fact]
    public async Task Subclasses_OnlyTheThirdCastersLimitSpellSchools()
    {
        // Act
        var limited = (await ReadAsync<SubclassSeedRow>("Subclasses.json")).Where(s => s.SpellSchools.Count > 0).Select(s => s.Id);

        // Assert
        limited.Should().BeEquivalentTo("eldritch-knight", "arcane-trickster");
    }

    [Theory]
    // How many Fighting Styles a character owes is counted from these features
    [InlineData("artificer", new int[0])]
    [InlineData("barbarian", new int[0])]
    [InlineData("bard", new int[0])]
    [InlineData("cleric", new int[0])]
    [InlineData("druid", new int[0])]
    [InlineData("fighter", new[] { 1 })]
    [InlineData("monk", new int[0])]
    [InlineData("paladin", new[] { 2 })]
    [InlineData("ranger", new[] { 2 })]
    [InlineData("rogue", new int[0])]
    [InlineData("sorcerer", new int[0])]
    [InlineData("warlock", new int[0])]
    [InlineData("wizard", new int[0])]
    [InlineData("artificer-2024", new int[0])]
    [InlineData("barbarian-2024", new int[0])]
    [InlineData("bard-2024", new int[0])]
    [InlineData("cleric-2024", new int[0])]
    [InlineData("druid-2024", new int[0])]
    [InlineData("fighter-2024", new[] { 1 })]
    [InlineData("monk-2024", new int[0])]
    [InlineData("paladin-2024", new[] { 2 })]
    [InlineData("ranger-2024", new[] { 2 })]
    [InlineData("rogue-2024", new int[0])]
    [InlineData("sorcerer-2024", new int[0])]
    [InlineData("warlock-2024", new int[0])]
    [InlineData("wizard-2024", new int[0])]
    // The subclasses that give one: College of Swords, and the Champion's Additional Fighting Style
    [InlineData("swords", new[] { 3 })]
    [InlineData("champion", new[] { 10 })]
    [InlineData("champion-2024", new[] { 7 })]
    public async Task ClassOrSubclass_GivesAFightingStyleAtTheBooksLevels(string ownerId, int[] levels)
    {
        // Arrange
        var features = await ReadAsync<FeatureSeedRow>("ClassFeatures.json");

        // Act
        var styleLevels = features
            .Where(f => (f.ClassId ?? f.SubclassId) == ownerId && f.Name is "Fighting Style" or "Additional Fighting Style")
            .Select(f => f.Level);

        // Assert
        styleLevels.Should().Equal(levels);
    }

    [Fact]
    public async Task Features_OnlyTheBooksClassesAndSubclassesGiveAFightingStyle()
    {
        // Act - the Mystic Arts monk's "Mystic Fighting Style" is another feature, and not counted
        var owners = (await ReadAsync<FeatureSeedRow>("ClassFeatures.json"))
            .Where(f => f.Name is "Fighting Style" or "Additional Fighting Style")
            .Select(f => f.ClassId ?? f.SubclassId);

        // Assert
        owners.Should().BeEquivalentTo(
            "fighter", "paladin", "ranger", "fighter-2024", "paladin-2024", "ranger-2024", "swords", "champion", "champion-2024");
    }

    [Theory]
    [InlineData("bard", 3, 10)]
    [InlineData("bard-2024", 3, 10)]
    [InlineData("monk", 1, 27)]
    [InlineData("monk-2024", 1, 27)]
    [InlineData("artificer", 1, 17)]
    [InlineData("artificer-2024", 1, 17)]
    [InlineData("rogue", 0, 0)]
    [InlineData("fighter-2024", 0, 0)]
    public async Task Class_ToolChoices_MatchTheBook(string classId, int count, int options)
    {
        // Act - the monk's artisan's tools or instrument is one pick from both kinds
        var @class = await ClassAsync(classId);

        // Assert
        @class.ToolChoices.Sum(c => c.Count).Should().Be(count);
        @class.ToolChoices.Sum(c => c.Options.Count).Should().Be(options);
    }

    [Fact]
    public async Task Bard_PicksThreeInstruments()
    {
        // Act
        var bard = await ClassAsync("bard");

        // Assert
        bard.ToolChoices.Should().ContainSingle().Which.Options.Should().Contain(["lute", "lyre", "bagpipes"]).And.NotContain("dice_set");
    }

    private static async Task<ClassSeedRow> ClassAsync(string id) =>
        (await ReadAsync<ClassSeedRow>("Classes.json")).Single(c => c.Id == id);

    private static Task<List<T>> ReadAsync<T>(string file) =>
        SeedFile.ReadListAsync<T>(Path.Combine(SeedService.DataPath, file), TestContext.Current.CancellationToken);
}
