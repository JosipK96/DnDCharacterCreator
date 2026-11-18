namespace CharacterCreatorApp.Contracts.Responses;

/// <summary>
/// What a wizard list leaves out for the books the draft has not ticked: how many entries, and the
/// books that hold them, in the books list's order.
/// </summary>
public record HiddenOptionsApiResponse(int Count, List<HiddenOptionsBookApiResponse> Books);

/// <summary>An unticked book, by the code a row's Source holds, and how many of the list's entries it holds.</summary>
public record HiddenOptionsBookApiResponse(string Code, string Name, int Count);
