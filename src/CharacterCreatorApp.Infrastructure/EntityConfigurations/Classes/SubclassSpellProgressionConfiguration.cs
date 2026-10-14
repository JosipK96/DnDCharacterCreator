using CharacterCreatorApp.Domain.DomainModels.Classes;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Classes;

public class SubclassSpellProgressionConfiguration : IEntityTypeConfiguration<SubclassSpellProgression>
{
    public void Configure(EntityTypeBuilder<SubclassSpellProgression> builder)
    {
        builder.HasKey(sp => new { sp.SubclassId, sp.CharacterLevel });

        builder.HasOne(sp => sp.Subclass)
               .WithMany(s => s.SpellProgression)
               .HasForeignKey(sp => sp.SubclassId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}