using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetBackground;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;
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

    [Fact]
    public async Task Handle_WithADarkGiftForAMistWanderer_StoresTheFeat()
    {
        // Arrange
        var draft = ArrangeDraft(RulesEdition.Edition2024);
        draft.AllowBooks(["RHW"], new DraftChoiceSources(null, null, null, null, null, new Dictionary<string, string>(), new Dictionary<string, string>()));
        ArrangeBackground(MistWanderer());
        ArrangeFeat(DarkGift());

        // Act
        var result = await _sut.Handle(new SetCharacterBackgroundCommand(_draftId, "mist_wanderer_2024", Increases(), "mist_walker"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.BackgroundFeatId.Should().Be("mist_walker");
    }

    [Fact]
    public async Task Handle_WithAFeatForABackgroundWithItsOwn_ReturnsBackgroundFeatNotChoosable()
    {
        // Arrange - the 2024 soldier's feat is Savage Attacker
        var draft = ArrangeDraft(RulesEdition.Edition2024);
        ArrangeBackground(Soldier2024());

        // Act
        var result = await _sut.Handle(
            new SetCharacterBackgroundCommand(_draftId, "soldier_2024", new() { ["STR"] = 2, ["CON"] = 1 }, "alert_2024"), _cts.Token);

        // Assert
        AssertRefused(result, draft, "CharacterDraft.BackgroundFeatNotChoosable");
    }

    [Fact]
    public async Task Handle_WithAFeatOfAnotherCategory_ReturnsBackgroundFeatNotOfCategory()
    {
        // Arrange
        var draft = ArrangeDraft(RulesEdition.Edition2024);
        draft.AllowBooks(["RHW"], new DraftChoiceSources(null, null, null, null, null, new Dictionary<string, string>(), new Dictionary<string, string>()));
        ArrangeBackground(MistWanderer());
        var feat = DarkGift();
        feat.Category = "Origin";
        ArrangeFeat(feat);

        // Act
        var result = await _sut.Handle(new SetCharacterBackgroundCommand(_draftId, "mist_wanderer_2024", Increases(), "mist_walker"), _cts.Token);

        // Assert
        AssertRefused(result, draft, "CharacterDraft.BackgroundFeatNotOfCategory");
    }

    [Fact]
    public async Task Handle_WithAFeatFromABookTheDraftDoesNotAllow_ReturnsSourceNotAllowed()
    {
        // Arrange - the Dark Gifts are from RHW, which the draft has not ticked
        var draft = ArrangeDraft(RulesEdition.Edition2024);
        var background = MistWanderer();
        background.Source = "XPHB";
        ArrangeBackground(background);
        ArrangeFeat(DarkGift());

        // Act
        var result = await _sut.Handle(new SetCharacterBackgroundCommand(_draftId, "mist_wanderer_2024", Increases(), "mist_walker"), _cts.Token);

        // Assert
        AssertRefused(result, draft, "CharacterDraft.SourceNotAllowed");
    }

    [Fact]
    public async Task Handle_WithAFeatOfTheOtherEdition_ReturnsEditionMismatch()
    {
        // Arrange
        var draft = ArrangeDraft(RulesEdition.Edition2024);
        draft.AllowBooks(["RHW"], new DraftChoiceSources(null, null, null, null, null, new Dictionary<string, string>(), new Dictionary<string, string>()));
        ArrangeBackground(MistWanderer());
        var feat = DarkGift();
        feat.Edition = RulesEdition.Edition2014;
        ArrangeFeat(feat);

        // Act
        var result = await _sut.Handle(new SetCharacterBackgroundCommand(_draftId, "mist_wanderer_2024", Increases(), "mist_walker"), _cts.Token);

        // Assert
        AssertRefused(result, draft, "CharacterDraft.EditionMismatch");
    }

    [Fact]
    public async Task Handle_WithAFeatThatDoesNotExist_ReturnsFeatNotFound()
    {
        // Arrange
        var draft = ArrangeDraft(RulesEdition.Edition2024);
        draft.AllowBooks(["RHW"], new DraftChoiceSources(null, null, null, null, null, new Dictionary<string, string>(), new Dictionary<string, string>()));
        ArrangeBackground(MistWanderer());
        _featRepositoryMock.Setup(r => r.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "mist_walker" })), _cts.Token)).ReturnsAsync([]);

        // Act
        var result = await _sut.Handle(new SetCharacterBackgroundCommand(_draftId, "mist_wanderer_2024", Increases(), "mist_walker"), _cts.Token);

        // Assert
        AssertRefused(result, draft, "Feat.NotFound");
    }

    [Theory]
    [InlineData("haunted_one_2024")]
    [InlineData("investigator_2024")]
    public async Task Handle_WithADarkGiftForABackgroundThatAllowsOneInsteadOfItsFeat_StoresTheFeat(string backgroundId)
    {
        // Arrange
        var draft = ArrangeDraft(RulesEdition.Edition2024);
        draft.AllowBooks(["RHW"], new DraftChoiceSources(null, null, null, null, null, new Dictionary<string, string>(), new Dictionary<string, string>()));
        ArrangeBackground(DarkGiftAlternatives()[backgroundId]);
        ArrangeFeat(DarkGift());

        // Act
        var result = await _sut.Handle(new SetCharacterBackgroundCommand(_draftId, backgroundId, WisdomAndCharisma(), "mist_walker"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.BackgroundId.Should().Be(backgroundId);
        draft.BackgroundFeatId.Should().Be("mist_walker");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoFeatForABackgroundThatAllowsADarkGift_KeepsItsOwn()
    {
        // Arrange - the Haunted One keeps Survivor, which no pick replaces
        var draft = ArrangeDraft(RulesEdition.Edition2024);
        draft.AllowBooks(["RHW"], new DraftChoiceSources(null, null, null, null, null, new Dictionary<string, string>(), new Dictionary<string, string>()));
        ArrangeBackground(HauntedOne());

        // Act
        var result = await _sut.Handle(new SetCharacterBackgroundCommand(_draftId, "haunted_one_2024", WisdomAndCharisma()), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.BackgroundId.Should().Be("haunted_one_2024");
        draft.BackgroundFeatId.Should().BeNull();
        _featRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WithAnOriginFeatInPlaceOfAHauntedOnesOwn_ReturnsBackgroundFeatNotOfCategory()
    {
        // Arrange - only a Dark Gift may replace Survivor
        var draft = ArrangeDraft(RulesEdition.Edition2024);
        draft.AllowBooks(["RHW"], new DraftChoiceSources(null, null, null, null, null, new Dictionary<string, string>(), new Dictionary<string, string>()));
        ArrangeBackground(HauntedOne());
        var feat = DarkGift();
        feat.Category = "Origin";
        ArrangeFeat(feat);

        // Act
        var result = await _sut.Handle(new SetCharacterBackgroundCommand(_draftId, "haunted_one_2024", WisdomAndCharisma(), "mist_walker"), _cts.Token);

        // Assert
        AssertRefused(result, draft, "CharacterDraft.BackgroundFeatNotOfCategory");
        result.Errors[0].Description.Should().Be("A feat picked for the Haunted One background is a Dark Gift; Mist Walker is not.");
    }

    [Fact]
    public async Task Handle_WithADarkGiftFromABookTheDraftDoesNotAllowForAHauntedOne_ReturnsSourceNotAllowed()
    {
        // Arrange - the background is allowed, the Dark Gift's book is not
        var draft = ArrangeDraft(RulesEdition.Edition2024);
        var background = HauntedOne();
        background.Source = "XPHB";
        ArrangeBackground(background);
        ArrangeFeat(DarkGift());

        // Act
        var result = await _sut.Handle(new SetCharacterBackgroundCommand(_draftId, "haunted_one_2024", WisdomAndCharisma(), "mist_walker"), _cts.Token);

        // Assert
        AssertRefused(result, draft, "CharacterDraft.SourceNotAllowed");
    }

    [Fact]
    public async Task Handle_WithADarkGiftForASoldier_ReturnsBackgroundFeatNotChoosable()
    {
        // Arrange - the 2024 soldier has Savage Attacker and no alternative
        var draft = ArrangeDraft(RulesEdition.Edition2024);
        ArrangeBackground(Soldier2024());

        // Act
        var result = await _sut.Handle(
            new SetCharacterBackgroundCommand(_draftId, "soldier_2024", new() { ["STR"] = 2, ["CON"] = 1 }, "mist_walker"), _cts.Token);

        // Assert
        AssertRefused(result, draft, "CharacterDraft.BackgroundFeatNotChoosable");
    }

    private void AssertRefused(Response result, CharacterDraft draft, string code)
    {
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be(code);
        draft.BackgroundId.Should().BeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private void ArrangeFeat(FeatReadModel feat) =>
        _featRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { feat.Id })), _cts.Token))
            .ReturnsAsync([feat]);

    private static Dictionary<string, int> Increases() => new() { ["DEX"] = 2, ["WIS"] = 1 };

    // Two of the three a Haunted One and an Investigator both raise
    private static Dictionary<string, int> WisdomAndCharisma() => new() { ["WIS"] = 2, ["CHA"] = 1 };

    // Any Dark Gift, and +2/+1 or +1 to each of Dexterity, Wisdom and Charisma
    private static Background MistWanderer() => new()
    {
        Id = "mist_wanderer_2024",
        Name = "Mist Wanderer",
        Edition = RulesEdition.Edition2024,
        Source = "RHW",
        AbilityScoreOptions = ["DEX", "WIS", "CHA"],
        FeatChoiceCategory = "Dark Gift",
    };

    // Survivor, or a Dark Gift in its place; the ability options are trimmed to the Mist Wanderer's
    // so that Increases() fits every background here
    private static Background HauntedOne() => new()
    {
        Id = "haunted_one_2024",
        Name = "Haunted One",
        Edition = RulesEdition.Edition2024,
        Source = "RHW",
        AbilityScoreOptions = ["CON", "WIS", "CHA"],
        FeatId = "survivor",
        FeatAlternativeCategory = "Dark Gift",
    };

    private static Dictionary<string, Background> DarkGiftAlternatives() => new()
    {
        ["haunted_one_2024"] = HauntedOne(),
        ["investigator_2024"] = new()
        {
            Id = "investigator_2024",
            Name = "Investigator",
            Edition = RulesEdition.Edition2024,
            Source = "RHW",
            AbilityScoreOptions = ["INT", "WIS", "CHA"],
            FeatId = "sharp_eye",
            FeatAlternativeCategory = "Dark Gift",
        },
    };

    private static FeatReadModel DarkGift() => new()
    {
        Id = "mist_walker",
        Name = "Mist Walker",
        Description = "A feat.",
        Edition = RulesEdition.Edition2024,
        Source = "RHW",
        Category = "Dark Gift",
    };

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
