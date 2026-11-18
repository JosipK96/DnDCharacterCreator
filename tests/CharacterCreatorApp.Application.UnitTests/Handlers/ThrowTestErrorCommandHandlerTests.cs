using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.Authentication;
using CharacterCreatorApp.Application.Features.Authentication.ThrowTestError;
using CharacterCreatorApp.Domain.Common.Results;

using FluentAssertions;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class ThrowTestErrorCommandHandlerTests : IDisposable
{
    private const string OwnerEmail = "owner@example.com";
    private const string PlayerEmail = "thorin@example.com";

    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly Mock<IAdminGate> _adminGateMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();

    [Fact]
    public async Task Handle_ByTheOwner_ThrowsTheDeliberateError()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.Email).Returns(OwnerEmail);
        _adminGateMock.Setup(g => g.IsAdmin(OwnerEmail)).Returns(true);

        // Act
        var act = () => Handler().Handle(new ThrowTestErrorCommand(), _cts.Token);

        // Assert
        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage(ThrowTestErrorCommandHandler.Message);
    }

    [Fact]
    public async Task Handle_BySomeoneWhoIsNotTheOwner_ReturnsForbiddenWithoutThrowing()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.Email).Returns(PlayerEmail);
        _adminGateMock.Setup(g => g.IsAdmin(PlayerEmail)).Returns(false);

        // Act
        var result = await Handler().Handle(new ThrowTestErrorCommand(), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("User.NotAnAdmin");
        result.Errors[0].Type.Should().Be(ErrorType.Forbidden);
    }

    public void Dispose() => _cts.Dispose();

    private ThrowTestErrorCommandHandler Handler() => new(_userContextMock.Object, _adminGateMock.Object);
}
