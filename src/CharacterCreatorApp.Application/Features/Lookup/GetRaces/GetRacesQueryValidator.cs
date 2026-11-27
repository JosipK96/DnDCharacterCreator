using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Validation;

using FluentValidation;

namespace CharacterCreatorApp.Application.Features.Lookup.GetRaces;

public class GetRacesQueryValidator : AbstractValidator<GetRacesQuery>
{
    public GetRacesQueryValidator()
    {
        RuleFor(x => x.Edition)
            .MustBeKnownEdition(RaceErrors.Codes.InvalidEdition)
            .When(x => x.Edition != null);
    }
}
