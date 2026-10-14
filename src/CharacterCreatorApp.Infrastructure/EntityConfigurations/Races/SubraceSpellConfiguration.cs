using CharacterCreatorApp.Domain.DomainModels.Races;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Races;

public class SubraceSpellConfiguration : IEntityTypeConfiguration<SubraceSpell>
{
    public void Configure(EntityTypeBuilder<SubraceSpell> builder)
    {
        builder.HasKey(e => new { e.SubraceId, e.SpellId });

        builder.Property(e => e.SubraceId)
            .HasMaxLength(50);

        builder.Property(e => e.SpellId)
            .HasMaxLength(100);

        builder.HasOne(e => e.Spell)
            .WithMany()
            .HasForeignKey(e => e.SpellId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
