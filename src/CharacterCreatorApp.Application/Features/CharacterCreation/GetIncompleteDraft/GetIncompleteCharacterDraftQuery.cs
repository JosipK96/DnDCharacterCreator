using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels.Characters;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.GetIncompleteDraft;

public record GetIncompleteCharacterDraftQuery : IQuery<Response<CharacterDraftDetailsReadModel>>;