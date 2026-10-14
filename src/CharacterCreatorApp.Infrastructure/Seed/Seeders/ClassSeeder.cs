using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Proficiencies;
using CharacterCreatorApp.Infrastructure.Seed.Rows;

using Microsoft.Extensions.Logging;

namespace CharacterCreatorApp.Infrastructure.Seed.Seeders
{
    public class ClassSeeder(ILogger<ClassSeeder> logger) : IEntitySeeder
    {
        public int Priority => 3;

        public async Task SeedAsync(ApplicationDbContext context, string dataPath, CancellationToken cancellationToken)
        {
            var rows = await SeedFile.ReadListAsync<ClassSeedRow>(Path.Combine(dataPath, "Classes.json"), cancellationToken);

            await context.Classes.AddRangeAsync(rows.Select(ToClass), cancellationToken);

            logger.LogInformation("Seeded {Count} classes: {Editions}.", rows.Count,
                string.Join(", ", rows.CountBy(r => r.Edition).Select(kvp => $"{kvp.Value} for {kvp.Key}")));
        }

        private static Class ToClass(ClassSeedRow row) => new()
        {
            Id = row.Id,
            Name = row.Name,
            Edition = row.Edition,
            Source = row.Source,
            HitDie = row.HitDie,
            SubclassLevel = row.SubclassLevel,
            AsiLevels = row.AsiLevels,
            SavingThrowAbilityIds = row.SavingThrowAbilityIds,
            ToolChoices = row.ToolChoices,
            ClassProficiencies = row.ProficiencyIds
                .Select(id => new ClassProficiency { ClassId = row.Id, ProficiencyId = id })
                .ToList(),
            ProficiencyChoice = row.ProficiencyChoices == null ? null : new ProficiencyChoice
            {
                ClassId = row.Id,
                Type = row.ProficiencyChoices.Type,
                Count = row.ProficiencyChoices.Count,
                Options = row.ProficiencyChoices.Options
                    .Select(id => new ProficiencyChoiceOption { ProficiencyId = id })
                    .ToList()
            },
            Spellcasting = row.Spellcasting == null ? null : new Spellcasting
            {
                ClassId = row.Id,
                SpellcastingAbility = row.Spellcasting.Ability,
                IsPreparedCaster = row.Spellcasting.IsPreparedCaster
            },
            SpellProgression = row.SpellProgression
                .Select(p => new ClassSpellProgression
                {
                    ClassId = row.Id,
                    CharacterLevel = p.Level,
                    CantripsKnown = p.CantripsKnown,
                    SpellsKnown = p.SpellsKnown,
                    MaxSpellLevel = p.MaxSpellLevel
                })
                .ToList()
        };
    }
}
