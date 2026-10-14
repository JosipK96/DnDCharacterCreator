using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.FinalizeCharacter;

public class FinalizeCharacterCommandValidator : AbstractValidator<FinalizeCharacterCommand>
{
    public FinalizeCharacterCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.DraftIdRequired);
    }
}