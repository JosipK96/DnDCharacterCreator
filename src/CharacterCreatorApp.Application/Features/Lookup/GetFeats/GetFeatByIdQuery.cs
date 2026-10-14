using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetFeats;

public record GetFeatByIdQuery(string Id) : IQuery<Response<FeatReadModel>>;