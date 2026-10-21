namespace CharacterCreatorApp.Contracts.Responses;

/// <summary>
/// How many cantrips and spells the draft's class lets it pick, up to which spell level; whether
/// the spell count is a 2014 prepared caster's, following its scores and level; and the domain or
/// oath spells it has prepared already. CastsSpells is false for a character that casts nothing yet.
/// </summary>
public record SpellAllowanceApiResponse(
    bool CastsSpells,
    int Cantrips,
    int Spells,
    int MaxSpellLevel,
    bool SpellsFollowScores,
    List<string> AlwaysPreparedSpellIds);
