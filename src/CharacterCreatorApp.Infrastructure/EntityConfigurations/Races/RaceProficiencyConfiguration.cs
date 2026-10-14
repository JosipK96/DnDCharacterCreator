using CharacterCreatorApp.Domain.DomainModels.Races;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Races
{
    public class RaceProficiencyConfiguration : IEntityTypeConfiguration<RaceProficiency>
    {
        public void Configure(EntityTypeBuilder<RaceProficiency> builder)
        {
            builder.HasKey(rp => new { rp.RaceId, rp.ProficiencyId });

            builder.Property(rp => rp.RaceId).HasMaxLength(50);
            builder.Property(rp => rp.ProficiencyId).HasMaxLength(50);

            builder.HasOne(rp => rp.Race)
                   .WithMany(r => r.RaceProficiencies)
                   .HasForeignKey(rp => rp.RaceId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(rp => rp.Proficiency)
                   .WithMany(p => p.RaceProficiencies)
                   .HasForeignKey(rp => rp.ProficiencyId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
