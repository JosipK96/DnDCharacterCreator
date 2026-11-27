using CharacterCreatorApp.Application.ReadModels.Books;
using CharacterCreatorApp.Contracts.Responses.Books;

namespace CharacterCreatorApp.Api.Mapping;

/// <summary>Turns the book read models into the responses the client reads.</summary>
public static class BookMappings
{
    public static BookApiResponse ToResponse(this BookReadModel book) =>
        new(Code: book.Code, Name: book.Name, Edition: book.Edition, IsPlayersHandbook: book.IsPlayersHandbook);

    public static HiddenOptionsApiResponse ToResponse(this HiddenOptionsReadModel hidden) => new(
        Count: hidden.Count,
        Books: hidden.Books.Select(book => book.ToResponse()).ToList());

    public static HiddenOptionsBookApiResponse ToResponse(this HiddenOptionsBookReadModel book) =>
        new(Code: book.Code, Name: book.Name, Count: book.Count);
}
