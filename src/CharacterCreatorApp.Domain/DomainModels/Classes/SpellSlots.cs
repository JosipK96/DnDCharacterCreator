namespace CharacterCreatorApp.Domain.DomainModels.Classes;

/// <summary>
/// The spell slots a character of one level has, from its class's or subclass's spell table:
/// how many of each spell level (index 0 is 1st level), and a warlock's Pact Magic apart, since
/// those slots are all of one level and come back on a short rest.
/// </summary>
public sealed record SpellSlots(IReadOnlyList<int> PerSpellLevel, PactMagicSlots? PactMagic)
{
    public static SpellSlots None { get; } = new([], null);
}

/// <summary>A warlock's Pact Magic: so many slots, all of one spell level.</summary>
public sealed record PactMagicSlots(int Slots, int SlotLevel);
