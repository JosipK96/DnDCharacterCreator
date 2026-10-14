using CharacterCreatorApp.Domain.DomainModels.Races;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Races;

public class SubraceAbilityBonusConfiguration : IEntityTypeConfiguration<SubraceAbilityBonus>
{
    public void Configure(EntityTypeBuilder<SubraceAbilityBonus> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.SubraceId)
            .HasMaxLength(50);

        builder.Property(e => e.AbilityScoreId)
            .HasMaxLength(3);

        builder.Property(e => e.Bonus);

        builder.HasOne(e => e.Subrace)
            .WithMany(s => s.AbilityBonuses)
            .HasForeignKey(e => e.SubraceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.SubraceId);
    }
}