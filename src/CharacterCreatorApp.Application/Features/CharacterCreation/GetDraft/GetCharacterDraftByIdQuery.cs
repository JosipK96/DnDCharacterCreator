using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.GetDraft;

public record GetCharacterDraftByIdQuery(Guid DraftId) : IQuery<Response<CharacterDraftDetailsReadModel>>;