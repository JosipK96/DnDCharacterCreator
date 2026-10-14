using CharacterCreatorApp.Domain.DomainModels.Races;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Races
{
    public class RaceConfiguration : IEntityTypeConfiguration<Race>
    {
        public void Configure(EntityTypeBuilder<Race> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .HasMaxLength(50);

            builder.Property(r => r.Name)
                .HasMaxLength(50);

            builder.Property(r => r.Edition)
                .HasMaxLength(4);

            builder.Property(r => r.Source)
                .HasMaxLength(20);

            builder.Property(r => r.Size)
                .HasMaxLength(20);

            builder.Property(r => r.Speed);
            builder.Property(r => r.SkillChoiceCount);
            builder.Property(r => r.SkillChoiceOptions);
            builder.Property(r => r.LanguageChoiceCount);
            builder.Property(r => r.AbilityChoiceCount);
            builder.Property(r => r.AbilityChoiceOptions);
            builder.Property(r => r.FeatChoiceCount);
            builder.Property(r => r.FeatChoiceCategory)
                .HasMaxLength(30);

            builder.HasIndex(r => r.Edition);

            builder.HasMany(r => r.Spells)
                .WithOne(s => s.Race)
                .HasForeignKey(s => s.RaceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.RaceLanguages)
                .WithOne(rl => rl.Race)
                .HasForeignKey(rl => rl.RaceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.AbilityBonuses)
                .WithOne(ab => ab.Race)
                .HasForeignKey(ab => ab.RaceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.Traits)
                .WithOne(t => t.Race)
                .HasForeignKey(t => t.RaceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.RaceProficiencies)
                .WithOne(rp => rp.Race)
                .HasForeignKey(rp => rp.RaceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.Subraces)
                .WithOne(sr => sr.Race)
                .HasForeignKey(sr => sr.RaceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
