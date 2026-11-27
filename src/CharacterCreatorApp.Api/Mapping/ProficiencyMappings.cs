using CharacterCreatorApp.Application.ReadModels.Proficiencies;
using CharacterCreatorApp.Contracts.Responses.Proficiencies;

namespace CharacterCreatorApp.Api.Mapping;

/// <summary>Turns the proficiency read models into the responses the client reads.</summary>
public static class ProficiencyMappings
{
    public static ProficiencyApiResponse ToResponse(this ProficiencyReadModel proficiency) =>
        new(Id: proficiency.Id, Name: proficiency.Name, Type: proficiency.Type);

    public static ProficiencyChoiceApiResponse ToResponse(this ProficiencyChoiceReadModel choice) => new(
        Type: choice.Type,
        Count: choice.Count,
        Options: choice.Options.Select(option => option.ToResponse()).ToList());

    public static ToolChoiceApiResponse ToResponse(this ToolChoiceReadModel choice) => new(
        OfferedBy: choice.OfferedBy,
        Count: choice.Count,
        Options: choice.Options.Select(option => option.ToResponse()).ToList());
}
