using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails
{
    /// <summary>
    /// A request MVC could not bind, such as malformed JSON or a missing body. It never reached a
    /// handler, so it has no error code of its own; every entry gets <see cref="InvalidRequestCode"/>
    /// and the name of the field that failed, in the same shape as <see cref="CustomProblemDetails"/>.
    /// </summary>
    public class CustomValidationProblemDetails : ValidationProblemDetails
    {
        public const string InvalidRequestCode = "Request.Invalid";

        private const string InvalidPropertyText = "Invalid Property";

        public CustomValidationProblemDetails()
        {
        }

        public CustomValidationProblemDetails(ModelStateDictionary modelState)
        {
            Errors = modelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .SelectMany(entry => entry.Value!.Errors.Select(error => ProblemDetailsError.Create(
                    InvalidRequestCode,
                    propertyName: entry.Key,
                    detail: string.IsNullOrEmpty(error.ErrorMessage) ? InvalidPropertyText : error.ErrorMessage)))
                .ToList();
        }

        [JsonPropertyName("errors")]
        public new IReadOnlyList<ProblemDetailsError> Errors { get; } = [];
    }
}
