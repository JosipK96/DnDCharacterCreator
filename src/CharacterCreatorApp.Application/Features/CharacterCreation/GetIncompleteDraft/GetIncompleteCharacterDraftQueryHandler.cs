using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.GetIncompleteDraft;

public class GetIncompleteCharacterDraftQueryHandler(
    ICharacterDraftRepository characterDraftRepository,
    IUserContext userContext) : IQueryHandler<GetIncompleteCharacterDraftQuery, Response<CharacterDraftDetailsReadModel>>
{
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly IUserContext _userContext = userContext;

    public async Task<Response<CharacterDraftDetailsReadModel>> Handle(GetIncompleteCharacterDraftQuery query, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetIncompleteDraft(_userContext.UserId, cancellationToken);

        if (draft is null)
        {
            return CharacterDraftErrors.NoIncompleteDraft();
        }

        return draft;
    }
}
