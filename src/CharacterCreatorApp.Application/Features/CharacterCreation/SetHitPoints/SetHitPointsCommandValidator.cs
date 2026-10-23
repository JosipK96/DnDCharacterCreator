using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetHitPoints;

public class SetHitPointsCommandValidator : AbstractValidator<SetHitPointsCommand>
{
    public SetHitPointsCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.DraftIdRequired);

        // The die's size is the class's, so the handler checks the top
        RuleFor(x => x.Result).GreaterThanOrEqualTo(1).WithErrorCode(CharacterDraftErrors.Codes.InvalidHitDieResult);
    }
}
