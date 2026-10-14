using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Errors;

public static class RaceErrors
{
    public static class Codes
    {
        public const string InvalidEdition = "Race.InvalidEdition";
    }

    public static Error NotFoundById(string raceId) => Error.NotFound(code: "Race.NotFound", description: $"Race not found by id {raceId}");
}