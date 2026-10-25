using Bunit;

using CharacterCreatorApp.Client.Pages.Characters;
using CharacterCreatorApp.Client.Services;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Responses;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace CharacterCreatorApp.Client.UnitTests.Pages;

/// <summary>The character's sheet page: downloading it as a PDF.</summary>
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

    private static CharacterDetailsApiResponse Character() => new(
        CharacterId, "Bruenor", 1, "2014", "dwarf", null, "fighter", null, "soldier",
        "Dwarf", null, "Fighter", null, "Soldier", "Medium",
        [], [], [], [], [], [], [], null);
}
