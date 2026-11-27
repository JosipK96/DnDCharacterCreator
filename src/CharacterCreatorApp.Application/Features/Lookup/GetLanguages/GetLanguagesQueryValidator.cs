using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Validation;

using FluentValidation;

namespace CharacterCreatorApp.Application.Features.Lookup.GetLanguages;

public class GetLanguagesQueryValidator : AbstractValidator<GetLanguagesQuery>
{
    public GetLanguagesQueryValidator()
    {
        RuleFor(x => x.Edition)
            .MustBeKnownEdition(LanguageErrors.Codes.InvalidEdition)
            .When(x => x.Edition != null);
    }
}
