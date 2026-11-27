using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Validation;

using FluentValidation;

namespace CharacterCreatorApp.Application.Features.Lookup.GetHiddenOptions;

public class GetHiddenOptionsQueryValidator : AbstractValidator<GetHiddenOptionsQuery>
{
    public GetHiddenOptionsQueryValidator()
    {
        RuleFor(x => x.Edition).MustBeKnownEdition(BookErrors.Codes.InvalidEdition);

        RuleFor(x => x.List)
            .Must(list => HiddenOptionLists.All.Contains(list))
            .WithErrorCode(BookErrors.Codes.InvalidList)
            .WithMessage($"List must be one of {string.Join(", ", HiddenOptionLists.All)}.");

        RuleFor(x => x.ClassId)
            .NotEmpty()
            .When(x => HiddenOptionLists.NeedsClass(x.List))
            .WithErrorCode(BookErrors.Codes.ClassRequired)
            .WithMessage("A class is needed to count its subclasses or spells.");

        RuleFor(x => x.MaxLevel)
            .InclusiveBetween(0, 9)
            .When(x => x.MaxLevel.HasValue)
            .WithErrorCode(BookErrors.Codes.InvalidMaxLevel)
            .WithMessage("Max spell level must be between 0 and 9.");
    }
}
