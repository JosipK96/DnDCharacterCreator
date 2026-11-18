using Bunit;

using CharacterCreatorApp.Client.Pages.Characters;
using CharacterCreatorApp.Client.Services;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Responses;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace CharacterCreatorApp.Client.UnitTests.Pages;

/// <summary>The character's sheet page: its numbers as the API sends them, deleting, and downloading it as a PDF.</summary>
public class CharacterDetailsTests : WizardStepTestBase
{
    private static readonly Guid CharacterId = Guid.Parse("3f1c7a2e-5b8d-4e6f-9a0b-1c2d3e4f5a6b");
    private static readonly byte[] Pdf = "%PDF-1.7 a sheet"u8.ToArray();

    public CharacterDetailsTests()
    {
        Services.AddSingleton(new CharacterService(Api.CreateClient()));
        Api.Get($"/api/characters/{CharacterId}", Character());
    }

    [Theory]
    [InlineData(null, "a4")]
    [InlineData("letter", "letter")]
    public void DownloadPdf_AsksForTheSheetOnThePaperPickedAndSavesItUnderTheApisName(string? picked, string paper)
    {
        // Arrange
        Api.GetFile($"/api/characters/{CharacterId}/sheet.pdf?paper={paper}", "Bruenor.pdf", "application/pdf", Pdf);
        var save = JSInterop.SetupVoid("saveFile", _ => true);
        var cut = Render<CharacterDetails>(parameters => parameters.Add(p => p.Id, CharacterId));

        if (picked != null)
        {
            cut.Find("select[aria-label='Paper size']").Change(picked);
        }

        // Act
        Button(cut, "Download PDF").Click();

        // Assert
        var invocation = save.VerifyInvoke("saveFile");
        invocation.Arguments.Should().HaveCount(3);
        invocation.Arguments[0].Should().Be("Bruenor.pdf");
        invocation.Arguments[1].Should().Be("application/pdf");
        invocation.Arguments[2].Should().BeEquivalentTo(Pdf);
    }

    [Fact]
    public void Render_WithStats_ShowsTheSixCombatNumbersAsTheApiSendsThem()
    {
        // Arrange
        Api.Get($"/api/characters/{CharacterId}", Character(Stats()));

        // Act
        var cut = Render<CharacterDetails>(parameters => parameters.Add(p => p.Id, CharacterId));

        // Assert
        cut.FindAll("[data-testid='combat-number']")
            .Select(n => string.Join(' ', n.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)))
            .Should().Equal(
                "Hit points 12", "Armor class 13", "Initiative +1", "Speed 25 ft", "Hit dice 1d10", "Proficiency bonus +2");
    }

    [Fact]
    public void Render_WithStats_PutsEachAbilitysModifierInItsTab()
    {
        // Arrange
        Api.Get($"/api/characters/{CharacterId}", Character(Stats()));

        // Act
        var cut = Render<CharacterDetails>(parameters => parameters.Add(p => p.Id, CharacterId));

        // Assert
        var strength = cut.Find("[data-testid='ability'][data-ability='STR']");
        strength.TextContent.Should().Contain("Strength").And.Contain("16");
        strength.QuerySelector("[data-testid='modifier']")!.TextContent.Should().Be("+3");
    }

    [Fact]
    public void Render_WithStats_MarksOnlyTheProficientSavingThrowsAndSkills()
    {
        // Arrange
        Api.Get($"/api/characters/{CharacterId}", Character(Stats()));

        // Act
        var cut = Render<CharacterDetails>(parameters => parameters.Add(p => p.Id, CharacterId));

        // Assert
        Proficiency(cut, "saving-throw").Should().Equal(("Strength", "true"), ("Dexterity", "false"));
        Proficiency(cut, "skill").Should().Equal(("Athletics", "true"), ("Stealth", "false"));
    }

    [Fact]
    public void Delete_AsksInADialogAndCancelClosesItWithoutDeleting()
    {
        // Arrange
        var cut = Render<CharacterDetails>(parameters => parameters.Add(p => p.Id, CharacterId));

        // Act
        Button(cut, "Delete character").Click();
        var dialog = cut.Find("[role='dialog']");
        var dialogText = dialog.TextContent;
        var dialogButtons = dialog.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToList();
        Button(cut, "Cancel").Click();

        // Assert
        dialogText.Should().Contain("Bruenor");
        dialogButtons.Should().Equal("Cancel", "Delete");
        cut.FindAll("[role='dialog']").Should().BeEmpty();
        Api.Requests.Should().NotContain(r => r.StartsWith("DELETE", StringComparison.Ordinal));
    }

    // Each row's name (without a skill's ability after it) and its proficiency mark
    private static List<(string Name, string Proficient)> Proficiency(IRenderedComponent<CharacterDetails> cut, string testId) =>
        cut.FindAll($"[data-testid='{testId}']")
            .Select(row => (
                row.QuerySelector("[data-testid='name']")!.FirstChild!.TextContent.Trim(),
                row.QuerySelector("[data-proficient]")!.GetAttribute("data-proficient")!))
            .ToList();

    private static CharacterStatsApiResponse Stats() => new(
        ProficiencyBonus: 2,
        MaxHitPoints: 12,
        ArmorClass: 13,
        Initiative: 1,
        Speed: 25,
        HitDice: "1d10",
        PassivePerception: 11,
        Abilities: [new("STR", 16, 3), new("DEX", 12, 1)],
        SavingThrows: [new("STR", 5, true), new("DEX", 1, false)],
        Skills: [new("athletics", "Athletics", "STR", 5, true), new("stealth", "Stealth", "DEX", 1, false)],
        SpellSlots: [],
        PactMagic: null);

    private static CharacterDetailsApiResponse Character(CharacterStatsApiResponse? stats = null) => new(
        CharacterId, "Bruenor", 1, "2014", "dwarf", null, "fighter", null, "soldier",
        "Dwarf", null, "Fighter", null, "Soldier", "Medium",
        [], [], [], [], [], [], [], stats);
}
