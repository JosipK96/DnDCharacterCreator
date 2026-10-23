using Bunit;

using CharacterCreatorApp.Client.Pages.CharacterCreation;
using CharacterCreatorApp.Client.Pages.CharacterCreation.Components;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Requests.CharacterDrafts;
using CharacterCreatorApp.Contracts.Responses;

using FluentAssertions;

using static CharacterCreatorApp.Client.UnitTests.Fixtures.TestData;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

/// <summary>
/// The race, class and background steps show what the draft holds, so an edit of a saved
/// character (or a draft the player comes back to) goes through them unchanged with one click.
/// </summary>
public class SavedChoicesStepTests : WizardStepTestBase
{
    private int _movedOn;

    [Fact]
    public void RaceStep_WithARaceSaved_ShowsItAndSavesItUnchanged()
    {
        // Arrange - a high elf
        Api.Get(DraftPath(), new CharacterDraftDetailsApiResponse { Id = DraftId, RaceId = "elf", SubraceId = "elf_high" });
        Api.Get("/api/races/get-list?edition=2014", new List<RaceApiResponse>
        {
            Race("dwarf", "Dwarf"),
            Race("elf", "Elf", subraces: [Subrace("elf_wood", "Wood Elf", "elf"), Subrace("elf_high", "High Elf", "elf")]),
        });
        Api.Put(DraftPath("race"));

        // Act
        var cut = Render<RaceStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 2)
            .Add(p => p.Edition, "2014")
            .Add(p => p.OnNext, () => _movedOn++));
        Button(cut, "Next: Class").Click();

        // Assert
        var sent = Api.BodyOf<SetRaceApiRequest>(SaveRoute("race"));
        (sent.RaceId, sent.SubraceId).Should().Be(("elf", "elf_high"));
        _movedOn.Should().Be(1);
    }

    [Fact]
    public void ClassStep_WithAClassAndSubclassSaved_ShowsThemAndSavesThemUnchanged()
    {
        // Arrange - a level 3 fighter, a Champion
        Api.Get(DraftPath(), new CharacterDraftDetailsApiResponse { Id = DraftId, ClassId = "fighter", SubclassId = "champion" });
        Api.Get("/api/classes/get-list?edition=2014", new List<ClassApiResponse> { Class("wizard", "Wizard"), Class("fighter", "Fighter") });
        Api.Get("/api/subclasses?classId=fighter", new List<SubclassApiResponse>
        {
            new("battle_master", "Battle Master", "fighter", "PHB", null, []),
            new("champion", "Champion", "fighter", "PHB", null, []),
        });
        Api.Put(DraftPath("class"));

        // Act
        var cut = Render<ClassStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 3)
            .Add(p => p.CharacterLevel, 3)
            .Add(p => p.Edition, "2014")
            .Add(p => p.OnNext, () => _movedOn++));
        Button(cut, "Next: Background").Click();

        // Assert
        var sent = Api.BodyOf<SetClassApiRequest>(SaveRoute("class"));
        (sent.ClassId, sent.SubclassId).Should().Be(("fighter", "champion"));
        _movedOn.Should().Be(1);
    }

    [Fact]
    public void ClassStep_InALevelUpToTheSubclassLevel_KeepsTheClassAndAsksOnlyForTheSubclass()
    {
        // Arrange - a fighter levelled up to 3; its class cannot change, its subclass is picked now
        Api.Get(DraftPath(), new CharacterDraftDetailsApiResponse { Id = DraftId, ClassId = "fighter" });
        Api.Get("/api/classes/get-list?edition=2014", new List<ClassApiResponse> { Class("wizard", "Wizard"), Class("fighter", "Fighter") });
        Api.Get("/api/subclasses?classId=fighter", new List<SubclassApiResponse>
        {
            new("battle_master", "Battle Master", "fighter", "PHB", null, []),
            new("champion", "Champion", "fighter", "PHB", null, []),
        });
        Api.Put(DraftPath("class"));

        var cut = Render<ClassStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 3)
            .Add(p => p.Flow, WizardSteps.LevelUpOf(["hit-points", "subclass"]))
            .Add(p => p.CharacterLevel, 3)
            .Add(p => p.Edition, "2014")
            .Add(p => p.ClassLocked, true)
            .Add(p => p.OnNext, () => _movedOn++));

        // Act
        Button(cut, "Champion").Click();
        Button(cut, "Next: Review").Click();

        // Assert
        cut.Find("h2").TextContent.Should().Be("Step 2: Choose Your Class");
        Buttons(cut, "Change").Should().BeEmpty("a level-up keeps the class");
        var sent = Api.BodyOf<SetClassApiRequest>(SaveRoute("class"));
        (sent.ClassId, sent.SubclassId).Should().Be(("fighter", "champion"));
        _movedOn.Should().Be(1);
    }

    [Fact]
    public void BackgroundStep_With2024IncreasesSaved_ShowsThemAndSavesThemUnchanged()
    {
        // Arrange - a soldier with +2 Strength and +1 Constitution
        var soldier = Background("soldier_2024", "Soldier") with { AbilityScoreOptions = ["STR", "DEX", "CON"] };
        Api.Get(DraftPath(), new CharacterDraftDetailsApiResponse
        {
            Id = DraftId,
            BackgroundId = "soldier_2024",
            BackgroundAbilityIncreases = new Dictionary<string, int> { ["STR"] = 2, ["CON"] = 1 },
        });
        Api.Get("/api/backgrounds?edition=2024", new List<BackgroundApiResponse> { Background("sage_2024", "Sage"), soldier });
        Api.Put(DraftPath("background"));

        // Act
        var cut = Render<BackgroundStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 4)
            .Add(p => p.Edition, "2024")
            .Add(p => p.OnNext, () => _movedOn++));
        Button(cut, "Next: Languages").Click();

        // Assert
        var sent = Api.BodyOf<SetBackgroundApiRequest>(SaveRoute("background"));
        sent.BackgroundId.Should().Be("soldier_2024");
        sent.AbilityIncreases.Should().Equal(new Dictionary<string, int> { ["STR"] = 2, ["CON"] = 1 });
        _movedOn.Should().Be(1);
    }
}
