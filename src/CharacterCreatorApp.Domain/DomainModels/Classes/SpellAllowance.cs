namespace CharacterCreatorApp.Domain.DomainModels.Classes;

/// <summary>
/// What a character of one level picks from its spell table: so many cantrips, so many spells
/// of at most a spell level, and for a subclass that limits them, the schools those spells come
/// from. No spells means a prepared caster that picks only its cantrips, as a 2014 cleric does.
/// </summary>
public sealed record SpellAllowance(int Cantrips, int Spells, int MaxSpellLevel, SpellSchoolLimit? SchoolLimit = null);
