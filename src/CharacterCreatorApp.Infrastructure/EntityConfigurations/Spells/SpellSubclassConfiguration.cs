using CharacterCreatorApp.Domain.DomainModels.Spells;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Spells;

public class SpellSubclassConfiguration : IEntityTypeConfiguration<SpellSubclass>
{
    public void Configure(EntityTypeBuilder<SpellSubclass> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.SpellId)
            .HasMaxLength(100);

        builder.Property(e => e.SubclassId)
            .HasMaxLength(50);

        builder.HasOne(e => e.Spell)
            .WithMany(s => s.SpellSubclasses)
            .HasForeignKey(e => e.SpellId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Subclass)
            .WithMany(s => s.Spells)
            .HasForeignKey(e => e.SubclassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.SpellId, e.SubclassId })
            .IsUnique();
    }
}