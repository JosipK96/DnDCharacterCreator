using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Characters;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Building;

/// <summary>
/// The feats a character took, as the sheet and the review step both list them, so the two
/// cannot disagree.
/// </summary>
public static class CharacterFeatEntries
{
    /// <summary>
    /// One entry per feat taken, in the order taken, so a repeatable feat taken twice is listed
    /// twice: the repository returns each feat once. The nth time a feat appears gets the nth
    /// spell pick made for it, the order CharacterDraft.FeatSpells keeps them in.
    /// </summary>
    public static List<CharacterFeatReadModel> Build(
        IEnumerable<string> featIds,
        IEnumerable<FeatSpellPick> featSpells,
        IEnumerable<FeatReadModel> feats,
        IEnumerable<SpellReadModel> spells)
    {
        var featsById = feats.ToDictionary(f => f.Id);
        var spellsById = spells.ToDictionary(s => s.Id);
        var picksByFeat = featSpells
            .GroupBy(pick => pick.FeatId)
            .ToDictionary(group => group.Key, group => new Queue<FeatSpellPick>(group));
        var entries = new List<CharacterFeatReadModel>();

        foreach (var featId in featIds)
        {
            // A feat a reload of the seed data dropped has nothing to show
            if (!featsById.TryGetValue(featId, out var feat))
            {
                continue;
            }

            List<string> spellIds = picksByFeat.TryGetValue(featId, out var picks) && picks.TryDequeue(out var pick) ? pick.SpellIds : [];
            entries.Add(new CharacterFeatReadModel
            {
                Feat = feat,
                Spells = [.. spellIds.Where(spellsById.ContainsKey).Select(id => spellsById[id])],
            });
        }

        return entries;
    }
}
