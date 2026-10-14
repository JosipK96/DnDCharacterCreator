using CharacterCreatorApp.Domain.DomainModels.Races;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Races;

public class RaceTraitConfiguration : IEntityTypeConfiguration<RaceTrait>
{
    public void Configure(EntityTypeBuilder<RaceTrait> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasMaxLength(100);

        builder.Property(e => e.RaceId)
            .HasMaxLength(50);

        builder.Property(e => e.Name)
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.HasOne(e => e.Race)
            .WithMany(r => r.Traits)
            .HasForeignKey(e => e.RaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.RaceId);
    }
}