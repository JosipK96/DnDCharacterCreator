using CharacterCreatorApp.Domain.DomainModels.Races;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Races;

public class RaceAbilityBonusConfiguration : IEntityTypeConfiguration<RaceAbilityBonus>
{
    public void Configure(EntityTypeBuilder<RaceAbilityBonus> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RaceId)
            .HasMaxLength(50);

        builder.Property(e => e.AbilityScoreId)
            .HasMaxLength(3);

        builder.Property(e => e.Bonus);

        builder.HasOne(e => e.Race)
            .WithMany(r => r.AbilityBonuses)
            .HasForeignKey(e => e.RaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.RaceId);
    }
}