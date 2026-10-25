using CharacterCreatorApp.Application.ReadModels;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterSheetPdf;

/// <summary>
/// Draws a character sheet as a PDF. A PDF library implements it in Infrastructure, so
/// Application does not reference one.
/// </summary>
public interface ICharacterSheetPdfRenderer
{
    /// <summary>The PDF's bytes: the sheet on page 1, then features and spells on as many pages as they take.</summary>
    byte[] Render(CharacterDetailsReadModel character, SheetPaper paper);
}
