using CharacterCreatorApp.Domain.DomainModels.Classes;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Classes;

public class SpellcastingConfiguration : IEntityTypeConfiguration<Spellcasting>
{
    public void Configure(EntityTypeBuilder<Spellcasting> builder)
    {
        builder.ToTable("Spellcasting");

        // ClassId is both the primary key AND foreign key
        builder.HasKey(s => s.ClassId);

        // Configure one-to-one relationship
        builder.HasOne(s => s.Class)
               .WithOne(c => c.Spellcasting)
               .HasForeignKey<Spellcasting>(s => s.ClassId)
               .OnDelete(DeleteBehavior.Cascade);

        // Required fields
        builder.Property(s => s.SpellcastingAbility)
               .IsRequired()
               .HasMaxLength(20); // "Intelligence", "Wisdom", "Charisma"

        builder.Property(s => s.IsPreparedCaster)
               .IsRequired()
               .HasDefaultValue(false);
    }
}