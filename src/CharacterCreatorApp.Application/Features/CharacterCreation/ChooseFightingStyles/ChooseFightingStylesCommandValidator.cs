using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseFightingStyles;

public class ChooseFightingStylesCommandValidator : AbstractValidator<ChooseFightingStylesCommand>
{
    public ChooseFightingStylesCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.DraftIdRequired);
        RuleFor(x => x.FeatIds).NotNull().WithErrorCode(CharacterDraftErrors.Codes.FightingStyleIdsRequired);
    }
}
