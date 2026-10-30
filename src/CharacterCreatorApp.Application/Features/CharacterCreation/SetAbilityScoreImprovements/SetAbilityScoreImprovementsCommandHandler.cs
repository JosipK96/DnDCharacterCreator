using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.CharacterCreation.Building;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Feats;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.Services;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetAbilityScoreImprovements;

public class SetAbilityScoreImprovementsCommandHandler(
    ICharacterDraftRepository characterDraftRepository,
    IClassRepository classRepository,
    IRaceRepository raceRepository,
    IBackgroundRepository backgroundRepository,
    IFeatRepository featRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext) : ICommandHandler<SetAbilityScoreImprovementsCommand, Response>
{
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly IClassRepository _classRepository = classRepository;
    private readonly IRaceRepository _raceRepository = raceRepository;
    private readonly IBackgroundRepository _backgroundRepository = backgroundRepository;
    private readonly IFeatRepository _featRepository = featRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IUserContext _userContext = userContext;

    public async Task<Response> Handle(SetAbilityScoreImprovementsCommand command, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetByIdForUpdateAsync(command.DraftId, _userContext.UserId, cancellationToken);

        if (draft == null)
        {
            return CharacterDraftErrors.NotFoundById(command.DraftId);
        }

        if (draft.Level == null || string.IsNullOrEmpty(draft.ClassId))
        {
            return CharacterDraftErrors.ClassAndLevelRequired();
        }

        // The class with its features: a Fighting Style feat asks for the feature of that name
        var @class = await _classRepository.GetByIdWithDetailsAsync(draft.ClassId, cancellationToken);

        if (@class == null)
        {
            return ClassErrors.NotFoundById(draft.ClassId);
        }

        // The race with its subraces: a variant human's feat comes with the subrace
        var race = string.IsNullOrEmpty(draft.RaceId) ? null : await _raceRepository.GetByIdWithDetailsAsync(draft.RaceId, cancellationToken);
        var subrace = race?.Subraces.FirstOrDefault(s => s.Id == draft.SubraceId);
        var raceName = subrace?.Name ?? race?.Name ?? string.Empty;
        var bonusFeats = race?.BonusFeatsWith(subrace) ?? new BonusFeatChoice(0, null);

        if (command.FeatIds.Count < bonusFeats.Count)
        {
            return CharacterDraftErrors.BonusFeatsMissing(raceName, bonusFeats.Count, command.FeatIds.Count);
        }

        var level = draft.Level.Value;
        var expectedAsiCount = @class.ImprovementsAt(level);
        var chosenCount = command.Improvements.Count + command.FeatIds.Count - bonusFeats.Count;

        if (chosenCount != expectedAsiCount)
        {
            return CharacterDraftErrors.InvalidAsiCount(level, draft.ClassId, expectedAsiCount, chosenCount);
        }

        var improvementError = ValidateImprovements(command.Improvements);

        if (improvementError is not null)
        {
            return improvementError.Value;
        }

        // The background's feat (a Noble's Skilled) may ask for picks too, made in this step
        var background = draft.BackgroundId is { } backgroundId
            ? await _backgroundRepository.GetByIdWithDetailsAsync(backgroundId, cancellationToken)
            : null;
        var backgroundFeatIds = background == null ? [] : CharacterBuilder.BackgroundFeatIds(background, draft).ToList();
        var featIdsToLoad = command.FeatIds.Concat(backgroundFeatIds).Distinct().ToList();
        var feats = featIdsToLoad.Count == 0 ? [] : await _featRepository.GetByIdsAsync(featIdsToLoad, cancellationToken);
        var featsById = feats.ToDictionary(f => f.Id);

        // The prerequisites are only looked up once ValidateFeats has found every id
        var featError = ValidateFeats(command.FeatIds, backgroundFeatIds, featsById, draft)
            ?? FeatPrerequisiteCheck.FindUnmet(
                command.FeatIds.ConvertAll(id => featsById[id]),
                bonusFeats.Count,
                command.Improvements,
                draft,
                race,
                subrace,
                @class,
                @class.Subclasses.FirstOrDefault(s => s.Id == draft.SubclassId),
                level);

        if (featError is not null)
        {
            return featError.Value;
        }

        var notOfCategory = command.FeatIds.Take(bonusFeats.Count).Select(id => featsById[id]).FirstOrDefault(f => !bonusFeats.Accepts(f.Category));

        if (notOfCategory is not null)
        {
            return CharacterDraftErrors.BonusFeatNotOfCategory(raceName, notOfCategory.Name, bonusFeats.Category!);
        }

        var featIncreases = ResolveFeatAbilityIncreases(command.FeatIds, command.FeatAbilityIncreases, featsById);

        if (!featIncreases.IsSuccess)
        {
            return featIncreases.Errors;
        }

        var proficiencyPicks = command.FeatProficiencyPicks ?? [];
        var proficiencyError = FeatProficiencyPicks.FindInvalidPick(
                proficiencyPicks, FeatProficiencyPicks.SlotsFor(backgroundFeatIds, command.FeatIds, featsById))
            ?? ProficiencyPickCheck.FindRepeated(draft, race, subrace, background, @class, featPicks: proficiencyPicks);

        if (proficiencyError is not null)
        {
            return proficiencyError.Value;
        }

        draft.SetAbilityScoreImprovements(command.Improvements);
        draft.SetFeats(command.FeatIds, featIncreases.Value, proficiencyPicks);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Response.Success();
    }

    private static Error? ValidateImprovements(List<Dictionary<string, int>> improvements)
    {
        foreach (var improvement in improvements)
        {
            // The rule is AbilityIncreaseChoice.Improvement's; what follows only says which part was broken
            if (AbilityIncreaseChoice.Improvement.Allows(improvement))
            {
                continue;
            }

            if (improvement.Count == 0)
            {
                return CharacterDraftErrors.EmptyAsi();
            }

            var totalPoints = improvement.Values.Sum();

            if (totalPoints != 2)
            {
                return CharacterDraftErrors.InvalidAsiPoints(totalPoints);
            }

            var invalidAbilities = improvement.Keys.Where(k => !AbilityScore.TryNormalize(k, out _)).ToList();

            if (invalidAbilities.Count != 0)
            {
                return CharacterDraftErrors.InvalidAsiAbility(invalidAbilities);
            }

            if (improvement.Values.Any(v => v < 1 || v > 2))
            {
                return CharacterDraftErrors.InvalidAsiValue();
            }

            // The same ability twice under two spellings ("str" and "Strength")
            return CharacterDraftErrors.InvalidAsiValue();
        }

        return null;
    }

    /// <summary>
    /// What each feat raises: the pick where it offers several abilities, its own where it offers
    /// one, nothing where it offers none. A pick the feat does not allow is refused.
    /// </summary>
    private static Response<List<Dictionary<string, int>>> ResolveFeatAbilityIncreases(
        List<string> featIds,
        List<Dictionary<string, int>>? picks,
        Dictionary<string, FeatReadModel> featsById)
    {
        var resolved = new List<Dictionary<string, int>>();

        for (var i = 0; i < featIds.Count; i++)
        {
            var feat = featsById[featIds[i]];
            var picked = picks?[i] ?? [];
            var increase = feat.AbilityIncrease is { } choice
                ? choice.Resolve(picked)
                : picked.Values.All(amount => amount == 0) ? [] : null;

            if (increase == null)
            {
                return CharacterDraftErrors.InvalidFeatAbilityIncrease(
                    feat.Name,
                    feat.AbilityIncrease?.Points ?? 0,
                    feat.AbilityIncrease?.Options.ConvertAll(id => AbilityScore.FromId(id).Name) ?? []);
            }

            resolved.Add(increase);
        }

        return resolved;
    }

    private static Error? ValidateFeats(
        List<string> featIds,
        List<string> backgroundFeatIds,
        Dictionary<string, FeatReadModel> featsById,
        CharacterDraft draft)
    {
        if (featIds.Count == 0)
        {
            return null;
        }

        var unknown = featIds.Where(id => !featsById.ContainsKey(id)).Distinct().ToList();

        if (unknown.Count != 0)
        {
            return CharacterDraftErrors.InvalidFeatIds(unknown);
        }

        // A 2014 Fighting Style comes with the class alone; no improvement buys one
        var classOnly = featIds.Select(id => featsById[id]).FirstOrDefault(f => !FeatCategory.CanBeTakenForImprovement(f.Edition, f.Category));

        if (classOnly is not null)
        {
            return CharacterDraftErrors.FeatNotForImprovement(classOnly.Name);
        }

        // The background's feat counts too: a 2024 criminal has Alert already. The Fighting
        // Styles are saved after this step's feats, and their own step checks them against these.
        var repeatedNonRepeatable = featIds
            .Concat(backgroundFeatIds.Where(featsById.ContainsKey))
            .GroupBy(id => id)
            .Where(g => g.Count() > 1 && !featsById[g.Key].IsRepeatable)
            .Select(g => featsById[g.Key].Name)
            .ToList();

        if (repeatedNonRepeatable.Count != 0)
        {
            return CharacterDraftErrors.DuplicateFeats(repeatedNonRepeatable);
        }

        var otherEdition = featIds.Select(id => featsById[id]).FirstOrDefault(f => f.Edition != draft.Edition);

        if (otherEdition is not null)
        {
            return CharacterDraftErrors.EditionMismatch("feat", otherEdition.Name, otherEdition.Edition, draft.Edition);
        }

        var notAllowed = featIds.Select(id => featsById[id]).FirstOrDefault(f => !draft.AllowsSource(f.Source));

        return notAllowed is null ? null : CharacterDraftErrors.SourceNotAllowed("feat", notAllowed.Name, notAllowed.Source);
    }
}
