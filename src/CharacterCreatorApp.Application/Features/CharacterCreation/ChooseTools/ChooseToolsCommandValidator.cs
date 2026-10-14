using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseTools;

public class ChooseToolsCommandValidator : AbstractValidator<ChooseToolsCommand>
{
    public ChooseToolsCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.DraftIdRequired);

        RuleFor(x => x.Picks)
            .Must(picks => picks != null && picks.All(pick => pick != null))
            .WithErrorCode(CharacterDraftErrors.Codes.ToolPicksRequired)
            .WithMessage("Give one list of tools per tool choice");

        RuleFor(x => x.Picks)
            .Must(picks => picks.All(pick => pick.All(id => !string.IsNullOrWhiteSpace(id))))
            .When(x => x.Picks != null && x.Picks.All(pick => pick != null))
            .WithErrorCode(CharacterDraftErrors.Codes.BlankProficiencyId)
            .WithMessage("All proficiency IDs must be valid strings");
    }
}
