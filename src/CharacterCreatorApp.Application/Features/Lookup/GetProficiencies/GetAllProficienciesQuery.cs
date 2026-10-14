using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetProficiencies;

public record GetAllProficienciesQuery : IQuery<Response<List<ProficiencyReadModel>>>;
