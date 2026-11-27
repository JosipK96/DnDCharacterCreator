using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Validation;

using FluentValidation;

namespace CharacterCreatorApp.Application.Features.Lookup.GetBackgrounds;

public class GetBackgroundsQueryValidator : AbstractValidator<GetBackgroundsQuery>
{
    public GetBackgroundsQueryValidator()
    {
        RuleFor(x => x.Edition)
            .MustBeKnownEdition(BackgroundErrors.Codes.InvalidEdition)
            .When(x => x.Edition != null);
    }
}
