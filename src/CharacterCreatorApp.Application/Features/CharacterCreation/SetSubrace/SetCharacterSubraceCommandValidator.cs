using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetSubrace;

public class SetCharacterSubraceCommandValidator : AbstractValidator<SetCharacterSubraceCommand>
{
    public SetCharacterSubraceCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.DraftIdRequired);
    }
}