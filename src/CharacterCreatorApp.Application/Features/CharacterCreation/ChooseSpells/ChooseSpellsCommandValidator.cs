using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseSpells;

public class ChooseSpellsCommandValidator : AbstractValidator<ChooseSpellsCommand>
{
    public ChooseSpellsCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.DraftIdRequired);
        RuleFor(x => x.SpellIds).NotNull().WithErrorCode(CharacterDraftErrors.Codes.SpellIdsRequired);
    }
}