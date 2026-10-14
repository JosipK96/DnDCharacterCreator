using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Infrastructure.Seed.Rows;

using Microsoft.Extensions.Logging;

namespace CharacterCreatorApp.Infrastructure.Seed.Seeders
{
    public class RaceSeeder(ILogger<RaceSeeder> logger) : IEntitySeeder
    {
        public int Priority => 2;

        public async Task SeedAsync(ApplicationDbContext context, string dataPath, CancellationToken cancellationToken)
        {
            // Both editions, each race with its subraces and what they give inside it
            var rows = await SeedFile.ReadListAsync<RaceSeedRow>(Path.Combine(dataPath, "Races.json"), cancellationToken);

            await context.Races.AddRangeAsync(rows.Select(ToRace), cancellationToken);

            logger.LogInformation("Seeded {Count} races with {Subraces} subraces: {Editions}.", rows.Count, rows.Sum(r => r.Subraces.Count),
                string.Join(", ", rows.CountBy(r => r.Edition).Select(kvp => $"{kvp.Value} for {kvp.Key}")));
        }

        private static Race ToRace(RaceSeedRow row) => new()
        {
            Id = row.Id,
            Name = row.Name,
            Edition = row.Edition,
            Source = row.Source,
            Size = row.Size,
            Speed = row.Speed,
            SkillChoiceCount = row.SkillChoiceCount,
            SkillChoiceOptions = row.SkillChoiceOptions,
            LanguageChoiceCount = row.LanguageChoiceCount,
            AbilityChoiceCount = row.AbilityChoiceCount,
            AbilityChoiceOptions = row.AbilityChoiceOptions,
            FeatChoiceCount = row.FeatChoiceCount,
            FeatChoiceCategory = row.FeatChoiceCategory,
            RaceLanguages = row.Languages.ConvertAll(language => new RaceLanguage { RaceId = row.Id, Language = language }),
            AbilityBonuses = row.AbilityBonuses.ConvertAll(b => new RaceAbilityBonus { RaceId = row.Id, AbilityScoreId = b.AbilityScoreId, Bonus = b.Bonus }),
            RaceProficiencies = row.ProficiencyIds.ConvertAll(id => new RaceProficiency { RaceId = row.Id, ProficiencyId = id }),
            Traits = row.Traits.ConvertAll(t => new RaceTrait { Id = t.Id, RaceId = row.Id, Name = t.Name, Description = t.Description }),
            Spells = row.Spells.ConvertAll(s => new RaceSpell { RaceId = row.Id, SpellId = s.SpellId, Level = s.Level }),
            Subraces = row.Subraces.ConvertAll(subrace => ToSubrace(row.Id, subrace)),
        };

        private static Subrace ToSubrace(string raceId, SubraceSeedRow row) => new()
        {
            Id = row.Id,
            Name = row.Name,
            RaceId = raceId,
            Source = row.Source,
            Speed = row.Speed,
            LanguageChoiceCount = row.LanguageChoiceCount,
            AbilityChoiceCount = row.AbilityChoiceCount,
            AbilityChoiceOptions = row.AbilityChoiceOptions,
            SkillChoiceCount = row.SkillChoiceCount,
            SkillChoiceOptions = row.SkillChoiceOptions,
            FeatChoiceCount = row.FeatChoiceCount,
            FeatChoiceCategory = row.FeatChoiceCategory,
            AbilityBonuses = row.AbilityBonuses.ConvertAll(b => new SubraceAbilityBonus { SubraceId = row.Id, AbilityScoreId = b.AbilityScoreId, Bonus = b.Bonus }),
            SubraceProficiencies = row.ProficiencyIds.ConvertAll(id => new SubraceProficiency { SubraceId = row.Id, ProficiencyId = id }),
            Traits = row.Traits.ConvertAll(t => new SubraceTrait { Id = t.Id, SubraceId = row.Id, Name = t.Name, Description = t.Description }),
            Spells = row.Spells.ConvertAll(s => new SubraceSpell { SubraceId = row.Id, SpellId = s.SpellId, Level = s.Level }),
        };
    }
}
