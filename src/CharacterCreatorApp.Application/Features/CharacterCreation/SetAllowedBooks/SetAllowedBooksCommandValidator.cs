using FluentValidation;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetAllowedBooks;

public class SetAllowedBooksCommandValidator : AbstractValidator<SetAllowedBooksCommand>
{
    public SetAllowedBooksCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty().WithErrorCode(CharacterDraftErrors.Codes.DraftIdRequired);
        RuleFor(x => x.Books).NotNull().WithErrorCode(CharacterDraftErrors.Codes.BooksRequired);
    }
}
