using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentValidation;

namespace CharacterCreatorApp.Application.Features.Lookup.GetRaces;

public class GetRacesQueryValidator : AbstractValidator<GetRacesQuery>
{
    public GetRacesQueryValidator()
    {
        RuleFor(x => x.Edition)
            .Must(RulesEdition.IsKnown)
            .When(x => x.Edition != null)
            .WithErrorCode(RaceErrors.Codes.InvalidEdition)
            .WithMessage($"Edition must be {RulesEdition.Edition2014} or {RulesEdition.Edition2024}.");
    }
}
