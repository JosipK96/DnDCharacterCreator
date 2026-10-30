using CharacterCreatorApp.Application.Features.Authentication;
using CharacterCreatorApp.Application.Features.Authentication.Login;
using CharacterCreatorApp.Domain.Common.Results;

using FluentAssertions;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class LoginUserCommandHandlerTests : IDisposable
{
    private const string Email = "thorin@example.com";
    private const string Password = "Erebor123";

    private readonly Mock<IIdentityService> _identityServiceMock = new(MockBehavior.Strict);
    private readonly Mock<IJwtHelper> _jwtHelperMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly LoginUserCommandHandler _sut;

    private readonly AuthenticatedUser _user = new(new Guid("0d6b8f3e-5c2a-4e71-9a4d-3f8c1b7e2a90"), Email);

    public LoginUserCommandHandlerTests()
    {
        _sut = new LoginUserCommandHandler(_identityServiceMock.Object, _jwtHelperMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsATokenForThatUser()
    {
        // Arrange
        _identityServiceMock
            .Setup(s => s.CheckCredentialsAsync(Email, Password, _cts.Token))
            .ReturnsAsync((Response<AuthenticatedUser>)_user);
        _jwtHelperMock.Setup(j => j.GenerateJwtToken(_user)).Returns("signed-token");

        // Act
        var result = await _sut.Handle(new LoginUserCommand(Email, Password), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("signed-token");
    }

    [Fact]
    public async Task Handle_WhenTheCredentialsAreRejected_ReturnsTheReasonWithoutIssuingAToken()
    {
        // Arrange
        _identityServiceMock
            .Setup(s => s.CheckCredentialsAsync(Email, Password, _cts.Token))
            .ReturnsAsync((Response<AuthenticatedUser>)UserErrors.InvalidCredentials());

        // Act
        var result = await _sut.Handle(new LoginUserCommand(Email, Password), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("User.InvalidCredentials");
        _jwtHelperMock.Verify(j => j.GenerateJwtToken(It.IsAny<AuthenticatedUser>()), Times.Never);
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
