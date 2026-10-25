using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterSheetPdf;

public record GetCharacterSheetPdfQuery(Guid CharacterId, SheetPaper Paper) : IQuery<Response<CharacterSheetPdf>>;
