using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseRaceSkills;

public class ChooseRaceSkillsCommandValidator : AbstractValidator<ChooseRaceSkillsCommand>
{
    public ChooseRaceSkillsCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.DraftIdRequired);

        RuleFor(x => x.ProficiencyIds).NotNull().WithErrorCode(CharacterDraftErrors.Codes.ProficiencyIdsRequired);

        RuleFor(x => x.ProficiencyIds)
            .Must(ids => ids.All(id => !string.IsNullOrWhiteSpace(id)))
            .When(x => x.ProficiencyIds != null)
            .WithErrorCode(CharacterDraftErrors.Codes.BlankProficiencyId)
            .WithMessage("All proficiency IDs must be valid strings");
    }
}
