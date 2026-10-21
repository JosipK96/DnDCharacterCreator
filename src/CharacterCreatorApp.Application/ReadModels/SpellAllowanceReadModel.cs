namespace CharacterCreatorApp.Application.ReadModels;

/// <summary>
/// What the draft's character picks in the spells step for its class, worked out as ChooseSpells
/// and finalize check it. CastsSpells is false, and the rest 0, for a character that casts
/// nothing yet.
/// </summary>
public class SpellAllowanceReadModel
{
    public bool CastsSpells { get; set; }
    public int Cantrips { get; set; }
    public int Spells { get; set; }
    public int MaxSpellLevel { get; set; }

    /// <summary>Whether Spells is a 2014 prepared caster's count, its modifier plus its level or half of it.</summary>
    public bool SpellsFollowScores { get; set; }

    /// <summary>The domain or oath spells the subclass has prepared already, which are not picked.</summary>
    public List<string> AlwaysPreparedSpellIds { get; set; } = [];
}
