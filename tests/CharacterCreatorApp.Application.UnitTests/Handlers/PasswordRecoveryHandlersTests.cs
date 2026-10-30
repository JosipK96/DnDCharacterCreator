using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.Authentication;
using CharacterCreatorApp.Application.Features.Authentication.ChangePassword;
using CharacterCreatorApp.Application.Features.Authentication.CreatePasswordResetLink;
using CharacterCreatorApp.Application.Features.Authentication.ResetPassword;
using CharacterCreatorApp.Domain.Common.Results;

using FluentAssertions;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

/// <summary>Creating a reset link, using it, and changing a password: all of them go through <see cref="IIdentityService"/>.</summary>
public class PasswordRecoveryHandlersTests : IDisposable
{
    private const string OwnerEmail = "owner@example.com";
    private const string PlayerEmail = "thorin@example.com";
    private const string Token = "reset-token";
    private const string CurrentPassword = "Erebor123";
    private const string NewPassword = "Moria4567";

    private static readonly Guid UserId = Guid.Parse("5b0c1e3a-7c4d-4c8e-9f3a-2d6b8a1e4f70");

    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly Mock<IAdminGate> _adminGateMock = new(MockBehavior.Strict);
    private readonly Mock<IIdentityService> _identityServiceMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();

    [Fact]
    public async Task CreateLink_ByTheOwner_ReturnsTheToken()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.Email).Returns(OwnerEmail);
        _adminGateMock.Setup(g => g.IsAdmin(OwnerEmail)).Returns(true);
        _identityServiceMock.Setup(s => s.CreatePasswordResetTokenAsync(PlayerEmail, _cts.Token)).ReturnsAsync(Token);

        // Act
        var result = await CreateLinkHandler().Handle(new CreatePasswordResetLinkCommand(PlayerEmail), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Token);
    }

    [Fact]
    public async Task CreateLink_BySomeoneWhoIsNotTheOwner_ReturnsForbiddenWithoutCreatingAToken()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.Email).Returns(PlayerEmail);
        _adminGateMock.Setup(g => g.IsAdmin(PlayerEmail)).Returns(false);

        // Act
        var result = await CreateLinkHandler().Handle(new CreatePasswordResetLinkCommand(PlayerEmail), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("User.NotAnAdmin");
        result.Errors[0].Type.Should().Be(ErrorType.Forbidden);
        _identityServiceMock.Verify(s => s.CreatePasswordResetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateLink_ForAnEmailWithNoAccount_ReturnsTheNotFoundError()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.Email).Returns(OwnerEmail);
        _adminGateMock.Setup(g => g.IsAdmin(OwnerEmail)).Returns(true);
        _identityServiceMock.Setup(s => s.CreatePasswordResetTokenAsync(PlayerEmail, _cts.Token)).ReturnsAsync(UserErrors.NotFound());

        // Act
        var result = await CreateLinkHandler().Handle(new CreatePasswordResetLinkCommand(PlayerEmail), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("User.NotFound");
        result.Errors[0].Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task ResetPassword_WithAValidLink_SetsThePassword()
    {
        // Arrange
        _identityServiceMock.Setup(s => s.ResetPasswordAsync(PlayerEmail, Token, NewPassword, _cts.Token)).ReturnsAsync(Response.Success());

        // Act
        var result = await new ResetPasswordCommandHandler(_identityServiceMock.Object)
            .Handle(new ResetPasswordCommand(PlayerEmail, Token, NewPassword), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _identityServiceMock.Verify(s => s.ResetPasswordAsync(PlayerEmail, Token, NewPassword, _cts.Token), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_WithAUsedOrExpiredLink_ReturnsTheInvalidLinkError()
    {
        // Arrange
        _identityServiceMock.Setup(s => s.ResetPasswordAsync(PlayerEmail, Token, NewPassword, _cts.Token)).ReturnsAsync(UserErrors.InvalidResetLink());

        // Act
        var result = await new ResetPasswordCommandHandler(_identityServiceMock.Object)
            .Handle(new ResetPasswordCommand(PlayerEmail, Token, NewPassword), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("User.InvalidResetLink");
        result.Errors[0].Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task ResetPassword_WithAPasswordTheRulesRefuse_ReturnsIdentitysReasons()
    {
        // Arrange
        _identityServiceMock
            .Setup(s => s.ResetPasswordAsync(PlayerEmail, Token, "abc", _cts.Token))
            .ReturnsAsync(new List<Error> { UserErrors.RejectedByIdentity("PasswordTooShort", "too short") });

        // Act
        var result = await new ResetPasswordCommandHandler(_identityServiceMock.Object)
            .Handle(new ResetPasswordCommand(PlayerEmail, Token, "abc"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("User.PasswordTooShort");
    }

    [Fact]
    public async Task ChangePassword_WithTheRightCurrentPassword_ChangesTheSignedInUsersPassword()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.UserId).Returns(UserId);
        _identityServiceMock.Setup(s => s.ChangePasswordAsync(UserId, CurrentPassword, NewPassword, _cts.Token)).ReturnsAsync(Response.Success());

        // Act
        var result = await ChangeHandler().Handle(new ChangePasswordCommand(CurrentPassword, NewPassword), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _identityServiceMock.Verify(s => s.ChangePasswordAsync(UserId, CurrentPassword, NewPassword, _cts.Token), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WithAWrongCurrentPassword_ReturnsTheWrongPasswordError()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.UserId).Returns(UserId);
        _identityServiceMock.Setup(s => s.ChangePasswordAsync(UserId, "nope", NewPassword, _cts.Token)).ReturnsAsync(UserErrors.WrongCurrentPassword());

        // Act
        var result = await ChangeHandler().Handle(new ChangePasswordCommand("nope", NewPassword), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("User.WrongCurrentPassword");
        result.Errors[0].Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task ChangePassword_WithANewPasswordTheRulesRefuse_ReturnsIdentitysReasons()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.UserId).Returns(UserId);
        _identityServiceMock
            .Setup(s => s.ChangePasswordAsync(UserId, CurrentPassword, "abc", _cts.Token))
            .ReturnsAsync(new List<Error> { UserErrors.RejectedByIdentity("PasswordTooShort", "too short") });

        // Act
        var result = await ChangeHandler().Handle(new ChangePasswordCommand(CurrentPassword, "abc"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("User.PasswordTooShort");
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    private CreatePasswordResetLinkCommandHandler CreateLinkHandler() =>
        new(_userContextMock.Object, _adminGateMock.Object, _identityServiceMock.Object);

    private ChangePasswordCommandHandler ChangeHandler() =>
        new(_userContextMock.Object, _identityServiceMock.Object);
}
