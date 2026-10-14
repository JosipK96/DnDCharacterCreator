using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetClass;

public class SetCharacterClassCommandValidator : AbstractValidator<SetCharacterClassCommand>
{
    public SetCharacterClassCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.DraftIdRequired);
        RuleFor(x => x.ClassId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.ClassIdRequired);
    }
}