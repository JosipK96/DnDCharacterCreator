using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetBackground;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class SetCharacterBackgroundCommandHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IBackgroundRepository> _backgroundRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFeatRepository> _featRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly SetCharacterBackgroundCommandHandler _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _draftId = Guid.NewGuid();

    public SetCharacterBackgroundCommandHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        _sut = new SetCharacterBackgroundCommandHandler(
            _draftRepositoryMock.Object,
            _backgroundRepositoryMock.Object,
            _featRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _userContextMock.Object);
    }

    [Theory]
    [InlineData("STR:2 CON:1")]
    [InlineData("STR:1 DEX:1 CON:1")]
    public async Task Handle_With2024BackgroundAndIncreasesItAllows_SavesBoth(string increasesText)
    {
        // Arrange - +2 and +1, or +1 to each of the three
        var draft = ArrangeDraft(RulesEdition.Edition2024);
        ArrangeBackground(Soldier2024());
        var increases = increasesText.Split(' ')
            .Select(pair => pair.Split(':'))
            .ToDictionary(parts => parts[0], parts => int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));

        // Act
        var result = await _sut.Handle(new SetCharacterBackgroundCommand(_draftId, "soldier_2024", increases), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.BackgroundId.Should().Be("soldier_2024");
        draft.BackgroundAbilityIncreases.Should().BeEquivalentTo(increases);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_With2024BackgroundAndIncreasesItDoesNotAllow_ReturnsInvalidBackgroundAbilityIncreases()
    {
        // Arrange - Wisdom is not one of the soldier's three
        var draft = ArrangeDraft(RulesEdition.Edition2024);
        ArrangeBackground(Soldier2024());

        // Act
        var result = await _sut.Handle(
            new SetCharacterBackgroundCommand(_draftId, "soldier_2024", new Dictionary<string, int> { ["STR"] = 2, ["WIS"] = 1 }), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.InvalidBackgroundAbilityIncreases");
        result.Errors[0].Description.Should().Be("Soldier raises Strength, Dexterity, Constitution: +2 to one and +1 to another, or +1 to each.");
        draft.BackgroundId.Should().BeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_With2014BackgroundAndIncreases_ReturnsInvalidBackgroundAbilityIncreases()
    {
        // Arrange - a 2014 character's race raises its abilities
        var draft = ArrangeDraft(RulesEdition.Edition2014);
        ArrangeBackground(Soldier2014());

        // Act
        var result = await _sut.Handle(
            new SetCharacterBackgroundCommand(_draftId, "soldier", new Dictionary<string, int> { ["STR"] = 2, ["CON"] = 1 }), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Description.Should().Be("Soldier raises no ability scores; the race does.");
        draft.BackgroundId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithABackgroundOfTheOtherEdition_ReturnsEditionMismatch()
    {
        // Arrange
        var draft = ArrangeDraft(RulesEdition.Edition2014);
        ArrangeBackground(Soldier2024());

        // Act
        var result = await _sut.Handle(
            new SetCharacterBackgroundCommand(_draftId, "soldier_2024", new Dictionary<string, int> { ["STR"] = 2, ["CON"] = 1 }), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Description.Should().Be(
            "The background Soldier follows the 2024 rules, but this character is built with the 2014 rules.");
        draft.BackgroundId.Should().BeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAnUnknownBackground_ReturnsNotFound()
    {
        // Arrange
        ArrangeDraft(RulesEdition.Edition2014);
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("no-such-background", _cts.Token)).ReturnsAsync((Background?)null);

        // Act
        var result = await _sut.Handle(new SetCharacterBackgroundCommand(_draftId, "no-such-background", []), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("Background.NotFound");
    }

    [Fact]
    public async Task Handle_WithABackgroundFromABookTheDraftDoesNotAllow_ReturnsSourceNotAllowed()
    {
        // Arrange - the draft allows only the 2014 Player's Handbook
        var draft = ArrangeDraft(RulesEdition.Edition2014);
        var background = Soldier2014();
        background.Source = "SCAG";
        ArrangeBackground(background);

        // Act
        var result = await _sut.Handle(new SetCharacterBackgroundCommand(_draftId, "soldier", []), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Description.Should().Be(
            "The background Soldier comes from SCAG, a book this character does not allow.");
        draft.BackgroundId.Should().BeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithABackgroundFromABookTheDraftAllows_SetsIt()
    {
        // Arrange
        var draft = ArrangeDraft(RulesEdition.Edition2014);
        draft.AllowBooks(["SCAG"], new DraftChoiceSources(null, null, null, null, null, new Dictionary<string, string>(), new Dictionary<string, string>()));
        var background = Soldier2014();
        background.Source = "SCAG";
        ArrangeBackground(background);

        // Act
        var result = await _sut.Handle(new SetCharacterBackgroundCommand(_draftId, "soldier", []), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.BackgroundId.Should().Be("soldier");
    }

    private CharacterDraft ArrangeDraft(string edition)
    {
        var draft = CharacterDraft.CreateEmpty(_userId, new FakeTimeProvider());
        draft.ChooseEdition(edition);
        draft.SetNameAndLevel("Thorin", 1);

        _draftRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token))
            .ReturnsAsync(draft);

        return draft;
    }

    private void ArrangeBackground(Background background) =>
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(background.Id, _cts.Token)).ReturnsAsync(background);

    private static Background Soldier2014() => new()
    {
        Id = "soldier",
        Name = "Soldier",
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        FeatureId = "soldier_military_rank"
    };

    // Savage Attacker, and +2/+1 or +1 to each of Strength, Dexterity and Constitution
    private static Background Soldier2024() => new()
    {
        Id = "soldier_2024",
        Name = "Soldier",
        Edition = RulesEdition.Edition2024,
        Source = "XPHB",
        AbilityScoreOptions = ["STR", "DEX", "CON"],
        FeatId = "savage_attacker_2024"
    };

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
