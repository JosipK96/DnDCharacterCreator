using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Errors
{
    public static class SubclassErrors
    {
        public static Error NotFoundById(string subclassId) => Error.NotFound(code: "Subclass.NotFound", description: $"Subclass not found by id {subclassId}");
    }
}
