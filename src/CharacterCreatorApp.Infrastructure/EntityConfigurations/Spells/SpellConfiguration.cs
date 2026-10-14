using CharacterCreatorApp.Domain.DomainModels.Spells;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Spells
{
    public class SpellConfiguration : IEntityTypeConfiguration<Spell>
    {
        public void Configure(EntityTypeBuilder<Spell> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                .HasMaxLength(100);

            builder.Property(s => s.Name)
                .HasMaxLength(100);

            builder.Property(s => s.School)
                .HasMaxLength(20);

            builder.Property(s => s.CastingTimeUnit)
                .HasMaxLength(20);

            builder.Property(s => s.RangeType)
                .HasMaxLength(20);

            builder.Property(s => s.RangeDistanceType)
                .HasMaxLength(20);

            builder.Property(s => s.Material)
                .HasMaxLength(500);

            builder.Property(s => s.DurationType)
                .HasMaxLength(20);

            builder.Property(s => s.DurationUnit)
                .HasMaxLength(20);

            builder.HasMany(s => s.SpellClasses)
                .WithOne(sc => sc.Spell)
                .HasForeignKey(sc => sc.SpellId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.SpellSubclasses)
                .WithOne(ss => ss.Spell)
                .HasForeignKey(ss => ss.SpellId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(s => s.Level);
            builder.HasIndex(s => s.School);
            builder.HasIndex(s => s.IsRitual);
            builder.HasIndex(s => s.IsConcentration);
            builder.HasIndex(s => s.HasMaterialComponent);

            builder.HasIndex(s => new { s.Level, s.School });
            builder.HasIndex(s => new { s.Level, s.IsRitual });
            builder.HasIndex(s => new { s.School, s.IsConcentration });
        }
    }
}
