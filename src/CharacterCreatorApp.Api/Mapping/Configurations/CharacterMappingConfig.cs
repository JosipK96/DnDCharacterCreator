using CharacterCreatorApp.Application.Common.Filtering;
using CharacterCreatorApp.Application.Features.CharacterManagement.GetMyCharacters;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Contracts.Common;
using CharacterCreatorApp.Contracts.Requests.Character;
using CharacterCreatorApp.Contracts.Responses;
using CharacterCreatorApp.Domain.Services;

using Mapster;

namespace CharacterCreatorApp.Api.Mapping.Configurations;

public class CharacterMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<FilterRequest<GetCharactersApiRequest>, FilterOptions<FilterCharacterSearchParams>>()
            .MapToConstructor(true);

        config.NewConfig<PagedResult<CharacterReadModel>, FilterResponse<CharacterSummaryApiResponse>>()
            .MapToConstructor(true);

        config.NewConfig<ProficiencyReadModel, ProficiencyApiResponse>()
            .MapToConstructor(true);

        config.NewConfig<FeatureReadModel, FeatureApiResponse>()
            .MapToConstructor(true);

        config.NewConfig<SpellReadModel, SpellApiResponse>()
            .MapToConstructor(true);

        config.NewConfig<AbilityStat, AbilityStatApiResponse>()
            .MapToConstructor(true);

        config.NewConfig<SavingThrowStat, SavingThrowStatApiResponse>()
            .MapToConstructor(true);

        config.NewConfig<SkillStat, SkillStatApiResponse>()
            .MapToConstructor(true);

        config.NewConfig<CharacterStats, CharacterStatsApiResponse>()
            .MapToConstructor(true);

        config.NewConfig<CharacterPreviewReadModel, CharacterPreviewApiResponse>()
            .MapToConstructor(true);

        config.NewConfig<CharacterFeatReadModel, CharacterFeatApiResponse>()
            .MapToConstructor(true);

        config.NewConfig<CharacterDetailsReadModel, CharacterDetailsApiResponse>()
            .MapToConstructor(true);
    }
}