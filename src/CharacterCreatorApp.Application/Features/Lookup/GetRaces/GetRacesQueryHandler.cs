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
            var races = await _raceRepository.GetAllAsync(query.Edition, cancellationToken);

            if (query.Sources is not { Count: > 0 } sources)
            {
                return races;
            }

            // A subrace can come from another book than its race
            races.ForEach(r => r.Subraces.RemoveAll(s => !sources.Contains(s.Source)));

            return races.FindAll(r => sources.Contains(r.Source));
        }
    }
}
