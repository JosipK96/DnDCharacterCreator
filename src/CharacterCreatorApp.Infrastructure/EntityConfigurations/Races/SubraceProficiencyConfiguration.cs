using CharacterCreatorApp.Domain.DomainModels.Races;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Races
{
    public class SubraceProficiencyConfiguration : IEntityTypeConfiguration<SubraceProficiency>
    {
        public void Configure(EntityTypeBuilder<SubraceProficiency> builder)
        {
            builder.HasKey(sp => new { sp.SubraceId, sp.ProficiencyId });

            builder.Property(sp => sp.SubraceId)
                .HasMaxLength(50);

            builder.Property(sp => sp.ProficiencyId)
                .HasMaxLength(50);

            builder.HasOne(sp => sp.Subrace)
                   .WithMany(sr => sr.SubraceProficiencies)
                   .HasForeignKey(sp => sp.SubraceId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(sp => sp.Proficiency)
                   .WithMany(p => p.SubraceProficiencies)
                   .HasForeignKey(sp => sp.ProficiencyId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
