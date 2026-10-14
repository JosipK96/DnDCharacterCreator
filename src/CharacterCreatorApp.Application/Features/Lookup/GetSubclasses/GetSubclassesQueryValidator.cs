using FluentValidation;

namespace CharacterCreatorApp.Application.Features.Lookup.GetSubclasses;

public class GetSubclassesQueryValidator : AbstractValidator<GetSubclassesQuery>
{
    public GetSubclassesQueryValidator()
    {
        RuleFor(x => x.ClassId)
            .NotEmpty()
            .WithMessage("ClassId is required");
    }
}