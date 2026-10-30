using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using CharacterCreatorApp.Client.Models;

namespace CharacterCreatorApp.Client.Services.Http;

/// <summary>
/// The one place the client turns an API response into a result or an <see cref="ApiException"/>,
/// so every page shows the API's message rather than "Response status code does not indicate
/// success: 400".
/// </summary>
public static class HttpResponseMessageExtensions
{
    /// <summary>Returns the failure as an <see cref="ApiException"/>, or null if the call succeeded.</summary>
    public static async Task<ApiException?> GetFailureAsync(this HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return null;
        }

        var problem = await ReadProblemAsync(response);
        var errors = problem?.Errors ?? [];
        var message = errors.FirstOrDefault()?.Detail ?? problem?.Detail ?? Describe(response.StatusCode);

        return new ApiException(response.StatusCode, message, errors);
    }

    public static async Task EnsureSuccessAsync(this HttpResponseMessage response)
    {
        var failure = await response.GetFailureAsync();

        if (failure is not null)
        {
            throw failure;
        }
    }

    /// <summary>Ensures the call succeeded, then reads its body.</summary>
    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        await response.EnsureSuccessAsync();

        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new ApiException(response.StatusCode, "The server sent an empty response.", []);
    }

    /// <summary>Like <see cref="ReadAsync{T}"/>, but a 404 is an answer rather than a failure.</summary>
    public static async Task<T?> ReadOrNullIfNotFoundAsync<T>(this HttpResponseMessage response)
        where T : class
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        return await response.ReadAsync<T>();
    }

    private static async Task<ApiErrorResponse?> ReadProblemAsync(HttpResponseMessage response)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // Not every failure has a problem body: the bearer handler's 401 is empty, and a proxy
            // in front of the API may answer with HTML. The status then has to speak for itself.
            return null;
        }
    }

    private static string Describe(HttpStatusCode statusCode) => (int)statusCode switch
    {
        401 => "Your session has expired. Please sign in again.",
        403 => "You don't have access to that.",
        404 => "That could not be found.",
        429 => "Too many attempts. Please wait a minute and try again.",
        >= 500 => "The server ran into a problem. Please try again.",
        _ => "The request could not be completed."
    };
}
