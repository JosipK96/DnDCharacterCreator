using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetAllowedBooks;

/// <summary>
/// The books, by code, the character may take its choices from, besides its edition's Player's
/// Handbook, which it always may.
/// </summary>
public record SetAllowedBooksCommand(Guid DraftId, List<string> Books) : ICommand<Response>;
