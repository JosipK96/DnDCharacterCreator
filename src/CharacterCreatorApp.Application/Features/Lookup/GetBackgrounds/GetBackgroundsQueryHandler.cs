using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetBackgrounds
{
    public class GetBackgroundsQueryHandler(IBackgroundRepository backgroundRepository, ILanguageRepository languageRepository) : IQueryHandler<GetBackgroundsQuery, Response<List<BackgroundReadModel>>>
    {
        private readonly IBackgroundRepository _backgroundRepository = backgroundRepository;
        private readonly ILanguageRepository _languageRepository = languageRepository;

        public async Task<Response<List<BackgroundReadModel>>> Handle(GetBackgroundsQuery query, CancellationToken cancellationToken)
        {
            var backgrounds = await _backgroundRepository.GetAllAsync(query.Edition, cancellationToken);

            var listed = query.Sources is { Count: > 0 } sources ? backgrounds.FindAll(x => sources.Contains(x.Source)) : backgrounds;
            await BackgroundLanguageNames.ApplyAsync(listed, _languageRepository, cancellationToken);

            return listed;
        }
    }
}
