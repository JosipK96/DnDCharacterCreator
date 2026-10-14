namespace CharacterCreatorApp.Contracts.Requests.CharacterDrafts;

/// <summary>One list of proficiency ids per tool choice, in the order GET tool-choices lists them.</summary>
public record ChooseToolsApiRequest(List<List<string>> Picks);
