using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Errors;

public static class SpellErrors
{
    public static Error Invalid() => Error.Validation(code: "Spell.Invalid", description: "One or more spells are not valid for this class/level");

    public static Error Duplicate(string spellName) =>
        Error.Validation("Spell.Duplicate", $"{spellName} was picked more than once.");

    public static Error NoSpellcasting(string className, int level) =>
        Error.Validation("Spell.NoSpellcasting", $"A level {level} {className} casts no spells, so it cannot pick any.");

    public static Error CantripCountMismatch(string className, int level, int required, int actual) =>
        Error.Validation("Spell.CantripCountMismatch", $"A level {level} {className} picks exactly {required} cantrip(s). The draft has {actual}.");

    public static Error SpellCountMismatch(string className, int level, int required, int actual) =>
        Error.Validation("Spell.SpellCountMismatch", $"A level {level} {className} picks exactly {required} spell(s) above cantrips. The draft has {actual}.");

    public static Error LevelTooHigh(string spellName, int spellLevel, string className, int level, int maxSpellLevel) =>
        Error.Validation(
            "Spell.LevelTooHigh",
            $"{spellName} is a level {spellLevel} spell; a level {level} {className} casts spells up to level {maxSpellLevel}.");

    public static Error SchoolNotAllowed(string className, int level, IReadOnlyList<string> schools, int anySchool, IReadOnlyList<string> outsideSpellNames) =>
        Error.Validation(
            "Spell.SchoolNotAllowed",
            $"A level {level} {className} picks at most {anySchool} spell(s) above cantrips from outside {string.Join(" and ", schools)}. The draft has {outsideSpellNames.Count}: {string.Join(", ", outsideSpellNames)}.");
}
