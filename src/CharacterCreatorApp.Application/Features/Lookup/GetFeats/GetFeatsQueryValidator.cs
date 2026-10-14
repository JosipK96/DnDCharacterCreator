using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentValidation;

namespace CharacterCreatorApp.Application.Features.Lookup.GetFeats;

public class GetFeatsQueryValidator : AbstractValidator<GetFeatsQuery>
{
    public GetFeatsQueryValidator()
    {
        RuleFor(x => x.Edition)
            .Must(RulesEdition.IsKnown)
            .When(x => x.Edition != null)
            .WithErrorCode(FeatErrors.Codes.InvalidEdition)
            .WithMessage($"Edition must be {RulesEdition.Edition2014} or {RulesEdition.Edition2024}.");
    }
}
