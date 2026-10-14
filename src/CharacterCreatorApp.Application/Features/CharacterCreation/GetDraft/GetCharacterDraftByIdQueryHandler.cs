using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.GetDraft;

public class GetCharacterDraftByIdQueryHandler(
    ICharacterDraftRepository characterDraftRepository,
    IUserContext userContext) : IQueryHandler<GetCharacterDraftByIdQuery, Response<CharacterDraftDetailsReadModel>>
{
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly IUserContext _userContext = userContext;

    public async Task<Response<CharacterDraftDetailsReadModel>> Handle(GetCharacterDraftByIdQuery query, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetDetailsById(query.DraftId, _userContext.UserId, cancellationToken);

        if (draft is null)
        {
            return CharacterDraftErrors.NotFoundById(query.DraftId);
        }

        return draft;
    }
}
