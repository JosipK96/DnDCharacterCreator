using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentValidation;

namespace CharacterCreatorApp.Application.Features.Lookup.GetClasses;

public class GetClassesQueryValidator : AbstractValidator<GetClassesQuery>
{
    public GetClassesQueryValidator()
    {
        RuleFor(x => x.Edition)
            .Must(RulesEdition.IsKnown)
            .When(x => x.Edition != null)
            .WithErrorCode(ClassErrors.Codes.InvalidEdition)
            .WithMessage($"Edition must be {RulesEdition.Edition2014} or {RulesEdition.Edition2024}.");
    }
}
