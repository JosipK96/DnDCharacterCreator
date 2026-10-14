using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Feats;
using CharacterCreatorApp.Domain.Services;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Building;

/// <summary>
/// The Fighting Styles a character picks for its class's Fighting Style features: as many as
/// CharacterFeatures.FightingStylesOwed says, each a Fighting Style feat of the character's
/// edition that its class may take. The style step and finalize both check through here.
/// </summary>
public static class FightingStyleChoice
{
    /// <summary>
    /// The styles the class offers: the Fighting Style feats of the edition and the allowed books
    /// whose prerequisites it meets.
    /// </summary>
    public static List<FeatReadModel> Choosable(
        IEnumerable<FeatReadModel> feats,
        CharacterDraft draft,
        Class @class,
        Subclass? subclass,
        int level) =>
        feats
            .Where(f => f.Category == FeatCategory.FightingStyle && f.Edition == draft.Edition && draft.AllowsSource(f.Source))
            .Where(f => FeatPrerequisiteCheck.FindUnmet([f], 0, [], draft, null, null, @class, subclass, level) is null)
            .OrderBy(f => f.Name, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Why the picked styles are not the ones the character is owed, or null when they are.
    /// otherFeatIds are the feats taken for improvements, which may not repeat a style.
    /// </summary>
    public static Error? FindInvalidPick(
        IReadOnlyList<string> pickedIds,
        IReadOnlyDictionary<string, FeatReadModel> featsById,
        IEnumerable<string> otherFeatIds,
        CharacterDraft draft,
        Class @class,
        Subclass? subclass,
        int level)
    {
        var owed = CharacterFeatures.FightingStylesOwed(@class, subclass, level);

        if (pickedIds.Count != owed)
        {
            return CharacterDraftErrors.FightingStyleCount(@class.Name, level, owed, pickedIds.Count);
        }

        var unknown = pickedIds.Where(id => !featsById.ContainsKey(id)).Distinct().ToList();

        if (unknown.Count != 0)
        {
            return CharacterDraftErrors.InvalidFeatIds(unknown);
        }

        var picked = pickedIds.Select(id => featsById[id]).ToList();

        if (picked.Find(f => f.Category != FeatCategory.FightingStyle) is { } notAStyle)
        {
            return CharacterDraftErrors.NotAFightingStyle(notAStyle.Name);
        }

        if (picked.Find(f => f.Edition != draft.Edition) is { } otherEdition)
        {
            return CharacterDraftErrors.EditionMismatch("Fighting Style", otherEdition.Name, otherEdition.Edition, draft.Edition);
        }

        if (picked.Find(f => !draft.AllowsSource(f.Source)) is { } notAllowed)
        {
            return CharacterDraftErrors.SourceNotAllowed("Fighting Style", notAllowed.Name, notAllowed.Source);
        }

        // No style is repeatable, and a 2024 one taken for an improvement counts too
        var repeated = pickedIds
            .Concat(otherFeatIds.Where(featsById.ContainsKey))
            .GroupBy(id => id)
            .Where(g => g.Count() > 1 && !featsById[g.Key].IsRepeatable)
            .Select(g => featsById[g.Key].Name)
            .ToList();

        if (repeated.Count != 0)
        {
            return CharacterDraftErrors.DuplicateFeats(repeated);
        }

        // A style asks only for a class and its Fighting Style feature, never the race or scores
        return FeatPrerequisiteCheck.FindUnmet(picked, 0, [], draft, null, null, @class, subclass, level);
    }
}
