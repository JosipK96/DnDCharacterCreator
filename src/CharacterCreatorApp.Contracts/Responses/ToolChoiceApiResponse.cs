namespace CharacterCreatorApp.Contracts.Responses;

/// <summary>A tool choice the draft owes: OfferedBy (a race, background or class) lets the player pick Count of Options.</summary>
public record ToolChoiceApiResponse(string OfferedBy, int Count, List<ProficiencyApiResponse> Options);
