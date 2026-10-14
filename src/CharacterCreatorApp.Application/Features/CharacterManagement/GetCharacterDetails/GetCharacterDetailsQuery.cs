using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterDetails;

public record GetCharacterDetailsQuery(Guid CharacterId) : IQuery<Response<CharacterDetailsReadModel>>;