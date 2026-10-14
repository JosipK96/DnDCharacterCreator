using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;

namespace CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;

/// <summary>
/// The body of every error response the API sends: RFC 9457 problem details plus the errors
/// that caused it, each with its code.
/// </summary>
public class CustomProblemDetails : ProblemDetails
{
    [JsonPropertyName("errors")]
    public List<ProblemDetailsError> Errors { get; } = [];
}
