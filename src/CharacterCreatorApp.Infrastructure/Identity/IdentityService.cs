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

        if (user is null)
        {
            return UserErrors.NotFoundByEmail();
        }

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            return UserErrors.WrongPassword();
        }

        // Found by its email, so Email is set
        return new AuthenticatedUser(user.Id, user.Email!);
    }
}
