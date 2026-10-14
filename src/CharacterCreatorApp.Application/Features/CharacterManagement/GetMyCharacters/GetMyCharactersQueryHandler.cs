using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Filtering;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.GetMyCharacters;

public class GetMyCharactersQueryHandler(
    ICharacterRepository characterRepository,
    IUserContext userContext) : IQueryHandler<GetMyCharactersQuery, Response<PagedResult<CharacterReadModel>>>
{
    private readonly ICharacterRepository _characterRepository = characterRepository;
    private readonly IUserContext _userContext = userContext;

    public async Task<Response<PagedResult<CharacterReadModel>>> Handle(GetMyCharactersQuery query, CancellationToken cancellationToken)
    {
        var characters = await _characterRepository.GetFilteredCharactersAsync(_userContext.UserId, query.Filter, cancellationToken);

        return characters;
    }
}
