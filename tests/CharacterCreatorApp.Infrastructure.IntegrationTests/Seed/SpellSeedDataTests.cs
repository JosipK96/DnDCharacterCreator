using CharacterCreatorApp.Infrastructure.Seed;
using CharacterCreatorApp.Infrastructure.Seed.Rows;

using FluentAssertions;

namespace CharacterCreatorApp.Infrastructure.IntegrationTests.Seed;

/// <summary>
/// The spells the generator writes from 5etools, checked against the books. No database is
/// involved.
/// </summary>
public class SpellSeedDataTests
{
    [Theory]
    // A spell reprinted in 2024 is two spells, each on its own edition's lists
    [InlineData("fireball", "wizard", true)]
    [InlineData("fireball", "wizard-2024", false)]
    [InlineData("fireball_2024", "wizard-2024", true)]
    [InlineData("fireball_2024", "wizard", false)]
    // One the 2024 book did not reprint is on both editions' lists
    [InlineData("absorb_elements", "wizard", true)]
    [InlineData("absorb_elements", "wizard-2024", true)]
    // Toll the Dead was reprinted, so the 2024 wizard gets the reprint
    [InlineData("toll_the_dead", "wizard-2024", false)]
    [InlineData("toll_the_dead_2024", "wizard-2024", true)]
    public async Task Spell_IsOnTheClassListsOfItsEdition(string spellId, string classId, bool onTheList)
    {
        // Act
        var rows = await ReadAsync<SpellClassSeedRow>("SpellClasses.json");

        // Assert
        rows.Any(r => r.SpellId == spellId && r.ClassId == classId).Should().Be(onTheList);
    }

    [Fact]
    public async Task Fireball_HasItsMaterialAndWhatAHigherSlotDoes()
    {
        // Act - the hand-made data had neither
        var fireball = (await ReadAsync<SpellSeedRow>("Spells.json")).Single(s => s.Id == "fireball");

        // Assert
        fireball.Material.Should().Be("a tiny ball of bat guano and sulfur");
        fireball.Description.Should().EndWith("the damage increases by 1d6 for each slot level above 3rd.");
    }

    [Theory]
    [InlineData("alarm", true, false)]
    [InlineData("guidance", false, true)]
    [InlineData("fireball", false, false)]
    public async Task Spell_IsARitualOrConcentrationAsTheBookSays(string spellId, bool ritual, bool concentration)
    {
        // Act - the hand-made data marked most rituals and concentration spells as neither
        var spell = (await ReadAsync<SpellSeedRow>("Spells.json")).Single(s => s.Id == spellId);

        // Assert
        (spell.IsRitual, spell.IsConcentration).Should().Be((ritual, concentration));
    }

    [Fact]
    public async Task LifeDomain_HasItsDomainSpellsPreparedAtTheirLevels()
    {
        // Act
        var rows = (await ReadAsync<SpellSubclassSeedRow>("SpellSubclasses.json")).Where(r => r.SubclassId == "life").ToList();

        // Assert
        rows.Should().OnlyContain(r => r.IsAlwaysPrepared);
        rows.Where(r => r.Level == 1).Select(r => r.SpellId).Should().BeEquivalentTo(["bless", "cure_wounds"]);
        rows.Where(r => r.Level == 3).Select(r => r.SpellId).Should().BeEquivalentTo(["lesser_restoration", "spiritual_weapon"]);
    }

    [Fact]
    public async Task FiendPatron2014_AddsItsSpellsToTheListWithoutPreparingThem()
    {
        // Act
        var rows = (await ReadAsync<SpellSubclassSeedRow>("SpellSubclasses.json")).Where(r => r.SubclassId == "fiend").ToList();

        // Assert - the 2014 warlock learns them like any other; the 2024 one has them prepared
        rows.Should().Contain(r => r.SpellId == "burning_hands").And.OnlyContain(r => !r.IsAlwaysPrepared);
    }

    [Theory]
    [InlineData("eldritch-knight", "wizard")]
    [InlineData("arcane-trickster", "wizard")]
    [InlineData("divine-soul", "cleric")]
    [InlineData("eldritch-knight-2024", "wizard-2024")]
    [InlineData("arcane-trickster-2024", "wizard-2024")]
    public async Task Subclass_OpensAnotherClassesSpellList(string subclassId, string classId)
    {
        // Act
        var subclass = (await ReadListAsync<SubclassSeedRow>("Subclasses.json")).Single(s => s.Id == subclassId);

        // Assert
        subclass.SpellListClassId.Should().Be(classId);
    }

    private static Task<List<T>> ReadAsync<T>(string file) =>
        SeedFile.ReadListAsync<T>(Path.Combine(SeedService.DataPath, "Spells", file), TestContext.Current.CancellationToken);

    private static Task<List<T>> ReadListAsync<T>(string file) =>
        SeedFile.ReadListAsync<T>(Path.Combine(SeedService.DataPath, file), TestContext.Current.CancellationToken);
}
