using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Authentication;

/// <summary>
/// The user accounts the app keeps. ASP.NET Core Identity implements it in Infrastructure, so
/// neither Application nor Domain has to reference Identity or know its user type.
/// </summary>
public interface IIdentityService
{
    Task<bool> IsEmailInUseAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Creates the account, or returns the reasons Identity refused it (a weak password, a
    /// user name already taken) as validation errors.
    /// </summary>
    Task<Response> CreateUserAsync(string userName, string email, string password, string? displayName, CancellationToken cancellationToken);

    /// <summary>
    /// The account the email and password belong to, or <see cref="UserErrors.InvalidCredentials"/>\r
    /// for an unknown email, a wrong password and a locked account alike. A wrong password counts\r
    /// toward locking the account; a successful sign-in resets the count.
    /// </summary>
    Task<Response<AuthenticatedUser>> CheckCredentialsAsync(string email, string password, CancellationToken cancellationToken);

    /// <summary>
    /// A one-time token that lets the account's owner set a new password, or
    /// <see cref="UserErrors.NotFound"/> when no account has the email. It expires and stops
    /// working once any password change has happened.
    /// </summary>
    Task<Response<string>> CreatePasswordResetTokenAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the new password. A used, expired or wrong token and an unknown email all answer
    /// <see cref="UserErrors.InvalidResetLink"/>; a password the rules refuse is a validation
    /// error. A reset also lifts a lockout.
    /// </summary>
    Task<Response> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken);

    /// <summary>
    /// Changes the password of a signed-in user. A wrong current password is
    /// <see cref="UserErrors.WrongCurrentPassword"/> and counts toward locking the account.
    /// </summary>
    Task<Response> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken);
}
