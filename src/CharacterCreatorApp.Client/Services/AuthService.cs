using System.Net.Http.Json;

using CharacterCreatorApp.Client.Services.Auth;
using CharacterCreatorApp.Client.Services.Http;
using CharacterCreatorApp.Contracts.Requests.User;
using CharacterCreatorApp.Contracts.Responses;

namespace CharacterCreatorApp.Client.Services;

public class AuthService(HttpClient http, JwtAuthenticationStateProvider authenticationState)
{
    private readonly HttpClient _http = http;
    private readonly JwtAuthenticationStateProvider _authenticationState = authenticationState;

    /// <summary>Signs in and returns null, or returns the API's reason for refusing.</summary>
    public async Task<string?> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("/api/users/login", new LoginUserRequest(email, password));

        var failure = await response.GetFailureAsync();

        if (failure is not null)
        {
            return failure.Message;
        }

        var login = await response.ReadAsync<LoginApiResponse>();
        await _authenticationState.SignInAsync(login.Token);

        return null;
    }

    /// <summary>Creates the account and returns nothing, or every reason the API gave for refusing it.</summary>
    public async Task<IReadOnlyList<string>> RegisterAsync(RegisterUserRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/users/register", request);

        var failure = await response.GetFailureAsync();

        if (failure is null)
        {
            return [];
        }

        return failure.Errors.Count > 0
            ? failure.Errors.Select(e => e.Detail).ToList()
            : [failure.Message];
    }

    public Task LogoutAsync() => _authenticationState.SignOutAsync();
}
