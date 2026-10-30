using CharacterCreatorApp.Application.Features.Authentication;
using CharacterCreatorApp.Domain.Common.Results;

using Microsoft.AspNetCore.Identity;

namespace CharacterCreatorApp.Infrastructure.Identity;

/// <summary>
/// UserManager takes no CancellationToken, so each method checks the caller's token before it
/// calls in. That is as far as the token can reach.
/// </summary>
public class IdentityService(UserManager<ApplicationUser> userManager) : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<bool> IsEmailInUseAsync(string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _userManager.FindByEmailAsync(email) is not null;
    }

    public async Task<Response> CreateUserAsync(string userName, string email, string password, string? displayName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userManager.CreateAsync(ApplicationUser.Create(userName, email, displayName), password);

        if (result.Succeeded)
        {
            return Response.Success();
        }

        return result.Errors
            .Select(e => UserErrors.RejectedByIdentity(e.Code, e.Description))
            .ToList();
    }

    public async Task<Response<AuthenticatedUser>> CheckCredentialsAsync(string email, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByEmailAsync(email);

        // shortcut: an unknown email returns sooner than a wrong password (no hash is checked), so
        // timing can still tell them apart; the per-IP rate limit bounds how often it can be tried
        if (user is null)
        {
            return UserErrors.InvalidCredentials();
        }

        // Even the right password is refused while locked, with the same answer as a wrong one
        if (await _userManager.IsLockedOutAsync(user))
        {
            return UserErrors.InvalidCredentials();
        }

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            await _userManager.AccessFailedAsync(user);

            return UserErrors.InvalidCredentials();
        }

        if (user.AccessFailedCount > 0)
        {
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        // Found by its email, so Email is set
        return new AuthenticatedUser(user.Id, user.Email!);
    }

    public async Task<Response<string>> CreatePasswordResetTokenAsync(string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByEmailAsync(email);

        // Only the owner reaches this, so saying the email has no account leaks nothing to a guesser
        if (user is null)
        {
            return UserErrors.NotFound();
        }

        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<Response> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByEmailAsync(email);

        // shortcut: an unknown email returns sooner than a bad token (no token is verified), so
        // timing can still tell them apart; the per-IP rate limit bounds how often it can be tried
        if (user is null)
        {
            return UserErrors.InvalidResetLink();
        }

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            // Identity checks the token before the password rules, so an invalid token is the only error then
            return result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken))
                ? UserErrors.InvalidResetLink()
                : result.Errors.Select(e => UserErrors.RejectedByIdentity(e.Code, e.Description)).ToList();
        }

        // The owner vouched for this person, so a lockout from earlier failed sign-ins ends with the reset
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        return Response.Success();
    }

    public async Task<Response> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return UserErrors.NotFound();
        }

        // A stolen sign-in token must not be a way to guess the password past the lockout
        if (await _userManager.IsLockedOutAsync(user))
        {
            return UserErrors.WrongCurrentPassword();
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (result.Succeeded)
        {
            await _userManager.ResetAccessFailedCountAsync(user);

            return Response.Success();
        }

        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch)))
        {
            await _userManager.AccessFailedAsync(user);

            return UserErrors.WrongCurrentPassword();
        }

        return result.Errors.Select(e => UserErrors.RejectedByIdentity(e.Code, e.Description)).ToList();
    }
}
