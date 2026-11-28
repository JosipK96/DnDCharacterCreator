namespace CharacterCreatorApp.Contracts.Responses.Spells;

/// <summary>
/// How many cantrips and spells the draft's class lets it pick, up to which spell level; whether
/// the spell count is a 2014 prepared caster's, following its scores and level; and the domain or
/// oath spells it has prepared already; and the spells the step lists, by level then name: what its
/// lists offer from its books up to that level, which it picks from, and the always-prepared ones,
/// which it does not. CastsSpells is false for a character that casts nothing yet.
/// </summary>
public record SpellAllowanceApiResponse(
    bool CastsSpells,
    int Cantrips,
    int Spells,
    int MaxSpellLevel,
    bool SpellsFollowScores,
    List<string> AlwaysPreparedSpellIds,
    List<SpellApiResponse> SpellList);
