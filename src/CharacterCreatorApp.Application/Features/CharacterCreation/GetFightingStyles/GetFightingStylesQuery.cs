using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.GetFightingStyles;

public record GetFightingStylesQuery(Guid DraftId) : IQuery<Response<FightingStylesReadModel>>;
