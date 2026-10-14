using CharacterCreatorApp.Application.Errors;

using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.DeleteCharacter;

public class DeleteCharacterCommandValidator : AbstractValidator<DeleteCharacterCommand>
{
    public DeleteCharacterCommandValidator()
    {
        RuleFor(x => x.CharacterId).NotEmpty().WithErrorCode(CharacterErrors.Codes.CharacterIdRequired);
    }
}