using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Errors;

public static class ClassErrors
{
    public static class Codes
    {
        public const string InvalidEdition = "Class.InvalidEdition";
    }

    public static Error NotFoundById(string classId) => Error.NotFound(code: "Class.NotFound", description: $"Class not found by id {classId}");
}
