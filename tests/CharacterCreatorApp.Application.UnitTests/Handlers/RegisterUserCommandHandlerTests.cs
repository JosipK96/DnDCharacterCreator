using CharacterCreatorApp.Application.Features.Authentication;
using CharacterCreatorApp.Application.Features.Authentication.Register;
using CharacterCreatorApp.Domain.Common.Results;

using FluentAssertions;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class RegisterUserCommandHandlerTests : IDisposable
{
    private const string Email = "thorin@example.com";
    private const string UserName = "thorin";
    private const string Password = "Erebor123";
    private const string DisplayName = "Thorin Oakenshield";

    private readonly Mock<IIdentityService> _identityServiceMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly RegisterUserCommandHandler _sut;

    private readonly RegisterUserCommand _command = new(Email, UserName, Password, DisplayName);

    public RegisterUserCommandHandlerTests()
    {
        _sut = new RegisterUserCommandHandler(_identityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithAnUnusedEmail_CreatesTheAccount()
    {
        // Arrange
        _identityServiceMock.Setup(s => s.IsEmailInUseAsync(Email, _cts.Token)).ReturnsAsync(false);
        _identityServiceMock
            .Setup(s => s.CreateUserAsync(UserName, Email, Password, DisplayName, _cts.Token))
            .ReturnsAsync(Response.Success());

        // Act
        var result = await _sut.Handle(_command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _identityServiceMock.Verify(s => s.CreateUserAsync(UserName, Email, Password, DisplayName, _cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAnEmailAlreadyInUse_ReturnsConflictWithoutCreatingAnAccount()
    {
        // Arrange
        _identityServiceMock.Setup(s => s.IsEmailInUseAsync(Email, _cts.Token)).ReturnsAsync(true);

        // Act
        var result = await _sut.Handle(_command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("User.EmailAlreadyInUse");
        result.Errors[0].Type.Should().Be(ErrorType.Conflict);
        _identityServiceMock.Verify(
            s => s.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenIdentityRejectsTheAccount_ReturnsItsReasons()
    {
        // Arrange
        _identityServiceMock.Setup(s => s.IsEmailInUseAsync(Email, _cts.Token)).ReturnsAsync(false);
        _identityServiceMock
            .Setup(s => s.CreateUserAsync(UserName, Email, Password, DisplayName, _cts.Token))
            .ReturnsAsync((Response)UserErrors.RejectedByIdentity("PasswordRequiresNonAlphanumeric", "Passwords must have a symbol."));

        // Act
        var result = await _sut.Handle(_command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("User.PasswordRequiresNonAlphanumeric");
        result.Errors[0].Type.Should().Be(ErrorType.Validation);
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
