using CharacterCreatorApp.Domain.DomainModels.Races;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Races
{
    public class SubraceConfiguration : IEntityTypeConfiguration<Subrace>
    {
        public void Configure(EntityTypeBuilder<Subrace> builder)
        {
            builder.HasKey(sr => sr.Id);

            builder.Property(sr => sr.Id)
                .HasMaxLength(50);

            builder.Property(sr => sr.Name)
                .HasMaxLength(50);

            builder.Property(sr => sr.RaceId)
                .HasMaxLength(50);

            builder.Property(sr => sr.Source)
                .HasMaxLength(20);

            builder.Property(sr => sr.Speed);
            builder.Property(sr => sr.LanguageChoiceCount);
            builder.Property(sr => sr.AbilityChoiceCount);
            builder.Property(sr => sr.AbilityChoiceOptions);
            builder.Property(sr => sr.SkillChoiceCount);
            builder.Property(sr => sr.SkillChoiceOptions);
            builder.Property(sr => sr.FeatChoiceCount);
            builder.Property(sr => sr.FeatChoiceCategory)
                .HasMaxLength(30);

            builder.OwnsOne(sr => sr.SpellChoice).ToJson();

            builder.HasMany(sr => sr.Spells)
                .WithOne(s => s.Subrace)
                .HasForeignKey(s => s.SubraceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(sr => sr.Race)
                .WithMany(r => r.Subraces)
                .HasForeignKey(sr => sr.RaceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(sr => sr.AbilityBonuses)
                .WithOne(ab => ab.Subrace)
                .HasForeignKey(ab => ab.SubraceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(sr => sr.Traits)
                .WithOne(t => t.Subrace)
                .HasForeignKey(t => t.SubraceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(sr => sr.SubraceProficiencies)
                .WithOne(sp => sp.Subrace)
                .HasForeignKey(sp => sp.SubraceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
