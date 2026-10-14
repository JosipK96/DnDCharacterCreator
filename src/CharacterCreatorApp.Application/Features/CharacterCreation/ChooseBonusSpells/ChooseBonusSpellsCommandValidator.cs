using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseBonusSpells;

public class ChooseBonusSpellsCommandValidator : AbstractValidator<ChooseBonusSpellsCommand>
{
    public ChooseBonusSpellsCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.DraftIdRequired);
        RuleFor(x => x.RaceSpellIds).NotNull().WithErrorCode(CharacterDraftErrors.Codes.BonusSpellsRequired);
        RuleFor(x => x.FeatSpells).NotNull().WithErrorCode(CharacterDraftErrors.Codes.BonusSpellsRequired);
        RuleForEach(x => x.FeatSpells).ChildRules(pick =>
        {
            pick.RuleFor(p => p.FeatId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.BonusSpellsRequired);
            pick.RuleFor(p => p.ClassId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.BonusSpellsRequired);
            pick.RuleFor(p => p.SpellIds).NotNull().WithErrorCode(CharacterDraftErrors.Codes.BonusSpellsRequired);
        });
    }
}
