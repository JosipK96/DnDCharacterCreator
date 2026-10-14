using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetBackgrounds
{
    public class GetBackgroundsQueryHandler(IBackgroundRepository backgroundRepository) : IQueryHandler<GetBackgroundsQuery, Response<List<BackgroundReadModel>>>
    {
        private readonly IBackgroundRepository _backgroundRepository = backgroundRepository;

        public async Task<Response<List<BackgroundReadModel>>> Handle(GetBackgroundsQuery query, CancellationToken cancellationToken)
        {
            return await _backgroundRepository.GetAllAsync(query.Edition, cancellationToken);
        }
    }
}
