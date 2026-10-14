using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetFeats
{
    public class GetFeatByIdQueryHandler(IFeatRepository featRepository) : IQueryHandler<GetFeatByIdQuery, Response<FeatReadModel>>
    {
        private readonly IFeatRepository _featRepository = featRepository;

        public async Task<Response<FeatReadModel>> Handle(GetFeatByIdQuery query, CancellationToken cancellationToken)
        {
            var feat = await _featRepository.GetByIdAsync(query.Id, cancellationToken);

            if (feat == null)
            {
                return FeatErrors.NotFoundById(query.Id);
            }

            return feat;
        }
    }
}
