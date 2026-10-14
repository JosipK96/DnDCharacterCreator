using CharacterCreatorApp.Domain.DomainModels.Spells;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Spells;

public class SpellClassConfiguration : IEntityTypeConfiguration<SpellClass>
{
    public void Configure(EntityTypeBuilder<SpellClass> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.SpellId)
            .HasMaxLength(100);

        builder.Property(e => e.ClassId)
            .HasMaxLength(50);

        builder.HasOne(e => e.Spell)
            .WithMany(s => s.SpellClasses)
            .HasForeignKey(e => e.SpellId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Class)
            .WithMany()
            .HasForeignKey(e => e.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: a spell can only be assigned to a class once
        builder.HasIndex(e => new { e.SpellId, e.ClassId })
            .IsUnique();
    }
}