using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Validation;

using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetNameAndLevel;

public class SetCharacterNameAndLevelCommandValidator : AbstractValidator<SetCharacterNameAndLevelCommand>
{
    public SetCharacterNameAndLevelCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.DraftIdRequired);
        RuleFor(x => x.Name).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.NameRequired);
        RuleFor(x => x.Name).MaximumLength(100).WithErrorCode(CharacterDraftErrors.Codes.NameTooLong);
        RuleFor(x => x.Level).InclusiveBetween(1, 20).WithErrorCode(CharacterDraftErrors.Codes.InvalidLevel);
        RuleFor(x => x.Edition).MustBeKnownEdition(CharacterDraftErrors.Codes.InvalidEdition);
    }
}
