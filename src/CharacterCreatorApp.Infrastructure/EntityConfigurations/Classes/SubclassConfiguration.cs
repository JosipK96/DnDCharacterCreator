using CharacterCreatorApp.Domain.DomainModels.Classes;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Classes
{
    public class SubclassConfiguration : IEntityTypeConfiguration<Subclass>
    {
        public void Configure(EntityTypeBuilder<Subclass> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(x => x.Name)
                .HasMaxLength(50);

            builder.Property(x => x.SpellListClassId)
                .HasMaxLength(50);

            builder.Property(x => x.Source)
                .HasMaxLength(20);

            builder.Property(x => x.SpellSchools);
            builder.Property(x => x.AnySchoolSpellLevels);

            builder.OwnsOne(x => x.UnarmoredArmorClass).ToJson();

            builder.HasOne(s => s.Class)
                .WithMany(c => c.Subclasses)
                .HasForeignKey(s => s.ClassId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
