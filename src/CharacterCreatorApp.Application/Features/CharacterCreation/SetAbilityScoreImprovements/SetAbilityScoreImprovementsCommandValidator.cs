using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetAbilityScoreImprovements;

public class SetAbilityScoreImprovementsCommandValidator : AbstractValidator<SetAbilityScoreImprovementsCommand>
{
    public SetAbilityScoreImprovementsCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.DraftIdRequired);

        RuleFor(x => x.Improvements).NotNull().WithErrorCode(CharacterDraftErrors.Codes.ImprovementsRequired);

        RuleFor(x => x.FeatAbilityIncreases)
            .Must((command, increases) => increases == null || increases.Count == command.FeatIds.Count)
            .WithMessage("Give one ability increase per feat, in the order of the feats, or none at all.")
            .WithErrorCode(CharacterDraftErrors.Codes.FeatAbilityIncreasesMismatch);
    }
}
