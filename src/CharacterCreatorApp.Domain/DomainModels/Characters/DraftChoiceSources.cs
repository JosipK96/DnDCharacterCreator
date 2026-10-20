namespace CharacterCreatorApp.Domain.DomainModels.Characters
{
    /// <summary>
    /// The book each choice a draft holds comes from, by book code ("PHB", "XGE"), so
    /// CharacterDraft.AllowBooks can clear the ones from a book no longer allowed, and
    /// ClearMissingChoices the ones the reference data no longer has. Null where nothing is chosen
    /// or the entity is gone; Feats covers the background feat, the improvement feats and the
    /// Fighting Styles, by feat id.
    /// </summary>
    public sealed record DraftChoiceSources(
        string? Race,
        string? Subrace,
        string? Class,
        string? Subclass,
        string? Background,
        IReadOnlyDictionary<string, string> Feats,
        IReadOnlyDictionary<string, string> Spells);
}
