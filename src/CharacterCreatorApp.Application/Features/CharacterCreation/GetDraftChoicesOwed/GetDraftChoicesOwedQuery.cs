using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels.Characters;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.GetDraftChoicesOwed;

public record GetDraftChoicesOwedQuery(Guid DraftId) : IQuery<Response<DraftChoicesOwedReadModel>>;
