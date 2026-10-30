using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetSubrace;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class SetCharacterSubraceCommandHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IRaceRepository> _raceRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly SetCharacterSubraceCommandHandler _sut;

    private readonly Guid _userId = new("00000000-0000-0000-0000-000000000001");
    private readonly Guid _draftId = new("00000000-0000-0000-0000-000000000002");
    private readonly CharacterDraft _draft;

    public SetCharacterSubraceCommandHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        // A 2014 elf, whose high elf is the Player's Handbook's and whose sea elf is Mordenkainen's
        _draft = CharacterDraft.CreateEmpty(_userId, new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
        _draft.ChooseEdition(RulesEdition.Edition2014);
        _draft.SetRace("elf");

        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(_draft);
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("elf", _cts.Token)).ReturnsAsync(new Race
        {
            Id = "elf",
            Name = "Elf",
            Edition = RulesEdition.Edition2014,
            Source = "PHB",
            Size = "Medium",
            Subraces =
            [
                new Subrace { Id = "high_elf", Name = "High Elf", Source = "PHB", RaceId = "elf" },
                new Subrace { Id = "sea_elf", Name = "Sea Elf", Source = "MTF", RaceId = "elf" },
            ],
        });

        _sut = new SetCharacterSubraceCommandHandler(
            _draftRepositoryMock.Object,
            _raceRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _userContextMock.Object);
    }

    [Fact]
    public async Task Handle_WithASubraceFromAnAllowedBook_SetsIt()
    {
        // Act
        var result = await _sut.Handle(new SetCharacterSubraceCommand(_draftId, "high_elf"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _draft.SubraceId.Should().Be("high_elf");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithASubraceFromABookTheDraftDoesNotAllow_ReturnsSourceNotAllowed()
    {
        // Act
        var result = await _sut.Handle(new SetCharacterSubraceCommand(_draftId, "sea_elf"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Description.Should().Be(
            "The subrace Sea Elf comes from MTF, a book this character does not allow.");
        _draft.SubraceId.Should().BeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNoSubrace_ClearsItWithoutLookingUpTheRace()
    {
        // Arrange
        _draft.SetSubrace("high_elf");

        // Act
        var result = await _sut.Handle(new SetCharacterSubraceCommand(_draftId, null), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _draft.SubraceId.Should().BeNull();
        _raceRepositoryMock.Verify(r => r.GetByIdWithDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTheDraftIsNotTheirs_ReturnsNotFound()
    {
        // Arrange
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync((CharacterDraft?)null);

        // Act
        var result = await _sut.Handle(new SetCharacterSubraceCommand(_draftId, "high_elf"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.NotFound");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BeforeARaceIsChosen_ReturnsRaceRequired()
    {
        // Arrange
        var draft = CharacterDraft.CreateEmpty(_userId, new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);

        // Act
        var result = await _sut.Handle(new SetCharacterSubraceCommand(_draftId, "high_elf"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.RaceRequired");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTheRaceIsGone_ReturnsRaceNotFound()
    {
        // Arrange
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("elf", _cts.Token)).ReturnsAsync((Race?)null);

        // Act
        var result = await _sut.Handle(new SetCharacterSubraceCommand(_draftId, "high_elf"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("Race.NotFound");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ForARaceWithNoSubraces_ReturnsNoSubracesAvailable()
    {
        // Arrange
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("elf", _cts.Token)).ReturnsAsync(new Race
        {
            Id = "elf",
            Name = "Elf",
            Edition = RulesEdition.Edition2014,
            Source = "PHB",
            Size = "Medium",
            Subraces = [],
        });

        // Act
        var result = await _sut.Handle(new SetCharacterSubraceCommand(_draftId, "high_elf"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.NoSubracesAvailable");
        _draft.SubraceId.Should().BeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithASubraceTheRaceDoesNotHave_ReturnsInvalidSubraceListingTheRealOnes()
    {
        // Act
        var result = await _sut.Handle(new SetCharacterSubraceCommand(_draftId, "hill_dwarf"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.InvalidSubrace");
        result.Errors[0].Description.Should().EndWith("Available subraces: High Elf, Sea Elf");
        _draft.SubraceId.Should().BeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
