using CharacterCreatorApp.Domain.DomainModels.Backgrounds;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Backgrounds
{
    public class BackgroundProficiencyConfiguration : IEntityTypeConfiguration<BackgroundProficiency>
    {
        public void Configure(EntityTypeBuilder<BackgroundProficiency> builder)
        {
            builder.HasKey(bp => new { bp.BackgroundId, bp.ProficiencyId });

            builder.Property(bp => bp.BackgroundId)
                .HasMaxLength(50);

            builder.Property(bp => bp.ProficiencyId)
                .HasMaxLength(50);

            builder.HasOne(bp => bp.Background)
                   .WithMany(b => b.BackgroundProficiencies)
                   .HasForeignKey(bp => bp.BackgroundId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(bp => bp.Proficiency)
                   .WithMany(p => p.BackgroundProficiencies)
                   .HasForeignKey(bp => bp.ProficiencyId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
