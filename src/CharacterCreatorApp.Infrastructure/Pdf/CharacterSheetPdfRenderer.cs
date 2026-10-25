using CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterSheetPdf;
using CharacterCreatorApp.Application.ReadModels;

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace CharacterCreatorApp.Infrastructure.Pdf;

/// <summary>Renders the sheet with QuestPDF under its Community licence (checked on #29).</summary>
public sealed class CharacterSheetPdfRenderer : ICharacterSheetPdfRenderer
{
    static CharacterSheetPdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        // Lato covers Latin, Greek and Cyrillic, but a player may name a character with an emoji
        // or in Japanese: draw a placeholder for those letters rather than refuse the whole sheet.
        // shortcut: no fallback font is bundled; add one (Noto) if the group writes in such scripts.
        QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;
    }

    public byte[] Render(CharacterDetailsReadModel character, SheetPaper paper) =>
        new CharacterSheetDocument(character, paper).GeneratePdf();
}
