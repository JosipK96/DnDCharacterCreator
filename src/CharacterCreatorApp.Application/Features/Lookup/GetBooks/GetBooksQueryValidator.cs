using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentValidation;

namespace CharacterCreatorApp.Application.Features.Lookup.GetBooks;

public class GetBooksQueryValidator : AbstractValidator<GetBooksQuery>
{
    public GetBooksQueryValidator()
    {
        RuleFor(x => x.Edition)
            .Must(RulesEdition.IsKnown)
            .WithErrorCode(BookErrors.Codes.InvalidEdition)
            .WithMessage($"Edition must be {RulesEdition.Edition2014} or {RulesEdition.Edition2024}.");
    }
}
