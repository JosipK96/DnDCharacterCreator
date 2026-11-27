using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels.Proficiencies;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetProficiencies;

public record GetProficiencyByIdQuery(string Id) : IQuery<Response<ProficiencyReadModel>>;