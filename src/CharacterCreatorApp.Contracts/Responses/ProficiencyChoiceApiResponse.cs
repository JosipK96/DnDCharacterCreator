namespace CharacterCreatorApp.Contracts.Responses;

public record ProficiencyChoiceApiResponse(string Type, int Count, List<ProficiencyApiResponse> Options);