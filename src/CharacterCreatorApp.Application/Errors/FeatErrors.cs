using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Errors;

public static class FeatErrors
{
    public static class Codes
    {
        public const string InvalidEdition = "Feat.InvalidEdition";
    }

    public static Error NotFoundById(string featId) => Error.NotFound(code: "Feat.NotFound", description: $"Feat not found by id {featId}");
}
