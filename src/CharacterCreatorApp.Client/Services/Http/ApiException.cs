using System.Net;

using CharacterCreatorApp.Client.Models;

namespace CharacterCreatorApp.Client.Services.Http;

/// <summary>
/// The API answered with an error. Message is the API's own description, ready to show as it
/// is; Errors lists every error with its code, for a page that has to react to a specific one.
/// </summary>
public sealed class ApiException(HttpStatusCode statusCode, string message, IReadOnlyList<ApiError> errors)
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    public IReadOnlyList<ApiError> Errors { get; } = errors;

    public bool HasCode(string code) => Errors.Any(e => e.Code == code);
}
