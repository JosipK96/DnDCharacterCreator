using CharacterCreatorApp.Domain.DomainModels.Classes;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Classes
{
    public class ClassProficiencyConfiguration : IEntityTypeConfiguration<ClassProficiency>
    {
        public void Configure(EntityTypeBuilder<ClassProficiency> builder)
        {
            builder.HasKey(cp => new { cp.ClassId, cp.ProficiencyId });

            builder.Property(cp => cp.ClassId)
                .HasMaxLength(50);

            builder.Property(cp => cp.ProficiencyId)
                .HasMaxLength(50);

            builder.HasOne(cp => cp.Class)
                   .WithMany(c => c.ClassProficiencies)
                   .HasForeignKey(cp => cp.ClassId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cp => cp.Proficiency)
                   .WithMany(p => p.ClassProficiencies)
                   .HasForeignKey(cp => cp.ProficiencyId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}