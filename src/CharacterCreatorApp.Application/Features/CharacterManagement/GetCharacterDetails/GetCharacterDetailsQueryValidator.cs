using CharacterCreatorApp.Application.Errors;

using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterDetails;

public class GetCharacterDetailsQueryValidator : AbstractValidator<GetCharacterDetailsQuery>
{
    public GetCharacterDetailsQueryValidator()
    {
        RuleFor(x => x.CharacterId).NotEmpty().WithErrorCode(CharacterErrors.Codes.CharacterIdRequired);
    }
}