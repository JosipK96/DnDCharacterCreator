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
}
