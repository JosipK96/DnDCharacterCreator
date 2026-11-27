namespace CharacterCreatorApp.Domain.Services;

/// <summary>
/// Spells a character casts with one ability, and what gives them: the class ("Wizard"), a third
/// caster's subclass ("Eldritch Knight"), a race ("High Elf") or a feat ("Magic Initiate"). The
/// ability is in any spelling the data has ("int", "Intelligence"); the calculator normalizes it.
/// </summary>
public sealed record SpellcastingSource(string Label, string AbilityId);
