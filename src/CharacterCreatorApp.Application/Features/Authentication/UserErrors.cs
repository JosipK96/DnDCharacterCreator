using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Authentication;

public static class UserErrors
{
    public static Error EmailAlreadyInUse() => Error.Conflict(code: "User.EmailAlreadyInUse", description: "Email is already in use");

    /// <summary>One answer for an unknown email, a wrong password and a locked account, so a failed sign-in never says which emails have accounts.</summary>
    public static Error InvalidCredentials() => Error.Unauthorized(code: "User.InvalidCredentials", description: "Wrong email or password");
    public static Error InvalidInviteCode() => Error.Forbidden(code: "User.InvalidInviteCode", description: "The invite code is missing or wrong");

    public static Error NotAnAdmin() => Error.Forbidden(code: "User.NotAnAdmin", description: "Only the owner can do this");
    public static Error NotFound() => Error.NotFound(code: "User.NotFound", description: "No account has that email");

    /// <summary>One answer for an unknown email and a used, expired or wrong link, so the page never says which emails have accounts.</summary>
    public static Error InvalidResetLink() => Error.Validation(code: "User.InvalidResetLink", description: "This reset link has been used, has expired, or is not valid. Ask for a new one");

    public static Error WrongCurrentPassword() => Error.Validation(code: "User.WrongCurrentPassword", description: "The current password is wrong");

    /// <summary>
    /// A reason ASP.NET Core Identity refused to create an account. Identity names its own
    /// reasons ("PasswordTooShort", "DuplicateUserName"), so the code is that name under "User.".
    /// </summary>
    public static Error RejectedByIdentity(string identityCode, string description) =>
        Error.Validation(code: $"User.{identityCode}", description: description);
}
