using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetSubclasses;

public record GetSubclassesQuery(string ClassId) : IQuery<Response<List<SubclassReadModel>>>;