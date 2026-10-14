using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.Services;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

namespace CharacterCreatorApp.Domain.UnitTests;

public class CharacterFeaturesTests
{
    // A fighter with a class feature at levels 1, 2 and 3, the Champion's at 3 and 7, and a
    // subclass feature the class itself lists too, which the data does for some of them
    private static readonly Subclass Champion = new()
    {
        Id = "champion",
        Name = "Champion",
        ClassId = "fighter",
        Features =
        [
            FeatureAt("champion-3-improved-critical", 3, subclassId: "champion"),
            FeatureAt("champion-7-remarkable-athlete", 7, subclassId: "champion")
        ]
    };

    private static readonly Class Fighter = new()
    {
        Id = "fighter",
        Name = "Fighter",
        Edition = RulesEdition.Edition2014,
        HitDie = "1d10",
        Features =
        [
            FeatureAt("fighter-1-second-wind", 1),
            FeatureAt("fighter-2-action-surge", 2),
            FeatureAt("fighter-3-martial-archetype", 3),
            FeatureAt("champion-3-improved-critical", 3, subclassId: "champion")
        ],
        Subclasses = [Champion]
    };

    private static readonly Background Soldier = new()
    {
        Id = "soldier",
        Name = "Soldier",
        Edition = RulesEdition.Edition2014,
        FeatureId = "soldier-military-rank"
    };

    // A 2024 background has an origin feat where the 2014 one had a feature
    private static readonly Background Soldier2024 = new()
    {
        Id = "soldier_2024",
        Name = "Soldier",
        Edition = RulesEdition.Edition2024,
        AbilityScoreOptions = ["STR", "DEX", "CON"],
        FeatId = "savage_attacker_2024"
    };

    [Fact]
    public void IdsFor_WithA2024BackgroundWithoutAFeature_ReturnsTheClassFeaturesAlone()
    {
        // Act
        var ids = CharacterFeatures.IdsFor(Fighter, subclass: null, Soldier2024, level: 1);

        // Assert
        ids.Should().Equal("fighter-1-second-wind");
    }

    [Fact]
    public void IdsFor_ReturnsTheClassAndSubclassFeaturesUpToTheLevelAndTheBackgrounds()
    {
        // Act
        var ids = CharacterFeatures.IdsFor(Fighter, Champion, Soldier, level: 3);

        // Assert
        ids.Should().Equal(
            "fighter-1-second-wind",
            "fighter-2-action-surge",
            "fighter-3-martial-archetype",
            "champion-3-improved-critical",
            "soldier-military-rank");
    }

    [Fact]
    public void IdsFor_WithoutASubclass_LeavesOutSubclassFeaturesTheClassAlsoLists()
    {
        // Act - a level 3 fighter who has not picked an archetype yet
        var ids = CharacterFeatures.IdsFor(Fighter, subclass: null, Soldier, level: 3);

        // Assert
        ids.Should().Equal(
            "fighter-1-second-wind",
            "fighter-2-action-surge",
            "fighter-3-martial-archetype",
            "soldier-military-rank");
    }

    [Fact]
    public void IdsFor_AtLevelOne_HasOnlyTheFirstLevelsFeatures()
    {
        // Act
        var ids = CharacterFeatures.IdsFor(Fighter, Champion, Soldier, level: 1);

        // Assert
        ids.Should().Equal("fighter-1-second-wind", "soldier-military-rank");
    }

    private static Feature FeatureAt(string id, int level, string? subclassId = null) => new()
    {
        Id = id,
        Name = id,
        Description = string.Empty,
        Level = level,
        ClassId = "fighter",
        SubclassId = subclassId,
        Type = subclassId == null ? "class" : "subclass"
    };
}
