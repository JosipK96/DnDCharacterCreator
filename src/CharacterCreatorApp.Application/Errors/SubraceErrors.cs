using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Errors
{
    public static class SubraceErrors
    {
        public static Error NotFoundById(string subraceId) => Error.NotFound(code: "Subrace.NotFound", description: $"Subrace not found by id {subraceId}");
    }
}
