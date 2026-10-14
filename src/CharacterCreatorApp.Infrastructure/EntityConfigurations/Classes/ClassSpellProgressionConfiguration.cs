using CharacterCreatorApp.Domain.DomainModels.Classes;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Classes;

public class ClassSpellProgressionConfiguration : IEntityTypeConfiguration<ClassSpellProgression>
{
    public void Configure(EntityTypeBuilder<ClassSpellProgression> builder)
    {
        builder.HasKey(sp => new { sp.ClassId, sp.CharacterLevel });

        builder.HasOne(sp => sp.Class)
               .WithMany(c => c.SpellProgression)
               .HasForeignKey(sp => sp.ClassId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}