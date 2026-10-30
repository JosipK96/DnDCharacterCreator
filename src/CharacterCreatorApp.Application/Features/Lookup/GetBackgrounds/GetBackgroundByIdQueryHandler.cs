using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetBackgrounds
{
    public class GetBackgroundByIdQueryHandler(IBackgroundRepository backgroundRepository, ILanguageRepository languageRepository) : IQueryHandler<GetBackgroundByIdQuery, Response<BackgroundReadModel>>
    {
        private readonly IBackgroundRepository _backgroundRepository = backgroundRepository;
        private readonly ILanguageRepository _languageRepository = languageRepository;

        public async Task<Response<BackgroundReadModel>> Handle(GetBackgroundByIdQuery query, CancellationToken cancellationToken)
        {
            var background = await _backgroundRepository.GetByIdAsync(query.Id, cancellationToken);

            if (background == null)
            {
                return BackgroundErrors.NotFoundById(query.Id);
            }

            await BackgroundLanguageNames.ApplyAsync([background], _languageRepository, cancellationToken);

            return background;
        }
    }
}
