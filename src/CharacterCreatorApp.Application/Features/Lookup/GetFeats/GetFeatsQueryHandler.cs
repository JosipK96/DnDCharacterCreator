using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetFeats
{
    public class GetFeatsQueryHandler(IFeatRepository featRepository) : IQueryHandler<GetFeatsQuery, Response<List<FeatReadModel>>>
    {
        private readonly IFeatRepository _featRepository = featRepository;

        public async Task<Response<List<FeatReadModel>>> Handle(GetFeatsQuery query, CancellationToken cancellationToken)
        {
            var feats = await _featRepository.GetAllAsync(query.Edition, cancellationToken);

            return query.Sources is { Count: > 0 } sources ? feats.FindAll(x => sources.Contains(x.Source)) : feats;
        }
    }
}
