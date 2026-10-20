using CharacterCreatorApp.Domain.DomainModels.Characters;

using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.Seed
{
    /// <summary>
    /// The book of every race, subrace, class, subclass, background, feat and spell, by id, as the
    /// reference tables hold them now. SeedService reads it after a replace to find what saved
    /// characters and drafts name that the new data no longer has.
    /// </summary>
    internal sealed record ReferenceSources(
        Dictionary<string, string> Races,
        Dictionary<string, string> Subraces,
        Dictionary<string, string> Classes,
        Dictionary<string, string> Subclasses,
        Dictionary<string, string> Backgrounds,
        Dictionary<string, string> Feats,
        Dictionary<string, string> Spells)
    {
        public static async Task<ReferenceSources> LoadAsync(ApplicationDbContext context, CancellationToken cancellationToken) => new(
            await context.Races.ToDictionaryAsync(r => r.Id, r => r.Source, cancellationToken),
            await context.Subraces.ToDictionaryAsync(s => s.Id, s => s.Source, cancellationToken),
            await context.Classes.ToDictionaryAsync(c => c.Id, c => c.Source, cancellationToken),
            await context.Subclasses.ToDictionaryAsync(s => s.Id, s => s.Source, cancellationToken),
            await context.Backgrounds.ToDictionaryAsync(b => b.Id, b => b.Source, cancellationToken),
            await context.Feats.ToDictionaryAsync(f => f.Id, f => f.Source, cancellationToken),
            await context.Spells.ToDictionaryAsync(s => s.Id, s => s.Source, cancellationToken));

        /// <summary>The book of each choice the draft holds; null, or left out, for one that is gone.</summary>
        public DraftChoiceSources For(CharacterDraft draft) => new(
            SourceOf(Races, draft.RaceId),
            SourceOf(Subraces, draft.SubraceId),
            SourceOf(Classes, draft.ClassId),
            SourceOf(Subclasses, draft.SubclassId),
            SourceOf(Backgrounds, draft.BackgroundId),
            Feats,
            Spells);

        private static string? SourceOf(Dictionary<string, string> sources, string? id) =>
            id != null && sources.TryGetValue(id, out var source) ? source : null;
    }
}
