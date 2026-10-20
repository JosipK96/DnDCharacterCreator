using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Infrastructure.Seed.Rows;

using Microsoft.Extensions.Logging;

namespace CharacterCreatorApp.Infrastructure.Seed.Seeders
{
    public class SubclassSeeder(ILogger<SubclassSeeder> logger) : IEntitySeeder
    {
        public int Priority => 4;

        public async Task SeedAsync(ApplicationDbContext context, string dataPath, CancellationToken cancellationToken)
        {
            var rows = await SeedFile.ReadListAsync<SubclassSeedRow>(Path.Combine(dataPath, "Subclasses.json"), cancellationToken);

            await context.Subclasses.AddRangeAsync(rows.Select(ToSubclass), cancellationToken);

            logger.LogInformation("Seeded {Count} subclasses.", rows.Count);
        }

        private static Subclass ToSubclass(SubclassSeedRow row)
        {
            if (row.SpellProgression.Count > 0 && string.IsNullOrEmpty(row.SpellcastingAbility))
            {
                throw new InvalidOperationException($"Subclass {row.Id} has a spell progression but no spellcasting ability.");
            }

            return new Subclass
            {
                Id = row.Id,
                Name = row.Name,
                ClassId = row.ClassId,
                Source = row.Source,
                SpellListClassId = row.SpellListClassId,
                SpellSchools = row.SpellSchools,
                AnySchoolSpellLevels = row.AnySchoolSpellLevels,
                UnarmoredArmorClass = row.UnarmoredArmorClass,
                SpellProgression = row.SpellProgression
                    .Select(p => new SubclassSpellProgression
                    {
                        SubclassId = row.Id,
                        CharacterLevel = p.Level,
                        CantripsKnown = p.CantripsKnown,
                        SpellsKnown = p.SpellsKnown,
                        MaxSpellLevel = p.MaxSpellLevel,
                        SpellcastingAbility = row.SpellcastingAbility!
                    })
                    .ToList()
            };
        }
    }
}
