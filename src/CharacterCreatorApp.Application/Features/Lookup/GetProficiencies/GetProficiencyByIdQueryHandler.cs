using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetProficiencies
{
    public class GetProficiencyByIdQueryHandler(IProficiencyRepository proficiencyRepository)
        : IQueryHandler<GetProficiencyByIdQuery, Response<ProficiencyReadModel>>
    {
        private readonly IProficiencyRepository _proficiencyRepository = proficiencyRepository;

        public async Task<Response<ProficiencyReadModel>> Handle(GetProficiencyByIdQuery request, CancellationToken cancellationToken)
        {
            var proficiency = await _proficiencyRepository.GetByIdAsync(request.Id, cancellationToken);

            if (proficiency == null)
            {
                return ProficiencyErrors.NotFoundById(request.Id);
            }

            return new ProficiencyReadModel() { Id = proficiency.Id, Name = proficiency.Name, Type = proficiency.Type };
        }
    }
}
