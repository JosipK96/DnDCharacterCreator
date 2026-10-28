using CharacterCreatorApp.Application.Features.CharacterCreation.Building;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class CharacterSheetContentLoaderTests : IDisposable
{
    private readonly Mock<IProficiencyRepository> _proficiencyRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFeatureRepository> _featureRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ISpellRepository> _spellRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ILanguageRepository> _languageRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFeatRepository> _featRepositoryMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly CharacterSheetContentLoader _sut;

    public CharacterSheetContentLoaderTests()
    {
        _sut = new CharacterSheetContentLoader(
            _proficiencyRepositoryMock.Object,
            _featureRepositoryMock.Object,
            _spellRepositoryMock.Object,
            _languageRepositoryMock.Object,
            _featRepositoryMock.Object);

        _proficiencyRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<string>>(), _cts.Token)).ReturnsAsync([]);
        _featureRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<string>>(), _cts.Token)).ReturnsAsync([]);
        _languageRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token)).ReturnsAsync([]);
    }

    [Fact]
    public async Task LoadAsync_WithoutSpells_DoesNotAskForThem()
    {
        // Arrange - the strict spell repository fails the test if it is called
        _featRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token)).ReturnsAsync([]);

        // Act
        var content = await _sut.LoadAsync([], [], [], [], [], [], _cts.Token);

        // Assert
        content.Spells.Should().BeEmpty();
        content.Feats.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_WithAFeatTakenTwice_ListsItTwiceWithTheSpellsPickedForEachTake()
    {
        // Arrange
        _featRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token))
            .ReturnsAsync([new FeatReadModel { Id = "magic_initiate", Name = "Magic Initiate", Description = "Spells", Edition = RulesEdition.Edition2014 }]);
        _spellRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<List<string>>(), _cts.Token))
            .ReturnsAsync(
            [
                Spell("light", "Light"),
                Spell("mage_hand", "Mage Hand")
            ]);

        // Act
        var content = await _sut.LoadAsync(
            [],
            [],
            ["light", "mage_hand"],
            [],
            ["magic_initiate", "magic_initiate"],
            [new FeatSpellPick { FeatId = "magic_initiate", ClassId = "wizard", SpellIds = ["light"] }, new FeatSpellPick { FeatId = "magic_initiate", ClassId = "wizard", SpellIds = ["mage_hand"] }],
            _cts.Token);

        // Assert
        content.Feats.Should().HaveCount(2);
        content.Feats[0].Spells.Select(s => s.Id).Should().Equal("light");
        content.Feats[1].Spells.Select(s => s.Id).Should().Equal("mage_hand");
    }

    private static SpellReadModel Spell(string id, string name) => new()
    {
        Id = id,
        Name = name,
        School = "Evocation",
        CastingTimeUnit = "action",
        RangeType = "point",
        DurationType = "instant",
        Description = "-"
    };

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
