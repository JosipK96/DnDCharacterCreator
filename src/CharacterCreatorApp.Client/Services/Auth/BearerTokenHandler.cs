using System.Net;
using System.Net.Http.Headers;

namespace CharacterCreatorApp.Client.Services.Auth;

/// <summary>
/// Adds the signed-in user's token to every API call, and signs the user out when the API
/// rejects it. An expired session then sends the user to the login page from wherever they are,
/// instead of every page checking for a 401 itself.
/// </summary>
public sealed class BearerTokenHandler(TokenStore tokenStore, JwtAuthenticationStateProvider authenticationState)
    : DelegatingHandler
{
    private readonly TokenStore _tokenStore = tokenStore;
    private readonly JwtAuthenticationStateProvider _authenticationState = authenticationState;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenStore.GetAsync();

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Only a request that carried a token can have been refused because of it; a wrong
        // password on the login form is a 401 too, and must not sign anyone out
        if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(token))
        {
            await _authenticationState.SignOutAsync();
        }

        return response;
    }
}
