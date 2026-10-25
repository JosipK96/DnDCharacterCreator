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
    }

    public byte[] Render(CharacterDetailsReadModel character, SheetPaper paper) =>
        new CharacterSheetDocument(character, paper).GeneratePdf();
}
