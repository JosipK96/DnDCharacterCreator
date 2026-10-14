namespace CharacterCreatorApp.Domain.DomainModels.Classes;

/// <summary>
/// The schools a 2014 Eldritch Knight's or Arcane Trickster's levelled spells must come from,
/// and how many of them may come from any school: one gained at each of levels 3, 8, 14 and 20.
/// Cantrips are not limited.
/// </summary>
public sealed record SpellSchoolLimit(IReadOnlyList<string> Schools, int AnySchoolSpells)
{
    public bool Allows(string school) => Schools.Contains(school, StringComparer.OrdinalIgnoreCase);
}
