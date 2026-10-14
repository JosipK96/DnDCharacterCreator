using FluentValidation;

namespace CharacterCreatorApp.Application.Features.Lookup.GetSpells;

public class GetSpellsQueryValidator : AbstractValidator<GetSpellsQuery>
{
    public GetSpellsQueryValidator()
    {
        When(x => x.Level.HasValue, () =>
        {
            RuleFor(x => x.Level)
                .InclusiveBetween(0, 9)
                .WithMessage("Spell level must be between 0 (cantrip) and 9");
        });

        When(x => x.MaxLevel.HasValue, () =>
        {
            RuleFor(x => x.MaxLevel)
                .InclusiveBetween(0, 9)
                .WithMessage("Max spell level must be between 0 and 9");
        });
    }
}