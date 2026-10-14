using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetBackground
{
    public class SetCharacterBackgroundCommandValidator : AbstractValidator<SetCharacterBackgroundCommand>
    {
        public SetCharacterBackgroundCommandValidator()
        {
            RuleFor(x => x.DraftId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.DraftIdRequired);
            RuleFor(x => x.BackgroundId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.BackgroundIdRequired);
        }
    }
}
