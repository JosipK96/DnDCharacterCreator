using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetRaces
{
    public class GetRacesQueryHandler(IRaceRepository raceRepository) : IQueryHandler<GetRacesQuery, Response<List<RaceReadModel>>>
    {
        private readonly IRaceRepository _raceRepository = raceRepository;

        public async Task<Response<List<RaceReadModel>>> Handle(GetRacesQuery query, CancellationToken cancellationToken)
        {
            return await _raceRepository.GetAllAsync(query.Edition, cancellationToken);
        }
    }
}
