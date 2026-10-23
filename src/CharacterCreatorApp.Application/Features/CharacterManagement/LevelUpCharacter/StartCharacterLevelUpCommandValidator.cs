using CharacterCreatorApp.Application.Errors;

using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.LevelUpCharacter;

public class StartCharacterLevelUpCommandValidator : AbstractValidator<StartCharacterLevelUpCommand>
{
    public StartCharacterLevelUpCommandValidator()
    {
        RuleFor(x => x.CharacterId).NotEmpty().WithErrorCode(CharacterErrors.Codes.CharacterIdRequired);
    }
}
