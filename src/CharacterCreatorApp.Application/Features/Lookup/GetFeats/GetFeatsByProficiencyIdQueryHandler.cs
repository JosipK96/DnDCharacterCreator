using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetFeats
{
    public class GetFeatsByProficiencyIdQueryHandler(IFeatRepository featRepository) : IQueryHandler<GetFeatsByProficiencyIdQuery, Response<List<FeatReadModel>>>
    {
        private readonly IFeatRepository _featRepository = featRepository;

        public async Task<Response<List<FeatReadModel>>> Handle(GetFeatsByProficiencyIdQuery query, CancellationToken cancellationToken)
        {
            var feats = await _featRepository.GetByProficiencyIdAsync(query.ProficiencyId, cancellationToken);

            if (feats == null)
            {
                return default(Response<List<FeatReadModel>>);
            }

            return feats;
        }
    }
}
