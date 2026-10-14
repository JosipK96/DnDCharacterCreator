using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Errors;

public static class BackgroundErrors
{
    public static class Codes
    {
        public const string InvalidEdition = "Background.InvalidEdition";
    }

    public static Error NotFoundById(string backgroundId) => Error.NotFound(code: "Background.NotFound", description: $"Background not found by id {backgroundId}");
}
