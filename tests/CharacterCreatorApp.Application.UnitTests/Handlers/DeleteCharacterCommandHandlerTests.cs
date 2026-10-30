using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterManagement.DeleteCharacter;
using CharacterCreatorApp.Application.Persistence;

using FluentAssertions;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class DeleteCharacterCommandHandlerTests : IDisposable
{
    private readonly Mock<ICharacterRepository> _characterRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly DeleteCharacterCommandHandler _sut;

    private readonly Guid _userId = new("00000000-0000-0000-0000-000000000001");
    private readonly Guid _characterId = new("00000000-0000-0000-0000-000000000003");

    public DeleteCharacterCommandHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);

        _sut = new DeleteCharacterCommandHandler(
            _characterRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _userContextMock.Object);
    }

    [Fact]
    public async Task Handle_WithTheirCharacter_DeletesItAndSaves()
    {
        // Arrange
        _characterRepositoryMock.Setup(r => r.DeleteAsync(_characterId, _userId, _cts.Token)).ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(new DeleteCharacterCommand(_characterId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTheCharacterIsNotTheirs_ReturnsNotFoundAndSavesNothing()
    {
        // Arrange
        _characterRepositoryMock.Setup(r => r.DeleteAsync(_characterId, _userId, _cts.Token)).ReturnsAsync(false);

        // Act
        var result = await _sut.Handle(new DeleteCharacterCommand(_characterId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("Character.NotFound");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
