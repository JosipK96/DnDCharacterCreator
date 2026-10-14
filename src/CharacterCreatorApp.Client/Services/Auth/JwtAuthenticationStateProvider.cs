using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;

namespace CharacterCreatorApp.Client.Services.Auth;

/// <summary>
/// Who is signed in, as [Authorize], AuthorizeView and AuthorizeRouteView see it. It is read
/// from the JWT in <see cref="TokenStore"/>: no token, one that is not a JWT, or one past its
/// expiry all mean signed out. The API validates every token anyway; this only decides what
/// the UI shows and where it sends the user.
/// </summary>
public sealed class JwtAuthenticationStateProvider(TokenStore tokenStore, TimeProvider timeProvider)
    : AuthenticationStateProvider
{
    private static readonly AuthenticationState SignedOut = new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly TokenStore _tokenStore = tokenStore;
    private readonly TimeProvider _timeProvider = timeProvider;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _tokenStore.GetAsync();

        return ToState(token);
    }

    public async Task SignInAsync(string token)
    {
        await _tokenStore.SetAsync(token);

        NotifyAuthenticationStateChanged(Task.FromResult(ToState(token)));
    }

    public async Task SignOutAsync()
    {
        await _tokenStore.ClearAsync();

        NotifyAuthenticationStateChanged(Task.FromResult(SignedOut));
    }

    private AuthenticationState ToState(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return SignedOut;
        }

        JwtSecurityToken jwt;

        try
        {
            jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        }
        catch (ArgumentException)
        {
            // Something in localStorage that is not a JWT: treat it as no session at all
            return SignedOut;
        }

        if (jwt.ValidTo <= _timeProvider.GetUtcNow().UtcDateTime)
        {
            return SignedOut;
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(jwt.Claims, authenticationType: "jwt")));
    }
}
