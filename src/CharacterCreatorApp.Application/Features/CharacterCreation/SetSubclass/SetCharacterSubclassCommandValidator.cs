using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetSubclass;

public class SetCharacterSubclassCommandValidator : AbstractValidator<SetCharacterSubclassCommand>
{
    public SetCharacterSubclassCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.DraftIdRequired);

        RuleFor(x => x.SubclassId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.SubclassIdRequired);
    }
}