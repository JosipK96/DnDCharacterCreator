using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterSheetPdf;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class GetCharacterSheetPdfQueryHandlerTests : IDisposable
{
    private readonly Mock<ICharacterRepository> _characterRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly Mock<IProficiencyRepository> _proficiencyRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFeatureRepository> _featureRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ISpellRepository> _spellRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ILanguageRepository> _languageRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFeatRepository> _featRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IRaceRepository> _raceRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IBackgroundRepository> _backgroundRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ICharacterSheetPdfRenderer> _rendererMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly GetCharacterSheetPdfQueryHandler _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _characterId = Guid.NewGuid();

    public GetCharacterSheetPdfQueryHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);

        _sut = new GetCharacterSheetPdfQueryHandler(
            _characterRepositoryMock.Object,
            _userContextMock.Object,
            _proficiencyRepositoryMock.Object,
            _featureRepositoryMock.Object,
            _spellRepositoryMock.Object,
            _languageRepositoryMock.Object,
            _featRepositoryMock.Object,
            _classRepositoryMock.Object,
            _raceRepositoryMock.Object,
            _backgroundRepositoryMock.Object,
            _rendererMock.Object);
    }

    [Fact]
    public async Task Handle_WithTheirCharacter_RendersTheSheetOnThePaperAndNamesTheFile()
    {
        // Arrange
        ArrangeCharacter("Thorin");
        byte[] pdf = [1, 2, 3];
        _rendererMock
            .Setup(r => r.Render(It.Is<CharacterDetailsReadModel>(c => c.Id == _characterId && c.Name == "Thorin"), SheetPaper.Letter))
            .Returns(pdf);

        // Act
        var result = await _sut.Handle(new GetCharacterSheetPdfQuery(_characterId, SheetPaper.Letter), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().Be("Thorin.pdf");
        result.Value.Content.Should().Equal(pdf);
    }

    [Theory]
    [InlineData("  Thorin Oakenshield  ", "Thorin Oakenshield.pdf")]
    [InlineData("Éowyn-the_Fair 2", "Éowyn-the_Fair 2.pdf")]
    [InlineData("a/b\\c:d\"e\r\nf", "a_b_c_d_e__f.pdf")]
    [InlineData("Thorin.pdf", "Thorin_pdf.pdf")]
    [InlineData("   ", "character.pdf")]
    [InlineData("", "character.pdf")]
    public async Task Handle_WithANameAFileMightRefuse_StripsItForTheFileName(string name, string fileName)
    {
        // Arrange
        ArrangeCharacter(name);
        _rendererMock
            .Setup(r => r.Render(It.IsAny<CharacterDetailsReadModel>(), SheetPaper.A4))
            .Returns([]);

        // Act
        var result = await _sut.Handle(new GetCharacterSheetPdfQuery(_characterId, SheetPaper.A4), _cts.Token);

        // Assert
        result.Value.FileName.Should().Be(fileName);
    }

    [Fact]
    public async Task Handle_WhenTheCharacterIsNotTheirs_ReturnsNotFoundAndRendersNothing()
    {
        // Arrange
        _characterRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(_characterId, _userId, _cts.Token))
            .ReturnsAsync((CharacterDetailsReadModel?)null);

        // Act
        var result = await _sut.Handle(new GetCharacterSheetPdfQuery(_characterId, SheetPaper.A4), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("Character.NotFound");
        _rendererMock.Verify(r => r.Render(It.IsAny<CharacterDetailsReadModel>(), It.IsAny<SheetPaper>()), Times.Never);
    }

    private void ArrangeCharacter(string name)
    {
        _characterRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(_characterId, _userId, _cts.Token))
            .ReturnsAsync(new CharacterDetailsReadModel
            {
                Id = _characterId,
                Name = name,
                Level = 1,
                RaceId = "dwarf",
                ClassId = "fighter",
                BackgroundId = "soldier",
                AbilityScores = new Dictionary<string, int> { ["STR"] = 15, ["DEX"] = 14, ["CON"] = 14, ["INT"] = 12, ["WIS"] = 10, ["CHA"] = 8 },
                ProficiencyIds = ["athletics"],
                LanguageIds = ["common-phb"],
                FeatIds = ["tough"],
            });
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("fighter", _cts.Token)).ReturnsAsync(new Class
        {
            Id = "fighter",
            Name = "Fighter",
            Edition = RulesEdition.Edition2014,
            Source = "PHB",
            HitDie = "1d10",
            SubclassLevel = 3,
            AsiLevels = [4],
            SavingThrowAbilityIds = ["STR", "CON"],
            ClassProficiencies = [],
            Features = [],
            Subclasses = [],
        });
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("dwarf", _cts.Token)).ReturnsAsync(new Race
        {
            Id = "dwarf",
            Name = "dwarf",
            Edition = RulesEdition.Edition2014,
            Source = "PHB",
            Size = "Medium",
            Speed = 25,
            AbilityBonuses = [],
            RaceLanguages = [],
            RaceProficiencies = [],
            Traits = [],
            Subraces = [],
        });
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("soldier", _cts.Token)).ReturnsAsync(new Background
        {
            Id = "soldier",
            Name = "Soldier",
            Edition = RulesEdition.Edition2014,
            Source = "PHB",
            FeatureId = "military-rank",
            Languages = [],
            BackgroundProficiencies = [],
        });
        _proficiencyRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<List<string>>(), _cts.Token))
            .ReturnsAsync([new ProficiencyReadModel { Id = "athletics", Name = "Athletics", Type = "skill" }]);
        _featureRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.Is<List<string>>(ids => ids.Contains("military-rank")), _cts.Token))
            .ReturnsAsync([new FeatureReadModel { Id = "military-rank", Name = "Military Rank", Description = "Rank" }]);
        _languageRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token))
            .ReturnsAsync([new LanguageReadModel { Id = "common-phb", Name = "Common", Type = "standard" }]);
        _featRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token))
            .ReturnsAsync([new FeatReadModel { Id = "tough", Name = "Tough", Description = "Hardy", Edition = RulesEdition.Edition2014 }]);
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
