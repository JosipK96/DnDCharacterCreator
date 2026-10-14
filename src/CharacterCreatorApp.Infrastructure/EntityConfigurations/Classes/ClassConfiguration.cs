using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Proficiencies;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Classes
{
    public class ClassConfiguration : IEntityTypeConfiguration<Class>
    {
        public void Configure(EntityTypeBuilder<Class> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                .HasMaxLength(50);

            builder.Property(c => c.Name)
                .HasMaxLength(50);

            builder.Property(c => c.HitDie)
                .HasMaxLength(10);

            builder.Property(c => c.Edition)
                .HasMaxLength(4);

            builder.Property(c => c.Source)
                .HasMaxLength(20);

            // Plain Postgres arrays (integer[] and text[]); nothing queries inside them
            builder.Property(c => c.AsiLevels);
            builder.Property(c => c.SavingThrowAbilityIds);

            builder.HasIndex(c => c.Edition);

            builder.HasMany(c => c.ClassProficiencies)
                   .WithOne(cp => cp.Class)
                   .HasForeignKey(cp => cp.ClassId);

            builder.HasOne(c => c.ProficiencyChoice)
                   .WithOne(pc => pc.Class)
                   .HasForeignKey<ProficiencyChoice>(pc => pc.ClassId);
        }
    }
}
