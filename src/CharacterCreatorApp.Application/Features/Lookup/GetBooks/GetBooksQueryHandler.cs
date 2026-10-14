using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetBooks;

public class GetBooksQueryHandler(IBookRepository bookRepository) : IQueryHandler<GetBooksQuery, Response<List<BookReadModel>>>
{
    private readonly IBookRepository _bookRepository = bookRepository;

    public async Task<Response<List<BookReadModel>>> Handle(GetBooksQuery query, CancellationToken cancellationToken)
    {
        return await _bookRepository.GetAllForEditionAsync(query.Edition, cancellationToken);
    }
}
