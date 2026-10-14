using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Filtering;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.GetMyCharacters;

public record GetMyCharactersQuery(FilterOptions<FilterCharacterSearchParams> Filter) : IQuery<Response<PagedResult<CharacterReadModel>>>;
