using CharacterCreatorApp.Domain.DomainModels.Races;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Races;

public class RaceSpellConfiguration : IEntityTypeConfiguration<RaceSpell>
{
    public void Configure(EntityTypeBuilder<RaceSpell> builder)
    {
        builder.HasKey(e => new { e.RaceId, e.SpellId });

        builder.Property(e => e.RaceId)
            .HasMaxLength(50);

        builder.Property(e => e.SpellId)
            .HasMaxLength(100);

        builder.HasOne(e => e.Spell)
            .WithMany()
            .HasForeignKey(e => e.SpellId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
