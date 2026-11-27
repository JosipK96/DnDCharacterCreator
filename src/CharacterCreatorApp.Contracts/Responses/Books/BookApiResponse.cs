namespace CharacterCreatorApp.Contracts.Responses.Books;

/// <summary>A book the reference data comes from, by the code each row's Source holds.</summary>
public record BookApiResponse(string Code, string Name, string Edition, bool IsPlayersHandbook);
