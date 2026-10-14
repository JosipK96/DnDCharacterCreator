namespace CharacterCreatorApp.Contracts.Requests.CharacterDrafts;

/// <summary>
/// The book codes ("EFA", "XGE") the character may take its choices from; its edition's Player's
/// Handbook is always allowed, listed or not.
/// </summary>
public record SetAllowedBooksApiRequest(List<string> Books);
