using CharacterCreatorApp.Domain.DomainModels.Proficiencies;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Proficiencies
{
    public class ProficiencyConfiguration : IEntityTypeConfiguration<Proficiency>
    {
        public void Configure(EntityTypeBuilder<Proficiency> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .HasMaxLength(50);

            builder.Property(p => p.Name)
                .HasMaxLength(50);

            builder.Property(p => p.Type)
                .HasMaxLength(20);

            builder.HasMany(p => p.ClassProficiencies)
                   .WithOne(cp => cp.Proficiency)
                   .HasForeignKey(cp => cp.ProficiencyId);
        }
    }
}
