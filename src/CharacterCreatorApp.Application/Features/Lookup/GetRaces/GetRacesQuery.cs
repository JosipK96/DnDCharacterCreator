using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetRaces;

/// <summary>The races of one rules edition, or of both when Edition is null.</summary>
public record GetRacesQuery(string? Edition = null) : IQuery<Response<List<RaceReadModel>>>;
