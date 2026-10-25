namespace CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterSheetPdf;

/// <summary>A rendered character sheet and the file name to save it under ("Aragorn.pdf").</summary>
public sealed record CharacterSheetPdf(string FileName, byte[] Content);
