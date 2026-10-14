using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;

/// <summary>
/// One entry in a problem response's "errors" list. Code is the error's "&lt;Area&gt;.&lt;Reason&gt;"
/// code, which a client can branch or translate on; Detail is the message to show; PropertyName
/// is set when a request field caused the error.
/// </summary>
public sealed class ProblemDetailsError
{
    public string Code { get; init; }
    public string? PropertyName { get; init; }
    public string? Detail { get; init; }

    private ProblemDetailsError(string code, string? propertyName, string? detail)
    {
        Code = code;
        PropertyName = propertyName;
        Detail = detail;
    }

    public static ProblemDetailsError Create(string code, string? propertyName = null, string? detail = null)
        => new(code, propertyName, detail);

    public static ProblemDetailsError From(Error error)
        => new(error.Code, error.FieldName, error.Description);
}
