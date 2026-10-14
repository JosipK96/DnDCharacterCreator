using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetClasses;

/// <summary>
/// The classes of one rules edition, or of both when Edition is null.
/// Sources, when given, keeps only those from these books (CharacterDraft.AllowedSources).
/// </summary>
public record GetClassesQuery(string? Edition = null, IReadOnlyCollection<string>? Sources = null) : IQuery<Response<List<ClassReadModel>>>;
