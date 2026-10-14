using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetFeats;
    
public record GetFeatsByProficiencyIdQuery(string ProficiencyId) : IQuery<Response<List<FeatReadModel>>>;