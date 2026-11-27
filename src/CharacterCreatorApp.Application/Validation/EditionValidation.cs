using CharacterCreatorApp.Domain.ValueObjects;

using FluentValidation;

namespace CharacterCreatorApp.Application.Validation;

/// <summary>
/// The edition rule every validator that takes an edition shares, so the accepted values and the
/// message are written once. Each caller passes its own entity's code (RaceErrors.Codes.InvalidEdition...),
/// so the client still learns which request was wrong.
/// </summary>
public static class EditionValidation
{
    /// <summary>The value is 2014 or 2024. Add <c>.When(x => x.Edition != null)</c> where no edition means 2014.</summary>
    public static IRuleBuilderOptions<T, string?> MustBeKnownEdition<T>(this IRuleBuilder<T, string?> rule, string errorCode) =>
        rule.Must(RulesEdition.IsKnown)
            .WithErrorCode(errorCode)
            .WithMessage($"Edition must be {RulesEdition.Edition2014} or {RulesEdition.Edition2024}.");
}
