using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Validation;

using FluentValidation;

namespace CharacterCreatorApp.Application.Features.Lookup.GetBooks;

public class GetBooksQueryValidator : AbstractValidator<GetBooksQuery>
{
    public GetBooksQueryValidator()
    {
        RuleFor(x => x.Edition).MustBeKnownEdition(BookErrors.Codes.InvalidEdition);
    }
}
