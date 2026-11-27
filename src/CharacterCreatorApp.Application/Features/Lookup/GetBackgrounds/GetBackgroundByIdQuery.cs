using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels.Backgrounds;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetBackgrounds;

public record GetBackgroundByIdQuery(string Id) : IQuery<Response<BackgroundReadModel>>;