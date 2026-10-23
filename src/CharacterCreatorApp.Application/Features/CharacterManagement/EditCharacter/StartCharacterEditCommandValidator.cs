using CharacterCreatorApp.Application.Errors;

using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.EditCharacter;

public class StartCharacterEditCommandValidator : AbstractValidator<StartCharacterEditCommand>
{
    public StartCharacterEditCommandValidator()
    {
        RuleFor(x => x.CharacterId).NotEmpty().WithErrorCode(CharacterErrors.Codes.CharacterIdRequired);
    }
}
