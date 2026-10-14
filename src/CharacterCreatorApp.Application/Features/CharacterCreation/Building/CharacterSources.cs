using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Races;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Building;

/// <summary>
/// Everything needed to turn a draft into a character: the entities the draft names only by id,
/// the background's feat among the feats, and the languages of the draft's edition, which the
/// names the race, background and feats give resolve against. The spells the character may
/// pick are loaded only for one who casts at the draft's level. BonusSpellLists holds, by class
/// id, every list a race or feat lets the player pick spells from (see BonusSpells).
/// </summary>
public sealed record CharacterSources(
    Race Race,
    Subrace? Subrace,
    Class Class,
    Subclass? Subclass,
    Background Background,
    IReadOnlyList<FeatReadModel> Feats,
    IReadOnlyList<LanguageReadModel> Languages,
    IReadOnlyList<SpellReadModel> ChoosableSpells,
    IReadOnlyDictionary<string, List<SpellReadModel>> BonusSpellLists);
