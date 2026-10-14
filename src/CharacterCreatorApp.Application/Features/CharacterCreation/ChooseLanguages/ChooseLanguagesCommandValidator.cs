using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseLanguages;

public class ChooseLanguagesCommandValidator : AbstractValidator<ChooseLanguagesCommand>
{
    public ChooseLanguagesCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.DraftIdRequired);

        RuleFor(x => x.LanguageIds).NotNull().WithErrorCode(CharacterDraftErrors.Codes.LanguageIdsRequired);

        RuleFor(x => x.LanguageIds)
            .Must(ids => ids.All(id => !string.IsNullOrWhiteSpace(id)))
            .When(x => x.LanguageIds != null)
            .WithErrorCode(CharacterDraftErrors.Codes.BlankLanguageId)
            .WithMessage("All language IDs must be valid strings");
    }
}
