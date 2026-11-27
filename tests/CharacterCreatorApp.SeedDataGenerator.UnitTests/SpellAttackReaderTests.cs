using System.Text.Json.Nodes;

using FluentAssertions;

namespace CharacterCreatorApp.SeedDataGenerator.UnitTests;

/// <summary>The spells are 5etools' entries, shortened to the keys that matter.</summary>
public class SpellAttackReaderTests
{
    private const string FireBolt = """
        {"name": "Fire Bolt", "level": 0, "spellAttack": ["R"], "damageInflict": ["fire"],
         "scalingLevelDice": {"label": "fire damage", "scaling": {"1": "1d10", "5": "2d10", "11": "3d10", "17": "4d10"}}}
        """;

    [Fact]
    public void FireBolt_IsARangedSpellAttackOfFireWithItsDiceAtEachTier()
    {
        // Arrange
        var spell = Spell(FireBolt);

        // Act & Assert
        SpellAttackReader.AttackType(spell).Should().Be("ranged");
        SpellAttackReader.SaveAbilityId(spell).Should().BeNull();
        SpellAttackReader.DamageType(spell).Should().Be("fire");
        SpellAttackReader.CantripDice(spell).Should().Equal("1d10", "2d10", "3d10", "4d10");
    }

    [Fact]
    public void SacredFlame_CallsForADexteritySave()
    {
        // Arrange
        var spell = Spell("""
            {"name": "Sacred Flame", "level": 0, "savingThrow": ["dexterity"], "damageInflict": ["radiant"],
             "scalingLevelDice": {"label": "radiant damage", "scaling": {"1": "1d8", "5": "2d8", "11": "3d8", "17": "4d8"}}}
            """);

        // Act & Assert
        SpellAttackReader.AttackType(spell).Should().BeNull();
        SpellAttackReader.SaveAbilityId(spell).Should().Be("DEX");
        SpellAttackReader.CantripDice(spell).Should().Equal("1d8", "2d8", "3d8", "4d8");
    }

    [Fact]
    public void ShockingGrasp_IsAMeleeSpellAttack()
    {
        // Act
        var attackType = SpellAttackReader.AttackType(Spell("""{"level": 0, "spellAttack": ["M"]}"""));

        // Assert
        attackType.Should().Be("melee");
    }

    [Fact]
    public void SorcerousBurst_JoinsTheDamageTypesToPickFrom()
    {
        // Act
        var damageType = SpellAttackReader.DamageType(Spell("""{"level": 0, "damageInflict": ["acid", "cold", "fire"]}"""));

        // Assert
        damageType.Should().Be("acid/cold/fire");
    }

    [Fact]
    public void TollTheDead_TakesItsFirstScalingEntry()
    {
        // Arrange - the second is the damage against a wounded creature
        var spell = Spell("""
            {"level": 0, "savingThrow": ["wisdom"], "damageInflict": ["necrotic"],
             "scalingLevelDice": [
               {"label": "necrotic damage", "scaling": {"1": "1d8", "5": "2d8", "11": "3d8", "17": "4d8"}},
               {"label": "necrotic damage to wounded creature", "scaling": {"1": "1d12", "5": "2d12", "11": "3d12", "17": "4d12"}}]}
            """);

        // Act & Assert
        SpellAttackReader.CantripDice(spell).Should().Equal("1d8", "2d8", "3d8", "4d8");
    }

    [Fact]
    public void EldritchBlast_WithNoScalingEntry_KeepsTheDiceOfItsTextAtEveryLevel()
    {
        // Arrange - its beams are separate attacks, each 1d10
        var spell = Spell("""
            {"level": 0, "spellAttack": ["R"], "damageInflict": ["force"],
             "entries": ["Make a ranged spell attack. On a hit, the target takes {@damage 1d10} force damage."]}
            """);

        // Act & Assert
        SpellAttackReader.CantripDice(spell).Should().Equal("1d10", "1d10", "1d10", "1d10");
    }

    [Theory]
    // Booming Blade: the damage on a hit only starts at level 5, added to a weapon's
    [InlineData("""
        {"level": 0, "spellAttack": ["M"], "damageInflict": ["thunder"], "scalingLevelDice": [
          {"label": "thunder damage on moving", "scaling": {"1": "1d8", "5": "2d8", "11": "3d8", "17": "4d8"}},
          {"label": "thunder damage on hit", "scaling": {"5": "1d8", "11": "2d8", "17": "3d8"}}]}
        """)]
    // Green-Flame Blade: the spellcasting modifier is not plain dice
    [InlineData("""
        {"level": 0, "spellAttack": ["M"], "damageInflict": ["fire"], "scalingLevelDice": [
          {"label": "fire damage to secondary creature", "scaling": {"1": "{{spellcasting_mod}}", "5": "1d8 + {{spellcasting_mod}}", "11": "2d8 + {{spellcasting_mod}}", "17": "3d8 + {{spellcasting_mod}}"}},
          {"label": "fire damage on hit", "scaling": {"5": "1d8", "11": "2d8", "17": "3d8"}}]}
        """)]
    // A levelled spell: its damage depends on the slot
    [InlineData("""{"level": 1, "spellAttack": ["R"], "damageInflict": ["radiant"], "entries": ["{@damage 4d6} radiant damage"]}""")]
    // Light: a save, but no damage
    [InlineData("""{"level": 0, "savingThrow": ["dexterity"], "entries": ["The object sheds bright light."]}""")]
    public void CantripDice_ForAWeaponCantripALevelledSpellOrNoDamage_IsEmpty(string json)
    {
        // Act
        var dice = SpellAttackReader.CantripDice(Spell(json));

        // Assert
        dice.Should().BeEmpty();
    }

    [Fact]
    public void ASpellWithNoAttackSaveOrDamage_HasNone()
    {
        // Arrange
        var spell = Spell("""{"level": 0}""");

        // Act & Assert
        (SpellAttackReader.AttackType(spell), SpellAttackReader.SaveAbilityId(spell), SpellAttackReader.DamageType(spell))
            .Should().Be((null, null, null));
    }

    private static JsonObject Spell(string json) => JsonNode.Parse(json)!.AsObject();
}
