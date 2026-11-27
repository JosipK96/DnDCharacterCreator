using FluentValidation;

using CharacterCreatorApp.Application.Errors;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetHitPoints;

public class SetHitPointsCommandValidator : AbstractValidator<SetHitPointsCommand>
{
    public SetHitPointsCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.DraftIdRequired);

        RuleFor(x => x.Results).NotNull().WithErrorCode(CharacterDraftErrors.Codes.HitDieResultsRequired);

        // The die's size is the class's and the levels are the draft's, so the handler checks those
        RuleForEach(x => x.Results.Values)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode(CharacterDraftErrors.Codes.InvalidHitDieResult)
            .When(x => x.Results is not null);
    }
}
