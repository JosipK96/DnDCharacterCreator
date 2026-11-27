using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Validation;

using FluentValidation;

namespace CharacterCreatorApp.Application.Features.Lookup.GetFeats;

public class GetFeatsQueryValidator : AbstractValidator<GetFeatsQuery>
{
    public GetFeatsQueryValidator()
    {
        RuleFor(x => x.Edition)
            .MustBeKnownEdition(FeatErrors.Codes.InvalidEdition)
            .When(x => x.Edition != null);
    }
}
