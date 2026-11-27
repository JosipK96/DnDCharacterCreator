using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels.Classes;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetClasses;

public record GetClassByIdQuery(string Id) : IQuery<Response<ClassReadModel>>;