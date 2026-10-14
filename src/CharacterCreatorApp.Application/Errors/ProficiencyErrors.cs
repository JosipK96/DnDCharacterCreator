using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Errors;

public static class ProficiencyErrors
{
    public static Error NotFoundById(string proficiencyId) => Error.NotFound(code: "Proficiency.NotFound", description: $"Proficiency not found by id {proficiencyId}");
}
