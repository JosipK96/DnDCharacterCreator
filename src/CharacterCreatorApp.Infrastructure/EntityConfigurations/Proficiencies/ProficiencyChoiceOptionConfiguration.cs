using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using CharacterCreatorApp.Domain.DomainModels.Proficiencies;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Proficiencies
{
    public class ProficiencyChoiceOptionConfiguration : IEntityTypeConfiguration<ProficiencyChoiceOption>
    {
        public void Configure(EntityTypeBuilder<ProficiencyChoiceOption> builder)
        {
            builder.HasKey(pco => new { pco.ProficiencyChoiceId, pco.ProficiencyId });

            builder.Property(pco => pco.ProficiencyChoiceId)
                .IsRequired();

            builder.Property(pco => pco.ProficiencyId)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasOne(pco => pco.ProficiencyChoice)
                .WithMany(pc => pc.Options)
                .HasForeignKey(pco => pco.ProficiencyChoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pco => pco.Proficiency)
                .WithMany(p => p.ProficiencyChoiceOptions)
                .HasForeignKey(pco => pco.ProficiencyId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
