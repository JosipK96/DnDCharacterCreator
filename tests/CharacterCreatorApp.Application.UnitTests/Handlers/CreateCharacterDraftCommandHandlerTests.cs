using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.CreateDraft;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.DomainModels.Characters;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class CreateCharacterDraftCommandHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero));
    private readonly CancellationTokenSource _cts = new();
    private readonly CreateCharacterDraftCommandHandler _sut;

    public CreateCharacterDraftCommandHandlerTests()
    {
        _sut = new CreateCharacterDraftCommandHandler(
            _draftRepositoryMock.Object,
            _userContextMock.Object,
            _unitOfWorkMock.Object,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_WithValidUser_CreatesNewDraft()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userContextMock.Setup(x => x.UserId).Returns(userId);

        CharacterDraft? capturedDraft = null;
        _draftRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<CharacterDraft>(), _cts.Token))
            .Callback<CharacterDraft, CancellationToken>((draft, _) => capturedDraft = draft)
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(new CreateCharacterDraftCommand(), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        capturedDraft.Should().NotBeNull();
        capturedDraft!.CreatedByUserId.Should().Be(userId);
        capturedDraft.IsCompleted.Should().BeFalse();
        capturedDraft.CreatedAt.Should().Be(_timeProvider.GetUtcNow());
        capturedDraft.Id.Should().Be(result.Value);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_StampsTheDraftWithTheProvidedClock()
    {
        // Arrange
        _userContextMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

        CharacterDraft? capturedDraft = null;
        _draftRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<CharacterDraft>(), _cts.Token))
            .Callback<CharacterDraft, CancellationToken>((draft, _) => capturedDraft = draft)
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(new CreateCharacterDraftCommand(), _cts.Token);

        // Assert
        capturedDraft.Should().NotBeNull();
        capturedDraft!.CreatedAt.Should().Be(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ForwardsTheCallersCancellationToken()
    {
        // Arrange - the setup matches only _cts.Token, so a dropped token throws on a strict mock
        _userContextMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _draftRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<CharacterDraft>(), _cts.Token))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(new CreateCharacterDraftCommand(), _cts.Token);

        // Assert
        _draftRepositoryMock.Verify(
            r => r.CreateAsync(It.IsAny<CharacterDraft>(), _cts.Token), Times.Once);
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
