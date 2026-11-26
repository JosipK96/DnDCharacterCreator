using CharacterCreatorApp.Domain.ValueObjects;

using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.GetFeatEligibility;

public class GetFeatEligibilityQueryValidator : AbstractValidator<GetFeatEligibilityQuery>
{
    public GetFeatEligibilityQueryValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.DraftIdRequired);

        RuleForEach(x => x.Increases)
            .Must(ability => AbilityScore.TryNormalize(ability, out _))
            .WithMessage("'{PropertyValue}' is not an ability score.")
            .WithErrorCode(CharacterDraftErrors.Codes.InvalidAsiAbility);
    }
}
