using CharacterCreatorApp.Domain.DomainModels.Spells;
using CharacterCreatorApp.Infrastructure.Seed.Rows;

using Microsoft.Extensions.Logging;

namespace CharacterCreatorApp.Infrastructure.Seed.Seeders
{
    public class SpellSeeder(ILogger<SpellSeeder> logger) : IEntitySeeder
    {
        public int Priority => 7;

        public async Task SeedAsync(ApplicationDbContext context, string dataPath, CancellationToken cancellationToken)
        {
            var spellsPath = Path.Combine(dataPath, "Spells");

            var spells = await SeedFile.ReadListAsync<SpellSeedRow>(Path.Combine(spellsPath, "Spells.json"), cancellationToken);
            var spellClasses = await SeedFile.ReadListAsync<SpellClassSeedRow>(Path.Combine(spellsPath, "SpellClasses.json"), cancellationToken);
            var spellSubclasses = await SeedFile.ReadListAsync<SpellSubclassSeedRow>(Path.Combine(spellsPath, "SpellSubclasses.json"), cancellationToken);

            // The subclass seeder runs first and its rows are not saved yet. A foreign key would
            // catch an unknown id too, but only as one failed insert among thousands.
            var existingSubclassIds = context.Subclasses.Local.Select(s => s.Id).ToHashSet();
            var invalidSubclassIds = spellSubclasses
                .Select(ss => ss.SubclassId)
                .Distinct()
                .Where(id => !existingSubclassIds.Contains(id))
                .ToList();

            if (invalidSubclassIds.Count > 0)
            {
                throw new InvalidOperationException(
                    $"SpellSubclasses.json names subclasses that do not exist: {string.Join(", ", invalidSubclassIds)}");
            }

            await context.Spells.AddRangeAsync(spells.Select(ToSpell), cancellationToken);
            await context.SpellClasses.AddRangeAsync(
                spellClasses.Select(r => new SpellClass { SpellId = r.SpellId, ClassId = r.ClassId }), cancellationToken);
            await context.SpellSubclasses.AddRangeAsync(
                spellSubclasses.Select(r => new SpellSubclass
                {
                    SpellId = r.SpellId,
                    SubclassId = r.SubclassId,
                    Level = r.Level,
                    IsAlwaysPrepared = r.IsAlwaysPrepared
                }),
                cancellationToken);

            logger.LogInformation("Seeded {SpellCount} spells with {ClassMappings} class mappings and {SubclassMappings} subclass mappings.",
                spells.Count, spellClasses.Count, spellSubclasses.Count);
        }

        private static Spell ToSpell(SpellSeedRow row) => new()
        {
            Id = row.Id,
            Name = row.Name,
            Level = row.Level,
            School = row.School,
            CastingTimeNumber = row.CastingTimeNumber,
            CastingTimeUnit = row.CastingTimeUnit,
            RangeType = row.RangeType,
            RangeDistanceType = row.RangeDistanceType,
            RangeDistanceAmount = row.RangeDistanceAmount,
            HasVerbalComponent = row.HasVerbalComponent,
            HasSomaticComponent = row.HasSomaticComponent,
            HasMaterialComponent = row.HasMaterialComponent,
            Material = row.Material,
            DurationType = row.DurationType,
            DurationAmount = row.DurationAmount,
            DurationUnit = row.DurationUnit,
            IsConcentration = row.IsConcentration,
            IsRitual = row.IsRitual,
            Description = row.Description
        };
    }
}
