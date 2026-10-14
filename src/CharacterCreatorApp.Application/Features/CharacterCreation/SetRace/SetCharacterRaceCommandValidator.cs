using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetRace;

public class SetCharacterRaceCommandValidator : AbstractValidator<SetCharacterRaceCommand>
{
    public SetCharacterRaceCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.DraftIdRequired);
        RuleFor(x => x.RaceId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.RaceIdRequired);
    }
}