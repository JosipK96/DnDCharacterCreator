using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetAbilityScores;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class SetCharacterAbilityScoresCommandHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly SetCharacterAbilityScoresCommandHandler _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _draftId = Guid.NewGuid();

    public SetCharacterAbilityScoresCommandHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);

        _sut = new SetCharacterAbilityScoresCommandHandler(
            _draftRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _userContextMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidDraft_UpdatesAbilityScores()
    {
        // Arrange
        var draft = ArrangeDraft();
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        var scores = ValidScores();

        // Act
        var result = await _sut.Handle(new SetCharacterAbilityScoresCommand(_draftId, scores, AbilityScoreMethod.StandardArray), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.BaseAbilityScores.Should().BeEquivalentTo(scores);
        draft.BaseAbilityScoreMethod.Should().Be(AbilityScoreMethod.StandardArray);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_CopiesTheScoresRatherThanHoldingTheCallersDictionary()
    {
        // Arrange
        var draft = ArrangeDraft();
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        var scores = ValidScores();

        // Act
        await _sut.Handle(new SetCharacterAbilityScoresCommand(_draftId, scores, AbilityScoreMethod.StandardArray), _cts.Token);
        scores["STR"] = 20;

        // Assert
        draft.BaseAbilityScores["STR"].Should().Be(15);
    }

    [Fact]
    public async Task Handle_WithNonExistentDraft_ReturnsNotFoundError()
    {
        // Arrange
        _draftRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token))
            .ReturnsAsync((CharacterDraft?)null);

        // Act
        var result = await _sut.Handle(new SetCharacterAbilityScoresCommand(_draftId, ValidScores(), AbilityScoreMethod.StandardArray), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("CharacterDraft.NotFound");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTheDraftBelongsToAnotherUser_ReturnsNotFound()
    {
        // Arrange - the repository scopes by user, so someone else's draft simply is not found
        _draftRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token))
            .ReturnsAsync((CharacterDraft?)null);

        // Act
        var result = await _sut.Handle(new SetCharacterAbilityScoresCommand(_draftId, ValidScores(), AbilityScoreMethod.StandardArray), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.NotFound");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ForwardsTheCallersCancellationToken()
    {
        // Arrange - the strict setup matches only _cts.Token
        ArrangeDraft();
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(new SetCharacterAbilityScoresCommand(_draftId, ValidScores(), AbilityScoreMethod.StandardArray), _cts.Token);

        // Assert
        _draftRepositoryMock.Verify(
            r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token), Times.Once);
    }

    private CharacterDraft ArrangeDraft()
    {
        var draft = CharacterDraft.CreateEmpty(
            _userId,
            new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));

        _draftRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token))
            .ReturnsAsync(draft);

        return draft;
    }

    private static Dictionary<string, int> ValidScores() => new()
    {
        ["STR"] = 15,
        ["DEX"] = 14,
        ["CON"] = 13,
        ["INT"] = 12,
        ["WIS"] = 10,
        ["CHA"] = 8
    };

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
