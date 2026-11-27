using System.Collections.Frozen;

using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Domain.ValueObjects;

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

            RuleFor(x => x.Method)
                .Must(AbilityScoreMethod.IsKnown)
                .WithErrorCode(CharacterDraftErrors.Codes.InvalidAbilityScoreMethod)
            .WithMessage($"Method must be one of {string.Join(", ", AbilityScoreMethod.All)}");

            // Only once the method and the six scores are valid on their own, so a bad request
            // gets the one error that explains it
            RuleFor(x => x.Scores)
                .Must((command, scores) => AbilityScoreMethod.Allows(command.Method, scores.Values))
                .When(command => AbilityScoreMethod.IsKnown(command.Method) && command.Scores.Count == 6)
                .WithErrorCode(CharacterDraftErrors.Codes.AbilityScoresDoNotFitMethod)
            .WithMessage(command => $"These are not scores {CharacterErrors.AbilityScoreMethodName(command.Method)} can produce");
        }
    }
}
