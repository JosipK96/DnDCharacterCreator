using System.Net.Http.Json;

using CharacterCreatorApp.Client.Services.Http;
using CharacterCreatorApp.Contracts.Requests.User;
using CharacterCreatorApp.Contracts.Responses;

namespace CharacterCreatorApp.Client.Services;

/// <summary>Setting a new password: from a reset link, from the account page, and the owner making the link.</summary>
public class PasswordService(HttpClient http)
{
    private readonly HttpClient _http = http;

    /// <summary>Throws an <see cref="ApiException"/> when the link is used, expired or invalid, or the password is refused.</summary>
    public async Task ResetPasswordAsync(string email, string token, string newPassword)
    {
        var response = await _http.PostAsJsonAsync("/api/users/reset-password", new ResetPasswordRequest(email, token, newPassword));

        await response.EnsureSuccessAsync();
    }

    /// <summary>Throws an <see cref="ApiException"/> when the current password is wrong or the new one is refused.</summary>
    public async Task ChangePasswordAsync(string currentPassword, string newPassword)
    {
        var response = await _http.PostAsJsonAsync("/api/users/change-password", new ChangePasswordRequest(currentPassword, newPassword));

        await response.EnsureSuccessAsync();
    }

    /// <summary>The owner's: the link to send a friend. Throws an <see cref="ApiException"/> for anyone else or an unknown email.</summary>
    public async Task<string> CreateResetLinkAsync(string email)
    {
        var response = await _http.PostAsJsonAsync("/api/admin/password-reset-links", new CreatePasswordResetLinkRequest(email));

        return (await response.ReadAsync<PasswordResetLinkApiResponse>()).Link;
    }
}
