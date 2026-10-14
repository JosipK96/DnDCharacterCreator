using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseProficiencies;

public class ChooseProficienciesCommandValidator : AbstractValidator<ChooseProficienciesCommand>
{
    public ChooseProficienciesCommandValidator()
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