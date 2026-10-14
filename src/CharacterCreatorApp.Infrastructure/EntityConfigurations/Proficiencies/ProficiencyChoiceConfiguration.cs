using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using CharacterCreatorApp.Domain.DomainModels.Proficiencies;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Proficiencies
{
    public class ProficiencyChoiceConfiguration : IEntityTypeConfiguration<ProficiencyChoice>
    {
        public void Configure(EntityTypeBuilder<ProficiencyChoice> builder)
        {
            builder.HasKey(pc => pc.Id);

            builder.Property(pc => pc.Id)
                .IsRequired();

            builder.Property(pc => pc.ClassId)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(pc => pc.Type)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(pc => pc.Count)
                .IsRequired();

            builder.HasMany(pc => pc.Options)
                .WithOne(pco => pco.ProficiencyChoice)
                .HasForeignKey(pco => pco.ProficiencyChoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
