using System.Collections.Frozen;

using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetAbilityScores
{
    public class SetCharacterAbilityScoresCommandValidator : AbstractValidator<SetCharacterAbilityScoresCommand>
    {
        private static readonly FrozenSet<string> ValidAbilityScores = new[]
        {
            "STR",
            "DEX",
            "CON",
            "INT",
            "WIS",
            "CHA"
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        public SetCharacterAbilityScoresCommandValidator()
        {
            RuleFor(x => x.DraftId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.DraftIdRequired);

            RuleFor(x => x.Scores)
                .Must(scores => scores.Count == 6)
                .WithErrorCode(CharacterDraftErrors.Codes.InvalidAbilityScoreCount)
            .WithMessage("Must provide exactly 6 ability scores");

            RuleFor(x => x.Scores.Keys)
                .Must(keys => keys.All(ValidAbilityScores.Contains))
                .WithErrorCode(CharacterDraftErrors.Codes.InvalidAbilityScoreKeys)
            .WithMessage("Invalid ability score key names. Valid names are STR, DEX, CON, INT, WIS, CHA");
        }
    }
}
