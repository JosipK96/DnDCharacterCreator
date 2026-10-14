using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetSubclasses;

/// <summary>A class's subclasses; Sources, when given, keeps only those from these books.</summary>
public record GetSubclassesQuery(string ClassId, IReadOnlyCollection<string>? Sources = null) : IQuery<Response<List<SubclassReadModel>>>;