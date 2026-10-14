using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

namespace CharacterCreatorApp.Api.IntegrationTests.Fixtures;

/// <summary>
/// The error body every failing endpoint returns, read the way a client reads it rather than
/// through the Api's own types, so a change to the wire shape fails these tests.
/// </summary>
internal sealed record ProblemResponse(int Status, string? Title, string? Detail, List<ProblemResponseError> Errors);

internal sealed record ProblemResponseError(string Code, string? PropertyName, string? Detail);

internal static class ProblemResponseExtensions
{
    public static async Task<ProblemResponse> ReadProblemAsync(this HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(TestContext.Current.CancellationToken);

        return problem ?? throw new InvalidOperationException("The response had no problem body.");
    }

    /// <summary>Asserts the response failed with this status and exactly one error, with this code.</summary>
    public static async Task ShouldFailWithAsync(this HttpResponseMessage response, HttpStatusCode status, string code)
    {
        response.StatusCode.Should().Be(status);
        var problem = await response.ReadProblemAsync();
        problem.Errors.Should().ContainSingle().Which.Code.Should().Be(code);
    }
}
