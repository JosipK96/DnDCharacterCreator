using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentValidation;

namespace CharacterCreatorApp.Application.Features.Lookup.GetBackgrounds;

public class GetBackgroundsQueryValidator : AbstractValidator<GetBackgroundsQuery>
{
    public GetBackgroundsQueryValidator()
    {
        RuleFor(x => x.Edition)
            .Must(RulesEdition.IsKnown)
            .When(x => x.Edition != null)
            .WithErrorCode(BackgroundErrors.Codes.InvalidEdition)
            .WithMessage($"Edition must be {RulesEdition.Edition2014} or {RulesEdition.Edition2024}.");
    }
}
