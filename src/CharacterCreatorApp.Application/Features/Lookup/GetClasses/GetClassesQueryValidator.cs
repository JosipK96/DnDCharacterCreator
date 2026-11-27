using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Validation;

using FluentValidation;

namespace CharacterCreatorApp.Application.Features.Lookup.GetClasses;

public class GetClassesQueryValidator : AbstractValidator<GetClassesQuery>
{
    public GetClassesQueryValidator()
    {
        RuleFor(x => x.Edition)
            .MustBeKnownEdition(ClassErrors.Codes.InvalidEdition)
            .When(x => x.Edition != null);
    }
}
