namespace CharacterCreatorApp.Contracts.Responses.Proficiencies;

public record ProficiencyChoiceApiResponse(string Type, int Count, List<ProficiencyApiResponse> Options);