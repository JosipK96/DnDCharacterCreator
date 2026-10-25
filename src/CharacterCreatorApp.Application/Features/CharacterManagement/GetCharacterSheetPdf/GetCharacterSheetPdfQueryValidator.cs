using CharacterCreatorApp.Application.Errors;

using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterSheetPdf;

public class GetCharacterSheetPdfQueryValidator : AbstractValidator<GetCharacterSheetPdfQuery>
{
    public GetCharacterSheetPdfQueryValidator()
    {
        RuleFor(x => x.CharacterId).NotEmpty().WithErrorCode(CharacterErrors.Codes.CharacterIdRequired);
        RuleFor(x => x.Paper).IsInEnum().WithErrorCode(CharacterErrors.Codes.SheetPaperInvalid);
    }
}
