using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Races;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Shared.Loaders;

/// <summary>
/// Loads the race, class and background a draft has selected so far, for the wizard's step
/// handlers. Each comes two ways:
/// <list type="bullet">
/// <item><c>Load…Async</c> when the step cannot run without it: the entity, or the error saying it
/// was not chosen yet (<c>CharacterDraftErrors.RaceRequired</c>) or cannot be found (<c>RaceErrors.NotFoundById</c>).</item>
/// <item><c>Load…IfChosenAsync</c> when the step only takes it into account: the entity, or null
/// while the player has not reached that step.</item>
/// </list>
/// <c>step</c> says what the player is doing, for the error: "selecting languages".
/// Finalize and preview need everything at once and use <see cref="DraftSourceLoader"/> instead.
/// </summary>
public sealed class DraftSelectionsLoader(
    IRaceRepository raceRepository,
    IClassRepository classRepository,
    IBackgroundRepository backgroundRepository)
{
    private readonly IRaceRepository _raceRepository = raceRepository;
    private readonly IClassRepository _classRepository = classRepository;
    private readonly IBackgroundRepository _backgroundRepository = backgroundRepository;

    public async Task<Response<Race>> LoadRaceAsync(CharacterDraft draft, string step, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(draft.RaceId))
        {
            return CharacterDraftErrors.RaceRequired(step);
        }

        var race = await _raceRepository.GetByIdWithDetailsAsync(draft.RaceId, cancellationToken);

        if (race == null)
        {
            return RaceErrors.NotFoundById(draft.RaceId);
        }

        return race;
    }

    public async Task<Response<Class>> LoadClassAsync(CharacterDraft draft, string step, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(draft.ClassId))
        {
            return CharacterDraftErrors.ClassRequired(step);
        }

        var @class = await _classRepository.GetByIdWithDetailsAsync(draft.ClassId, cancellationToken);

        if (@class == null)
        {
            return ClassErrors.NotFoundById(draft.ClassId);
        }

        return @class;
    }

    public async Task<Response<Background>> LoadBackgroundAsync(CharacterDraft draft, string step, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(draft.BackgroundId))
        {
            return CharacterDraftErrors.BackgroundRequired(step);
        }

        var background = await _backgroundRepository.GetByIdWithDetailsAsync(draft.BackgroundId, cancellationToken);

        if (background == null)
        {
            return BackgroundErrors.NotFoundById(draft.BackgroundId);
        }

        return background;
    }

    /// <summary>For a step that needs both: says what is not chosen yet before loading either.</summary>
    public async Task<Response<(Race Race, Background Background)>> LoadRaceAndBackgroundAsync(
        CharacterDraft draft, string step, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(draft.RaceId))
        {
            return CharacterDraftErrors.RaceRequired(step);
        }

        if (string.IsNullOrEmpty(draft.BackgroundId))
        {
            return CharacterDraftErrors.BackgroundRequired(step);
        }

        var race = await LoadRaceAsync(draft, step, cancellationToken);

        if (!race.IsSuccess)
        {
            return race.Errors;
        }

        var background = await LoadBackgroundAsync(draft, step, cancellationToken);

        if (!background.IsSuccess)
        {
            return background.Errors;
        }

        return (race.Value, background.Value);
    }

    /// <summary>For a step that needs all three (the tools): says what is not chosen yet before loading any.</summary>
    public async Task<Response<(Race Race, Background Background, Class Class)>> LoadRaceBackgroundAndClassAsync(
        CharacterDraft draft, string step, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(draft.RaceId))
        {
            return CharacterDraftErrors.RaceRequired(step);
        }

        if (string.IsNullOrEmpty(draft.BackgroundId))
        {
            return CharacterDraftErrors.BackgroundRequired(step);
        }

        if (string.IsNullOrEmpty(draft.ClassId))
        {
            return CharacterDraftErrors.ClassRequired(step);
        }

        var raceAndBackground = await LoadRaceAndBackgroundAsync(draft, step, cancellationToken);

        if (!raceAndBackground.IsSuccess)
        {
            return raceAndBackground.Errors;
        }

        var @class = await LoadClassAsync(draft, step, cancellationToken);

        if (!@class.IsSuccess)
        {
            return @class.Errors;
        }

        return (raceAndBackground.Value.Race, raceAndBackground.Value.Background, @class.Value);
    }

    public async Task<Race?> LoadRaceIfChosenAsync(CharacterDraft draft, CancellationToken cancellationToken) =>
        string.IsNullOrEmpty(draft.RaceId) ? null : await _raceRepository.GetByIdWithDetailsAsync(draft.RaceId, cancellationToken);

    public async Task<Class?> LoadClassIfChosenAsync(CharacterDraft draft, CancellationToken cancellationToken) =>
        string.IsNullOrEmpty(draft.ClassId) ? null : await _classRepository.GetByIdWithDetailsAsync(draft.ClassId, cancellationToken);

    public async Task<Background?> LoadBackgroundIfChosenAsync(CharacterDraft draft, CancellationToken cancellationToken) =>
        string.IsNullOrEmpty(draft.BackgroundId) ? null : await _backgroundRepository.GetByIdWithDetailsAsync(draft.BackgroundId, cancellationToken);
}
