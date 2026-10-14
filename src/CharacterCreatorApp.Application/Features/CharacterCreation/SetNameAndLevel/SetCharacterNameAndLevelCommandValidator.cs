using CharacterCreatorApp.Domain.ValueObjects;

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
        RuleFor(x => x.Edition)
            .Must(RulesEdition.IsKnown)
            .WithErrorCode(CharacterDraftErrors.Codes.InvalidEdition)
            .WithMessage($"Edition must be {RulesEdition.Edition2014} or {RulesEdition.Edition2024}.");
    }
}
