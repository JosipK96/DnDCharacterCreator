using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetRace;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class SetCharacterRaceCommandHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IRaceRepository> _raceRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly SetCharacterRaceCommandHandler _sut;

    private readonly Guid _userId = new("00000000-0000-0000-0000-000000000001");
    private readonly Guid _draftId = new("00000000-0000-0000-0000-000000000002");

    public SetCharacterRaceCommandHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        _sut = new SetCharacterRaceCommandHandler(
            _draftRepositoryMock.Object,
            _raceRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _userContextMock.Object);
    }

    [Fact]
    public async Task Handle_WithTheHalfElfsTwoIncreases_StoresThem()
    {
        // Arrange - any spelling of the abilities, kept under their ids
        var draft = ArrangeDraft();
        ArrangeRace(HalfElf());

        var command = new SetCharacterRaceCommand(_draftId, "half_elf", null, new() { ["dexterity"] = 1, ["WIS"] = 1 });

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.RaceId.Should().Be("half_elf");
        draft.RaceAbilityIncreases.Should().BeEquivalentTo(new Dictionary<string, int> { ["DEX"] = 1, ["WIS"] = 1 });
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAnIncreaseTheRaceDoesNotOffer_ReturnsValidationError()
    {
        // Arrange - the half-elf's +1s go to abilities other than its Charisma
        var draft = ArrangeDraft();
        ArrangeRace(HalfElf());

        var command = new SetCharacterRaceCommand(_draftId, "half_elf", null, new() { ["CHA"] = 1, ["DEX"] = 1 });

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.InvalidRaceAbilityIncreases");
        draft.RaceId.Should().BeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ForAVariantHuman_TakesTheSubracesIncreases()
    {
        // Arrange - the human race has no choice; its variant subrace picks two of all six
        var draft = ArrangeDraft();
        ArrangeRace(Human());

        var command = new SetCharacterRaceCommand(_draftId, "human", "variant_human", new() { ["CHA"] = 1, ["WIS"] = 1 });

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        (draft.RaceId, draft.SubraceId).Should().Be(("human", "variant_human"));
        draft.RaceAbilityIncreases.Should().BeEquivalentTo(new Dictionary<string, int> { ["CHA"] = 1, ["WIS"] = 1 });
    }

    [Fact]
    public async Task Handle_ForAStandardHumanWithIncreases_ReturnsValidationError()
    {
        // Arrange - the standard human's +1 to everything is fixed; there is nothing to pick
        ArrangeDraft();
        ArrangeRace(Human());

        var command = new SetCharacterRaceCommand(_draftId, "human", "standard_human", new() { ["CHA"] = 1, ["WIS"] = 1 });

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.InvalidRaceAbilityIncreases");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithASubraceOfAnotherRace_ReturnsSubraceNotFound()
    {
        // Arrange
        ArrangeDraft();
        ArrangeRace(Human());

        var command = new SetCharacterRaceCommand(_draftId, "human", "hill_dwarf");

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("Subrace.NotFound");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithARaceOfTheOtherEdition_ReturnsEditionMismatch()
    {
        // Arrange - a 2024 draft offered the 2014 half-elf
        ArrangeDraft(RulesEdition.Edition2024);
        ArrangeRace(HalfElf());

        var command = new SetCharacterRaceCommand(_draftId, "half_elf", null, new() { ["DEX"] = 1, ["WIS"] = 1 });

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.EditionMismatch");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAnUnknownRace_ReturnsRaceNotFound()
    {
        // Arrange
        ArrangeDraft();
        _raceRepositoryMock.Setup(r => r.GetByIdWithSubracesAsync("kender", _cts.Token)).ReturnsAsync((Race?)null);

        // Act
        var result = await _sut.Handle(new SetCharacterRaceCommand(_draftId, "kender", null), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("Race.NotFound");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAMissingDraft_ReturnsNotFound()
    {
        // Arrange
        _draftRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token))
            .ReturnsAsync((CharacterDraft?)null);

        // Act
        var result = await _sut.Handle(new SetCharacterRaceCommand(_draftId, "half_elf", null), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.NotFound");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithARaceFromABookTheDraftDoesNotAllow_ReturnsSourceNotAllowed()
    {
        // Arrange - Volo's Guide is not among the draft's books, the Player's Handbook alone
        var draft = ArrangeDraft();
        var race = HalfElf();
        race.Source = "VGM";
        ArrangeRace(race);

        var command = new SetCharacterRaceCommand(_draftId, "half_elf", null, new() { ["DEX"] = 1, ["WIS"] = 1 });

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Description.Should().Be(
            "The race Half-Elf comes from VGM, a book this character does not allow.");
        draft.RaceId.Should().BeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithARaceFromABookTheDraftAllows_SetsIt()
    {
        // Arrange
        var draft = ArrangeDraft();
        draft.AllowBooks(["VGM"], new DraftChoiceSources(null, null, null, null, null, new Dictionary<string, string>(), new Dictionary<string, string>()));
        var race = HalfElf();
        race.Source = "VGM";
        ArrangeRace(race);

        var command = new SetCharacterRaceCommand(_draftId, "half_elf", null, new() { ["DEX"] = 1, ["WIS"] = 1 });

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.RaceId.Should().Be("half_elf");
    }

    [Fact]
    public async Task Handle_WithASubraceFromABookTheDraftDoesNotAllow_ReturnsSourceNotAllowed()
    {
        // Arrange - a Player's Handbook race whose subrace is from another book
        var draft = ArrangeDraft();
        var human = Human();
        human.Subraces.First(s => s.Id == "standard_human").Source = "MTF";
        ArrangeRace(human);

        // Act
        var result = await _sut.Handle(new SetCharacterRaceCommand(_draftId, "human", "standard_human"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Description.Should().Be(
            "The subrace Standard Human comes from MTF, a book this character does not allow.");
        draft.RaceId.Should().BeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Small")]
    [InlineData("Medium")]
    public async Task Handle_WithASizeTheSpeciesOffers_StoresIt(string size)
    {
        // Arrange - a 2024 human is Small or Medium
        var draft = ArrangeDraft(RulesEdition.Edition2024);
        ArrangeRace(Human2024());

        // Act
        var result = await _sut.Handle(new SetCharacterRaceCommand(_draftId, "human_2024", null, null, size), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.Size.Should().Be(size);
    }

    [Fact]
    public async Task Handle_WithASizeTheSpeciesDoesNotOffer_ReturnsSizeNotOffered()
    {
        // Arrange
        var draft = ArrangeDraft(RulesEdition.Edition2024);
        ArrangeRace(Human2024());

        // Act
        var result = await _sut.Handle(new SetCharacterRaceCommand(_draftId, "human_2024", null, null, "Large"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.SizeNotOffered");
        draft.RaceId.Should().BeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithTheOnlySizeOfARace_KeepsNoSizeOnTheDraft()
    {
        // Arrange - a race of one size has it on the race
        var draft = ArrangeDraft();
        ArrangeRace(HalfElf());

        // Act
        var result = await _sut.Handle(new SetCharacterRaceCommand(_draftId, "half_elf", null, new() { ["DEX"] = 1, ["WIS"] = 1 }, "Medium"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.Size.Should().BeNull();
    }

    private CharacterDraft ArrangeDraft(string edition = RulesEdition.Edition2014)
    {
        var draft = CharacterDraft.CreateEmpty(
            _userId,
            new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
        draft.ChooseEdition(edition);

        _draftRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token))
            .ReturnsAsync(draft);

        return draft;
    }

    private void ArrangeRace(Race race) =>
        _raceRepositoryMock.Setup(r => r.GetByIdWithSubracesAsync(race.Id, _cts.Token)).ReturnsAsync(race);

    private static Race Human2024() => new()
    {
        Id = "human_2024",
        Name = "Human",
        Edition = RulesEdition.Edition2024,
        Source = "XPHB",
        Size = "Small or Medium",
        Speed = 30,
    };

    // +2 Charisma, and +1 to two others of the player's choice
    private static Race HalfElf() => new()
    {
        Id = "half_elf",
        Name = "Half-Elf",
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        Size = "Medium",
        Speed = 30,
        AbilityChoiceCount = 2,
        AbilityChoiceOptions = ["STR", "DEX", "CON", "INT", "WIS"],
        AbilityBonuses = [new RaceAbilityBonus { RaceId = "half_elf", AbilityScoreId = "CHA", Bonus = 2 }],
    };

    // The standard human's +1 to everything, or the variant's +1 to two of the player's choice
    private static Race Human() => new()
    {
        Id = "human",
        Name = "Human",
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        Size = "Medium",
        Speed = 30,
        Subraces =
        [
            new Subrace { Id = "standard_human", Name = "Standard Human", Source = "PHB", RaceId = "human" },
            new Subrace
            {
                Id = "variant_human",
                Name = "Variant Human",
                Source = "PHB",
                RaceId = "human",
                AbilityChoiceCount = 2,
                AbilityChoiceOptions = ["STR", "DEX", "CON", "INT", "WIS", "CHA"],
                SkillChoiceCount = 1,
                FeatChoiceCount = 1,
            },
        ],
    };

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
