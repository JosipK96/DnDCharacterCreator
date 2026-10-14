using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetFeats;

/// <summary>The feats of one rules edition, or of both when Edition is null.</summary>
public record GetFeatsQuery(string? Edition = null) : IQuery<Response<List<FeatReadModel>>>;
