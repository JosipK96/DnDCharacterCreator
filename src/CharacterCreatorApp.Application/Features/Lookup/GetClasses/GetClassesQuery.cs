using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetClasses;

/// <summary>The classes of one rules edition, or of both when Edition is null.</summary>
public record GetClassesQuery(string? Edition = null) : IQuery<Response<List<ClassReadModel>>>;
